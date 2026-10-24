namespace eBRestarter.Core.Domain.Services;

/// <summary>
/// Definiert Berechnungsoperationen für Neustartzeiten in der Domäne.
/// </summary>
public interface IRestartCalculationService
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}

