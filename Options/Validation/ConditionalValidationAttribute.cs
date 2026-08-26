using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ImaxWatcher.Options.Validation;

/// <summary>
/// Base for validations that apply only when another boolean property on the same
/// object holds a given value (typically <c>Enabled</c>).
/// </summary>
public abstract class ConditionalValidationAttribute(
    string conditionPropertyName,
    bool expectedValue = true) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var conditionProperty = validationContext.ObjectType.GetProperty(
            conditionPropertyName,
            BindingFlags.Instance | BindingFlags.Public);

        if (conditionProperty?.PropertyType != typeof(bool))
        {
            return new ValidationResult($"{conditionPropertyName} must be a boolean property.");
        }

        var conditionValue = (bool?)conditionProperty.GetValue(validationContext.ObjectInstance) ?? false;

        return conditionValue == expectedValue
            ? ValidateWhenActive(value, validationContext)
            : ValidationResult.Success;
    }

    /// <summary>Runs only when the condition is met.</summary>
    protected abstract ValidationResult? ValidateWhenActive(
        object? value,
        ValidationContext validationContext);

    protected ValidationResult Fail(ValidationContext validationContext)
    {
        var memberName = validationContext.MemberName ?? validationContext.DisplayName;

        // Without memberNames the aggregated message reads "members: ''".
        return new ValidationResult(
            FormatErrorMessage(memberName),
            memberName is null ? null : [memberName]);
    }
}
