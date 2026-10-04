using System;
using System.Collections.Generic;
using FormaterXJ.Models;

namespace FormaterXJ.Services;

public static class SearchService
{
    public static IReadOnlyList<DocumentNode> FindMatches(DocumentNode root, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<DocumentNode>();
        }

        var matches = new List<DocumentNode>();
        Traverse(root, node =>
        {
            if (Matches(node, query))
            {
                matches.Add(node);
            }
        });

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

    private static bool Matches(DocumentNode node, string query)
    {
        return Contains(node.Name, query)
            || Contains(node.Value, query)
            || Contains(node.Path, query)
            || Contains(node.Type, query);
    }

    private static bool Contains(string? value, string query)
    {
        return !string.IsNullOrEmpty(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
