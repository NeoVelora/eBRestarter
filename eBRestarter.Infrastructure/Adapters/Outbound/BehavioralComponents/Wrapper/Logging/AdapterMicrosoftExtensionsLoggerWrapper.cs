using System;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Logging;

#pragma warning disable S6672 // Generic logger injection should match enclosing type
public sealed class AdapterMicrosoftExtensionsLoggerWrapper<TCategory>(
    ILogger<TCategory> logger)
    : IOutboundPortApplicationLogger<TCategory>
#pragma warning restore S6672 // Generic logger injection should match enclosing type
{

    private readonly ILogger<TCategory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


#pragma warning disable CA2254 // Template should be a static expression
    public void LogError(Exception? exception, string message, params object?[] args) => _logger.LogError(exception, message, args);

    public void LogInformation(string message, params object?[] args) => _logger.LogInformation(message, args);

    public void LogWarning(string message, params object?[] args) => _logger.LogWarning(message, args);
#pragma warning restore CA2254 // Template should be a static expression
}
