namespace ImaxWatcher.Services.Parsing;

/// <summary>Projection of a DOM &lt;option&gt;. Deserialized from Playwright.</summary>
internal sealed class DomOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Disabled { get; set; }
}
