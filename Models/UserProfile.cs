namespace FormaterXJ.Models;

public sealed class UserProfile
{
    public string Name { get; set; } = string.Empty;
    public double EditorFontSize { get; set; } = 10;
    public double TreeFontSize { get; set; } = 13;
    public bool ShowLineNumbers { get; set; } = true;
    public bool IsTreeOnLeft { get; set; }
    public string Theme { get; set; } = "Dark";
}
