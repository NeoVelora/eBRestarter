using eBRestarter.Core.Application.Ports.Outbound.Logging;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Logging;

public sealed class MicrosoftExtensionsLoggerAdapter<TCategory>(ILogger<TCategory> logger) : IApplicationLoggerOutboundPort<TCategory>
{
    public void LogInformation(string message, params object?[] args) => logger.LogInformation(message, args);

    public void LogWarning(string message, params object?[] args) => logger.LogWarning(message, args);

    public void LogError(Exception? exception, string message, params object?[] args) => logger.LogError(exception, message, args);
}
