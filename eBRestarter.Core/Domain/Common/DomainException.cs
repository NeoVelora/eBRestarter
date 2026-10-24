namespace eBRestarter.Core.Domain.Common;

/// <summary>
/// Basisklasse für alle domänenspezifischen Ausnahmen in der eBRestarter-Domäne.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

