using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ImaxWatcher.Options.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class GreaterThanOrEqualToAttribute(string otherPropertyName) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectType.GetProperty(otherPropertyName, BindingFlags.Instance | BindingFlags.Public);
        if (otherProperty is null)
            return new ValidationResult($"Unknown property: {otherPropertyName}.");

        var otherValue = otherProperty.GetValue(validationContext.ObjectInstance);
        if (value is null || otherValue is null)
            return ValidationResult.Success;

        if (value is IComparable comparable)
        {
            return comparable.CompareTo(otherValue) < 0
                ? new ValidationResult(FormatErrorMessage(validationContext.MemberName ?? validationContext.DisplayName))
                : ValidationResult.Success;
        }

        return new ValidationResult($"{validationContext.MemberName} must implement IComparable.");
    }
}
