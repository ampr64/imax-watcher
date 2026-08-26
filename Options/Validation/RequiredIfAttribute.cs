using System.ComponentModel.DataAnnotations;

namespace ImaxWatcher.Options.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class RequiredIfAttribute(string conditionPropertyName, bool expectedValue = true)
    : ConditionalValidationAttribute(conditionPropertyName, expectedValue)
{
    /// <summary>
    /// When <c>false</c>, a value type holding its default (0, default(T)) also counts as
    /// missing. Without this an <c>int</c>/<c>long</c> can never fail validation.
    /// </summary>
    public bool AllowDefault { get; init; } = true;

    protected override ValidationResult? ValidateWhenActive(
        object? value,
        ValidationContext validationContext)
    {
        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? Fail(validationContext)
                : ValidationResult.Success;
        }

        if (value is null)
        {
            return Fail(validationContext);
        }

        if (!AllowDefault)
        {
            var valueType = value.GetType();
            var defaultValue = valueType.IsValueType ? Activator.CreateInstance(valueType) : null;

            if (Equals(value, defaultValue))
            {
                return Fail(validationContext);
            }
        }

        return ValidationResult.Success;
    }
}
