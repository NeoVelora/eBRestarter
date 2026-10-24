using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.Ports.Outbound.Application;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Handler.WindowsOS;

public sealed class AdapterWindowsApplicationLifetimeHandler(IServiceProvider? serviceProvider = null) : IOutboundPortApplicationLifetime
{
    private readonly IServiceProvider? _serviceProvider = serviceProvider;

    public void ExitApplication(int exitCode)
    {
        try
        {
            var loggerFactory = _serviceProvider?.GetService(typeof(ILoggerFactory)) as IDisposable;
            loggerFactory?.Dispose();

            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
        catch (Exception ex)
        {
            _ = ex;
        }

        Environment.Exit(exitCode);
    }
}
