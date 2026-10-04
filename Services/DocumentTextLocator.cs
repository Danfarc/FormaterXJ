using System;
using System.Text.Json;
using FormaterXJ.Models;

namespace FormaterXJ.Services;

public static class DocumentTextLocator
{
    public static void Locate(DocumentNode root, string text, DocumentFormatKind format)
    {
        var cursor = 0;
        LocateNode(root, text, format, ref cursor, true);
    }

    private static void LocateNode(DocumentNode node, string text, DocumentFormatKind format, ref int cursor, bool isRoot = false)
    {
        ResetSpans(node);
        var containerStart = -1;

        if (format == DocumentFormatKind.Json)
        {
            LocateJsonNode(node, text, ref cursor, isRoot, out containerStart);
        }
        else if (format == DocumentFormatKind.Xml)
        {
            LocateXmlNode(node, text, ref cursor, out containerStart);
        }

        foreach (var child in node.Children)
        {
            LocateNode(child, text, format, ref cursor);
        }

        if (containerStart >= 0)
        {
            var closeIndex = format == DocumentFormatKind.Json
                ? FindMatchingJsonClose(text, containerStart)
                : FindMatchingXmlClose(text, containerStart, node.Name);

            if (closeIndex >= containerStart)
            {
                var blockStart = node.EditorBlockStart >= 0 ? node.EditorBlockStart : containerStart;
                SetBlockSpan(node, blockStart, closeIndex - blockStart + 1);
            }
        }
    }

    private static void LocateJsonNode(
        DocumentNode node,
        string text,
        ref int cursor,
        bool isRoot,
        out int containerStart)
    {
        containerStart = -1;
        var isContainer = node.Type is "Object" or "Array";
        var isArrayItem = node.Name.StartsWith("[", StringComparison.Ordinal);
        var nameStart = -1;

        if (!isRoot && !isArrayItem)
        {
            var serializedName = JsonSerializer.Serialize(
                node.Name,
                new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            nameStart = text.IndexOf(serializedName, cursor, StringComparison.Ordinal);
            if (nameStart < 0)
            {
                return;
            }

            SetTokenSpan(node, nameStart, serializedName.Length);
            var colon = text.IndexOf(':', nameStart + serializedName.Length);
            if (colon < 0)
            {
                return;
            }

            cursor = SkipWhitespace(text, colon + 1);
        }

        var valueStart = SkipWhitespace(text, cursor);
        if (valueStart >= text.Length)
        {
            return;
        }

        if (isContainer)
        {
            var opening = node.Type == "Array" ? '[' : '{';
            var position = text.IndexOf(opening, valueStart);
            if (position < 0)
            {
                return;
            }

            containerStart = position;
            if (nameStart >= 0)
            {
                SetTokenSpan(node, nameStart, node.EditorLength);
                SetBlockSpan(node, nameStart, 0);
            }
            else
            {
                SetTokenSpan(node, position, 1);
                SetBlockSpan(node, position, 0);
            }

            cursor = position + 1;
            return;
        }

        var tokenLength = GetJsonValueLength(text, valueStart);
        if (tokenLength <= 0)
        {
            return;
        }

        if (nameStart >= 0)
        {
            SetBlockSpan(node, nameStart, valueStart + tokenLength - nameStart);
        }
        else
        {
            SetTokenSpan(node, valueStart, tokenLength);
            SetBlockSpan(node, valueStart, tokenLength);
        }

        cursor = valueStart + tokenLength;
    }

    private static void LocateXmlNode(DocumentNode node, string text, ref int cursor, out int containerStart)
    {
        containerStart = -1;

        if (node.Type == "Atributo")
        {
            var attributeName = node.Name.TrimStart('@');
            var nameStart = text.IndexOf(attributeName, cursor, StringComparison.Ordinal);
            while (nameStart >= 0)
            {
                var equalsIndex = SkipWhitespace(text, nameStart + attributeName.Length);
                if (equalsIndex < text.Length && text[equalsIndex] == '=')
                {
                    var valueStart = SkipWhitespace(text, equalsIndex + 1);
                    if (valueStart < text.Length && text[valueStart] is '"' or '\'')
                    {
                        var quote = text[valueStart];
                        var valueEnd = text.IndexOf(quote, valueStart + 1);
                        if (valueEnd >= 0)
                        {
                            SetTokenSpan(node, nameStart, attributeName.Length);
                            SetBlockSpan(node, nameStart, valueEnd - nameStart + 1);
                            cursor = valueEnd + 1;
                            return;
                        }
                    }
                }

                nameStart = text.IndexOf(attributeName, nameStart + attributeName.Length, StringComparison.Ordinal);
            }

            return;
        }

        var tag = $"<{node.Name}";
        var tagStart = text.IndexOf(tag, cursor, StringComparison.Ordinal);
        while (tagStart >= 0)
        {
            var afterName = tagStart + tag.Length;
            if (afterName == text.Length || char.IsWhiteSpace(text[afterName]) || text[afterName] is '>' or '/')
            {
                containerStart = tagStart;
                SetTokenSpan(node, tagStart + 1, node.Name.Length);
                SetBlockSpan(node, tagStart, 0);
                cursor = afterName;
                return;
            }

            tagStart = text.IndexOf(tag, afterName, StringComparison.Ordinal);
        }
    }

    private static int FindMatchingJsonClose(string text, int openingIndex)
    {
        var open = text[openingIndex];
        var close = open == '{' ? '}' : ']';
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var index = openingIndex; index < text.Length; index++)
        {
            var character = text[index];
            if (inString)
            {
                if (character == '"' && !escaped)
                {
                    inString = false;
                }

                if (character == '\\' && !escaped)
                {
                    escaped = true;
                }
                else
                {
                    escaped = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
            }
            else if (character == open)
            {
                depth++;
            }
            else if (character == close && --depth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindMatchingXmlClose(string text, int openingIndex, string localName)
    {
        var openingEnd = FindXmlTagEnd(text, openingIndex);
        if (openingEnd < 0 || IsSelfClosingTag(text, openingIndex, openingEnd))
        {
            return openingEnd;
        }

        var depth = 1;
        var searchIndex = openingEnd + 1;
        while (searchIndex < text.Length)
        {
            var tagStart = text.IndexOf('<', searchIndex);
            if (tagStart < 0)
            {
                return -1;
            }

            if (text.AsSpan(tagStart).StartsWith("<!--", StringComparison.Ordinal))
            {
                var commentEnd = text.IndexOf("-->", tagStart + 4, StringComparison.Ordinal);
                if (commentEnd < 0)
                {
                    return -1;
                }

                searchIndex = commentEnd + 3;
                continue;
            }

            if (text.AsSpan(tagStart).StartsWith("<![CDATA[", StringComparison.Ordinal))
            {
                var cdataEnd = text.IndexOf("]]>", tagStart + 9, StringComparison.Ordinal);
                if (cdataEnd < 0)
                {
                    return -1;
                }

                searchIndex = cdataEnd + 3;
                continue;
            }

            if (text.AsSpan(tagStart).StartsWith("<?", StringComparison.Ordinal)
                || text.AsSpan(tagStart).StartsWith("<!", StringComparison.Ordinal))
            {
                var declarationEnd = FindXmlTagEnd(text, tagStart);
                if (declarationEnd < 0)
                {
                    return -1;
                }

                searchIndex = declarationEnd + 1;
                continue;
            }

            var tagEnd = FindXmlTagEnd(text, tagStart);
            if (tagEnd < 0)
            {
                return -1;
            }

            var closingTag = text[tagStart + 1] == '/';
            var nameStart = tagStart + (closingTag ? 2 : 1);
            var nameEnd = nameStart;
            while (nameEnd < tagEnd && !char.IsWhiteSpace(text[nameEnd]) && text[nameEnd] is not '/' and not '>')
            {
                nameEnd++;
            }

            var qualifiedName = text[nameStart..nameEnd];
            var colon = qualifiedName.LastIndexOf(':');
            var tagLocalName = colon >= 0 ? qualifiedName[(colon + 1)..] : qualifiedName;
            if (string.Equals(tagLocalName, localName, StringComparison.Ordinal))
            {
                if (closingTag)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return tagEnd;
                    }
                }
                else if (!IsSelfClosingTag(text, tagStart, tagEnd))
                {
                    depth++;
                }
            }

            searchIndex = tagEnd + 1;
        }

        return -1;
    }

    private static int FindXmlTagEnd(string text, int tagStart)
    {
        var quote = '\0';
        for (var index = tagStart + 1; index < text.Length; index++)
        {
            var character = text[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character is '"' or '\'')
            {
                quote = character;
            }
            else if (character == '>')
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSelfClosingTag(string text, int tagStart, int tagEnd)
    {
        var index = tagEnd - 1;
        while (index > tagStart && char.IsWhiteSpace(text[index]))
        {
            index--;
        }

        return text[index] == '/';
    }

    private static int GetJsonValueLength(string text, int start)
    {
        if (text[start] == '"')
        {
            var escaped = false;
            for (var index = start + 1; index < text.Length; index++)
            {
                if (text[index] == '"' && !escaped)
                {
                    return index - start + 1;
                }

                if (text[index] == '\\' && !escaped)
                {
                    escaped = true;
                }
                else
                {
                    escaped = false;
                }
            }

            return 0;
        }

        var end = start;
        while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] is not ',' and not ']' and not '}')
        {
            end++;
        }

        return end - start;
    }

    private static int SkipWhitespace(string text, int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }

    private static void ResetSpans(DocumentNode node)
    {
        node.EditorStart = -1;
        node.EditorLength = 0;
        node.EditorBlockStart = -1;
        node.EditorBlockLength = 0;
    }

    private static void SetTokenSpan(DocumentNode node, int start, int length)
    {
        node.EditorStart = start;
        node.EditorLength = length;
    }

    private static void SetBlockSpan(DocumentNode node, int start, int length)
    {
        node.EditorBlockStart = start;
        node.EditorBlockLength = length;
    }
}
