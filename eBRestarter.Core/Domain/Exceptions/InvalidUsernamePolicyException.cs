namespace eBRestarter.Core.Domain.Exceptions;

using eBRestarter.Core.Domain.Common;

/// <summary>
/// Tritt auf, wenn ein Benutzername gegen die Domänen-Richtlinie verstößt.
/// </summary>
public sealed class InvalidUsernamePolicyException : DomainException
{
    public string? AttemptedUsername { get; }

    public InvalidUsernamePolicyException(string message)
        : base(message)
    {
    }

    public InvalidUsernamePolicyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InvalidUsernamePolicyException(string? attemptedUsername, string message)
        : base(message)
    {
        AttemptedUsername = attemptedUsername;
    }

    public static InvalidUsernamePolicyException ForUsername(string? attemptedUsername) =>
        new(attemptedUsername, $"The username '{attemptedUsername ?? "<null>"}' does not comply with the domain username policy.");
}

