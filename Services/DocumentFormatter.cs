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
                using var document = JsonDocument.Parse(text);
                return DocumentResult.Valid(DocumentFormatKind.Json, FormatJson(document.RootElement));
            }
            catch (JsonException ex)
            {
                var jsonError = $"El contenido parece JSON, pero no es válido: {ex.Message}";

                if (!LooksLikeXml(text))
                {
                    return DocumentResult.Invalid(jsonError);
                }

                try
                {
                    var xmlDocument = XDocument.Parse(text);
                    return DocumentResult.Valid(DocumentFormatKind.Xml, FormatXml(xmlDocument));
                }
                catch (XmlException xmlException)
                {
                    return DocumentResult.Invalid($"El contenido no es JSON ni XML válido. JSON: {jsonError}. XML: {xmlException.Message}");
                }
            }
        }

        if (LooksLikeXml(text))
        {
            try
            {
                var xmlDocument = XDocument.Parse(text);
                return DocumentResult.Valid(DocumentFormatKind.Xml, FormatXml(xmlDocument));
            }
            catch (XmlException ex)
            {
                try
                {
                    using var document = JsonDocument.Parse(text);
                    return DocumentResult.Valid(DocumentFormatKind.Json, FormatJson(document.RootElement));
                }
                catch (JsonException jsonException)
                {
                    return DocumentResult.Invalid($"El contenido no es JSON ni XML válido. XML: {ex.Message}. JSON: {jsonException.Message}");
                }
            }
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(text);
            return DocumentResult.Valid(DocumentFormatKind.Json, FormatJson(jsonDocument.RootElement));
        }
        catch (JsonException)
        {
            try
            {
                var xmlDocument = XDocument.Parse(text);
                return DocumentResult.Valid(DocumentFormatKind.Xml, FormatXml(xmlDocument));
            }
            catch (XmlException ex)
            {
                return DocumentResult.Invalid($"El contenido no es un JSON ni XML válido. Error: {ex.Message}");
            }
        }
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
