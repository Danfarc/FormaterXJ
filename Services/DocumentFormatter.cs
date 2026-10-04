using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using FormaterXJ.Models;

namespace FormaterXJ.Services;

public static class DocumentFormatter
{
    public static DocumentResult Analyze(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DocumentResult.Invalid("El contenido está vacío. Pega o carga un documento JSON o XML válido.");
        }

        var text = input.Trim();

        if (LooksLikeJson(text))
        {
            try
            {
                using var document = JsonDocument.Parse(input);
                return DocumentResult.Valid(DocumentFormatKind.Json, FormatJson(document.RootElement));
            }
            catch (JsonException ex)
            {
                var jsonError = $"El contenido parece JSON, pero no es válido: {ex.Message}";
                var jsonFailure = CreateJsonError(input, jsonError, ex);

                if (!LooksLikeXml(text))
                {
                    return jsonFailure;
                }

                try
                {
                    var xmlDocument = XDocument.Parse(input);
                    return DocumentResult.Valid(DocumentFormatKind.Xml, FormatXml(xmlDocument));
                }
                catch (XmlException xmlException)
                {
                    return CreateXmlError(input,
                        $"El contenido no es JSON ni XML válido. JSON: {jsonError}. XML: {xmlException.Message}",
                        xmlException);
                }
            }
        }

        if (LooksLikeXml(text))
        {
            try
            {
                var xmlDocument = XDocument.Parse(input);
                return DocumentResult.Valid(DocumentFormatKind.Xml, FormatXml(xmlDocument));
            }
            catch (XmlException ex)
            {
                try
                {
                    using var document = JsonDocument.Parse(input);
                    return DocumentResult.Valid(DocumentFormatKind.Json, FormatJson(document.RootElement));
                }
                catch (JsonException jsonException)
                {
                    return CreateXmlError(input,
                        $"El contenido no es JSON ni XML válido. XML: {ex.Message}. JSON: {jsonException.Message}",
                        ex);
                }
            }
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(input);
            return DocumentResult.Valid(DocumentFormatKind.Json, FormatJson(jsonDocument.RootElement));
        }
        catch (JsonException jsonException)
        {
            try
            {
                var xmlDocument = XDocument.Parse(input);
                return DocumentResult.Valid(DocumentFormatKind.Xml, FormatXml(xmlDocument));
            }
            catch (XmlException xmlException)
            {
                return LooksLikeXml(text)
                    ? CreateXmlError(input,
                        $"El contenido no es un JSON ni XML válido. XML: {xmlException.Message}. JSON: {jsonException.Message}",
                        xmlException)
                    : CreateJsonError(input,
                        $"El contenido no es un JSON ni XML válido. JSON: {jsonException.Message}. XML: {xmlException.Message}",
                        jsonException);
            }
        }
    }

    private static DocumentResult CreateJsonError(string input, string message, JsonException exception)
    {
        var line = (int)(exception.LineNumber ?? 0) + 1;
        var byteColumn = (int)(exception.BytePositionInLine ?? 0);
        var column = GetJsonCharacterColumn(input, line, byteColumn);
        return DocumentResult.InvalidAt(message, line, column, GetPosition(input, line, column));
    }

    private static DocumentResult CreateXmlError(string input, string message, XmlException exception)
    {
        var line = Math.Max(1, exception.LineNumber);
        var column = Math.Max(1, exception.LinePosition);
        return DocumentResult.InvalidAt(message, line, column, GetPosition(input, line, column));
    }

    private static int GetJsonCharacterColumn(string input, int line, int byteColumn)
    {
        var lineStart = GetLineStart(input, line);
        var lineEnd = input.IndexOfAny(['\r', '\n'], lineStart);
        if (lineEnd < 0)
        {
            lineEnd = input.Length;
        }

        var column = 0;
        var bytes = 0;
        while (lineStart + column < lineEnd && bytes < byteColumn)
        {
            var characterLength = char.IsHighSurrogate(input[lineStart + column])
                && lineStart + column + 1 < lineEnd
                && char.IsLowSurrogate(input[lineStart + column + 1])
                    ? 2
                    : 1;
            var characterBytes = Encoding.UTF8.GetByteCount(input.AsSpan(lineStart + column, characterLength));
            if (bytes + characterBytes > byteColumn)
            {
                break;
            }

            bytes += characterBytes;
            column += characterLength;
        }

        return column + 1;
    }

    private static int GetPosition(string input, int line, int column)
    {
        var lineStart = GetLineStart(input, line);
        return Math.Min(lineStart + column - 1, input.Length);
    }

    private static int GetLineStart(string input, int line)
    {
        var lineStart = 0;
        for (var currentLine = 1; currentLine < line && lineStart < input.Length; currentLine++)
        {
            var newline = input.IndexOf('\n', lineStart);
            if (newline < 0)
            {
                return input.Length;
            }

            lineStart = newline + 1;
        }

        return lineStart;
    }

    private static bool LooksLikeJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private static bool LooksLikeXml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.TrimStart();
        return trimmed.StartsWith('<');
    }

    private static string FormatJson(JsonElement element)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(element, options);
    }

    private static string FormatXml(XDocument document)
    {
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
