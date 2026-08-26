namespace ImaxWatcher.Options;

using ImaxWatcher.Options.Validation;

public sealed record TelegramOptions
{
    public const string SectionName = "Telegram";

    public bool Enabled { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "Telegram:BotApiUrl es obligatorio cuando Telegram:Enabled=true.")]
    public string BotApiUrl { get; init; } = string.Empty;

    [RequiredIf(nameof(Enabled), ErrorMessage = "Telegram:BotToken es obligatorio cuando Telegram:Enabled=true.")]
    public required string BotToken { get; init; }

    [RequiredIf(nameof(Enabled), AllowDefault = false, ErrorMessage = "Telegram:ChatId debe ser distinto de 0 cuando Telegram:Enabled=true.")]
    public long ChatId { get; init; }
}