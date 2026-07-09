namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface IOutboundPortApplicationLogger<out TCategory>
#pragma warning restore S2326 // Unused type parameters should be removed
{
    void LogInformation(string message, params object?[] args);
    void LogWarning(string message, params object?[] args);
    void LogError(Exception? exception, string message, params object?[] args);
}
