using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

public class CaseInsensitiveCompareAttribute : ValidationAttribute
{
    private readonly string _otherProperty;

    public CaseInsensitiveCompareAttribute(string otherProperty)
    {
        _otherProperty = otherProperty;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectType.GetProperty(_otherProperty);

        if (otherProperty == null)
        {
            return new ValidationResult($"Property {_otherProperty} not found.");
        }

        var otherValue = otherProperty.GetValue(validationContext.ObjectInstance) as string;

        if (string.Equals(value as string, otherValue, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "Values do not match.");
    }
}