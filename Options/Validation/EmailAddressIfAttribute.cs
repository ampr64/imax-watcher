using System.ComponentModel.DataAnnotations;

namespace ImaxWatcher.Options.Validation;

/// <summary>
/// Validates email format only when the channel is enabled.
/// A bare <see cref="EmailAddressAttribute"/> rejects the empty string, so applying it
/// unconditionally breaks startup on a fresh clone with no secrets configured.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EmailAddressIfAttribute(string conditionPropertyName, bool expectedValue = true)
    : ConditionalValidationAttribute(conditionPropertyName, expectedValue)
{
    private static readonly EmailAddressAttribute EmailAddress = new();

    protected override ValidationResult? ValidateWhenActive(
        object? value,
        ValidationContext validationContext)
    {
        // The "value is missing" case belongs to RequiredIf; only format matters here.
        if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
            return ValidationResult.Success;

        // A comma-separated list validates each address individually; a single
        // address just becomes a one-element list.
        if (value is string text)
        {
            var addresses = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return addresses.Length > 0 && addresses.All(EmailAddress.IsValid)
                ? ValidationResult.Success
                : Fail(validationContext);
        }

        return EmailAddress.IsValid(value)
            ? ValidationResult.Success
            : Fail(validationContext);
    }
}
