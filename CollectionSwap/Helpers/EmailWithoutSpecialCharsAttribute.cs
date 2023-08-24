using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class EmailWithoutSpecialCharsAttribute : ValidationAttribute
{
    private static readonly string AllowedSpecialCharsPattern = @"^[a-zA-Z0-9._\-\+]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    private static readonly Regex RegexValidator = new Regex(AllowedSpecialCharsPattern);

    public override bool IsValid(object value)
    {
        if (value == null)
            return true;

        string email = value.ToString();
        return RegexValidator.IsMatch(email);
    }
}
