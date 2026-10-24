using System;

namespace eBRestarter.Core.Application.Ports.Outbound.Logging;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface IOutboundPortApplicationLogger<out TCategory>
#pragma warning restore S2326 // Unused type parameters should be removed
{
    IDisposable? BeginScope(string key, object? value);

    void LogDebug(int eventId, string message, params object?[] args);

    void LogInformation(int eventId, string message, params object?[] args);

    void LogWarning(int eventId, string message, params object?[] args);

    void LogWarning(int eventId, Exception? exception, string message, params object?[] args);

    void LogError(int eventId, Exception? exception, string message, params object?[] args);

    void LogCritical(int eventId, Exception? exception, string message, params object?[] args);
}
