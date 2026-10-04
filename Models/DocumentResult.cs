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
    public string DetectLabel => Kind switch
    {
        DocumentFormatKind.Json => "JSON",
        DocumentFormatKind.Xml => "XML",
        _ => "Sin formato"
    };

    public static DocumentResult Invalid(string message) => new() { Kind = DocumentFormatKind.None, ErrorMessage = message };
    public static DocumentResult Valid(DocumentFormatKind kind, string formattedText) => new() { Kind = kind, FormattedText = formattedText };
}
