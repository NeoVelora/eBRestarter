using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.Logging;

#pragma warning disable S6672 // Generic logger injection should match enclosing type
public sealed class AdapterMicrosoftExtensionsLogger<TCategory>(ILogger<TCategory> logger) : IOutboundPortApplicationLogger<TCategory>
#pragma warning restore S6672 // Generic logger injection should match enclosing type
{
    public ILogger<TCategory> Logger { get; } = logger;

    public void LogInformation(string message, params object?[] args) => Logger.LogInformation(message, args);

    public void LogWarning(string message, params object?[] args) => Logger.LogWarning(message, args);

    public void LogError(Exception? exception, string message, params object?[] args) => Logger.LogError(exception, message, args);
}
