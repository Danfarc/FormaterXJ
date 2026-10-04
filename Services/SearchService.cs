using System;
using System.Collections.Generic;
using FormaterXJ.Models;

namespace FormaterXJ.Services;

public static class SearchService
{
    public static IReadOnlyList<DocumentNode> FindMatches(
        DocumentNode root,
        string query,
        bool matchCase = false,
        bool matchWholeWord = false)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<DocumentNode>();
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var matches = new List<DocumentNode>();
        Traverse(root, node =>
        {
            if (Matches(node, query, comparison, matchWholeWord))
            {
                matches.Add(node);
            }
        });

        return matches;
    }

    public static IReadOnlyList<(int Start, int Length)> FindTextMatches(
        string text,
        string query,
        bool matchCase = false,
        bool matchWholeWord = false)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<(int Start, int Length)>();
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var matches = new List<(int Start, int Length)>();
        var index = text.IndexOf(query, 0, comparison);
        while (index >= 0)
        {
            var end = index + query.Length;
            var hasWordBoundaryBefore = !IsWordCharacter(query[0])
                || index == 0
                || !IsWordCharacter(text[index - 1]);
            var hasWordBoundaryAfter = !IsWordCharacter(query[^1])
                || end == text.Length
                || !IsWordCharacter(text[end]);

            if (!matchWholeWord || (hasWordBoundaryBefore && hasWordBoundaryAfter))
            {
                matches.Add((index, query.Length));
            }

            index = text.IndexOf(query, index + 1, comparison);
        }

        return matches;
    }

    private static void Traverse(DocumentNode node, Action<DocumentNode> visitor)
    {
        visitor(node);

        foreach (var child in node.Children)
        {
            Traverse(child, visitor);
        }
    }

    private static bool Matches(
        DocumentNode node,
        string query,
        StringComparison comparison,
        bool matchWholeWord)
    {
        return Contains(node.Name, query, comparison, matchWholeWord)
            || Contains(node.Value, query, comparison, matchWholeWord)
            || Contains(node.Path, query, comparison, matchWholeWord)
            || Contains(node.Type, query, comparison, matchWholeWord);
    }

    private static bool Contains(
        string? value,
        string query,
        StringComparison comparison,
        bool matchWholeWord)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var index = value.IndexOf(query, 0, comparison);
        while (index >= 0)
        {
            var end = index + query.Length;
            var hasWordBoundaryBefore = !IsWordCharacter(query[0])
                || index == 0
                || !IsWordCharacter(value[index - 1]);
            var hasWordBoundaryAfter = !IsWordCharacter(query[^1])
                || end == value.Length
                || !IsWordCharacter(value[end]);

            if (!matchWholeWord || (hasWordBoundaryBefore && hasWordBoundaryAfter))
            {
                return true;
            }

            index = value.IndexOf(query, index + 1, comparison);
        }

        return false;
    }

    private static bool IsWordCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '_';
    }
}
