namespace FormaterXJ.Models;

public enum DocumentFormatKind
{
    Json,
    Xml,
    None
}

public sealed class DocumentResult
{
    public DocumentFormatKind Kind { get; init; }
    public bool IsValid => Kind != DocumentFormatKind.None;
    public string FormattedText { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public int ErrorLine { get; init; }
    public int ErrorColumn { get; init; }
    public int ErrorPosition { get; init; } = -1;
    public string DetectLabel => Kind switch
    {
        DocumentFormatKind.Json => "JSON",
        DocumentFormatKind.Xml => "XML",
        _ => "Sin formato"
    };

    public static DocumentResult Invalid(string message) => new() { Kind = DocumentFormatKind.None, ErrorMessage = message };
    public static DocumentResult InvalidAt(string message, int line, int column, int position) => new()
    {
        Kind = DocumentFormatKind.None,
        ErrorMessage = message,
        ErrorLine = line,
        ErrorColumn = column,
        ErrorPosition = position
    };

    public static DocumentResult Valid(DocumentFormatKind kind, string formattedText) => new() { Kind = kind, FormattedText = formattedText };
}
