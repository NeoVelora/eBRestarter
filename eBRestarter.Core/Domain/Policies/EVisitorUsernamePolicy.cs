using System.Text.RegularExpressions;

namespace eBRestarter.Core.Domain.Policies;

/// <summary>
/// Fachliche Richtlinie (Policy) für gültige eVisitor-Benutzernamen in der Domäne.
/// </summary>
public static partial class EVisitorUsernamePolicy
{
    private const int MaximumLength = 64;

    public const string RuleDescription = "The username may only contain letters, digits, dots, hyphens and underscores (1–64 characters).";

    public static bool IsValid(string? username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > MaximumLength)
        {
            return false;
        }

        return AllowedUsernameRegex().IsMatch(username);
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]+$", RegexOptions.NonBacktracking, matchTimeoutMilliseconds: 100)]
    private static partial Regex AllowedUsernameRegex();
}

