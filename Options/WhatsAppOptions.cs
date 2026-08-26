namespace ImaxWatcher.Options;

using ImaxWatcher.Options.Validation;

public sealed record WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public bool Enabled { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "WhatsApp:AccountSid es obligatorio cuando WhatsApp:Enabled=true.")]
    public required string AccountSid { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "WhatsApp:ApiKeySid es obligatorio cuando WhatsApp:Enabled=true.")]
    public required string ApiKeySid { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "WhatsApp:ApiKeySecret es obligatorio cuando WhatsApp:Enabled=true.")]
    public required string ApiKeySecret { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "WhatsApp:From es obligatorio cuando WhatsApp:Enabled=true.")]
    public required string From { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "WhatsApp:To es obligatorio cuando WhatsApp:Enabled=true.")]
    public required string To { get; init; }

    public string? ContentSid { get; init; }
}