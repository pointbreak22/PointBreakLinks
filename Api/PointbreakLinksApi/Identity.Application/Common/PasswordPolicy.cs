using System.Text.RegularExpressions;

namespace Identity.Application.Common;

public static partial class PasswordPolicy
{
    [GeneratedRegex(@"^(?=.*[A-Z])(?=.*\d).{8,}$")]
    private static partial Regex Pattern();

    public static bool IsValid(string password) => Pattern().IsMatch(password);
}
