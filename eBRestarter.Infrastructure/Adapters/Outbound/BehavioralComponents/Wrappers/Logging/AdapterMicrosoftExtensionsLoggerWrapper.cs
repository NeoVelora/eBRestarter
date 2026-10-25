using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.Ports.Outbound.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Logging;

#pragma warning disable S6672 // Generic logger injection should match enclosing type
public sealed class AdapterMicrosoftExtensionsLoggerWrapper<TCategory>(
    ILogger<TCategory> logger)
    : IOutboundPortApplicationLogger<TCategory>
#pragma warning restore S6672 // Generic logger injection should match enclosing type
{

    private readonly ILogger<TCategory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    public IDisposable? BeginScope(string key, object? value)
        => _logger.BeginScope(new Dictionary<string, object?> { [key] = value });

#pragma warning disable CA1848 // Adapter bridges dynamic port calls to Microsoft.Extensions.Logging
#pragma warning disable CA2254 // Template should be a static expression
        public void LogCritical(int eventId, Exception? exception, string message, params object?[] args)
        => _logger.LogCritical(new EventId(eventId), exception, message, args);

    public void LogDebug(int eventId, string message, params object?[] args)
        => _logger.LogDebug(new EventId(eventId), message, args);

    public void LogError(int eventId, Exception? exception, string message, params object?[] args)
        => _logger.LogError(new EventId(eventId), exception, message, args);

    public void LogInformation(int eventId, string message, params object?[] args)
        => _logger.LogInformation(new EventId(eventId), message, args);

    public void LogWarning(int eventId, string message, params object?[] args)
        => _logger.LogWarning(new EventId(eventId), message, args);

    public void LogWarning(int eventId, Exception? exception, string message, params object?[] args)
        => _logger.LogWarning(new EventId(eventId), exception, message, args);
#pragma warning restore CA2254 // Template should be a static expression
#pragma warning restore CA1848 // Use the LoggerMessage delegates
}
