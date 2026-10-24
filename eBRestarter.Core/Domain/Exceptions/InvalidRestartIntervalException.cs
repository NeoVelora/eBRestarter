namespace eBRestarter.Core.Domain.Exceptions;

using eBRestarter.Core.Domain.Common;

/// <summary>
/// Tritt auf, wenn ein ungültiges Neustart-Intervall angegeben wird.
/// </summary>
public sealed class InvalidRestartIntervalException : DomainException
{
    public int AttemptedIntervalDays { get; }

    public InvalidRestartIntervalException(int attemptedIntervalDays)
        : base($"The restart interval of {attemptedIntervalDays} day(s) is invalid.")
    {
        AttemptedIntervalDays = attemptedIntervalDays;
    }

    public InvalidRestartIntervalException(string message)
        : base(message)
    {
    }

    public InvalidRestartIntervalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

