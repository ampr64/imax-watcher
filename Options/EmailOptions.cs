namespace ImaxWatcher.Options;

using System.ComponentModel.DataAnnotations;
using ImaxWatcher.Options.Validation;

public sealed record EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }

    [Required]
    public required string Host { get; init; }

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool UseSsl { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "Email:From es obligatorio cuando Email:Enabled=true.")]
    [EmailAddressIf(nameof(Enabled), ErrorMessage = "Email:From no es una dirección válida.")]
    public required string From { get; init; }

    public string? FromName { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "Email:To es obligatorio cuando Email:Enabled=true.")]
    [EmailAddressIf(nameof(Enabled), ErrorMessage = "Email:To tiene una dirección inválida.")]
    public required string To { get; init; }

    [RequiredIf(nameof(Enabled), ErrorMessage = "Email:Password es obligatorio cuando Email:Enabled=true.")]
    public required string Password { get; init; }

    public IReadOnlyList<string> Recipients =>
        To.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
