namespace eBRestarter.Core.Application.Ports.Outbound.Logging;

public interface IApplicationLoggerOutboundPort<out TCategory>
{
    void LogInformation(string message, params object?[] args);
    void LogWarning(string message, params object?[] args);
    void LogError(Exception? exception, string message, params object?[] args);
}
