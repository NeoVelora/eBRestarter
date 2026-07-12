using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Infrastructure.Adapters.Outbound.Browsers;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Infrastructure.Factories;

public sealed class BrowserFactory(IServiceProvider serviceProvider) : IOutboundPortBrowserFactory
{
    public IOutboundPortBrowser Create(BrowserType type)
    {
        if (serviceProvider.GetKeyedService<IOutboundPortBrowser>(type) is { } keyedBrowser)
        {
            return keyedBrowser;
        }

        return type switch
        {
            BrowserType.Chrome => serviceProvider.GetRequiredService<AdapterChromeBrowser>(),
            BrowserType.Firefox => serviceProvider.GetRequiredService<AdapterFirefoxBrowser>(),
            BrowserType.Edge => serviceProvider.GetRequiredService<AdapterEdgeBrowser>(),
            BrowserType.Brave => serviceProvider.GetRequiredService<AdapterBraveBrowser>(),
            BrowserType.Vivaldi => serviceProvider.GetRequiredService<AdapterVivaldiBrowser>(),
            _ => throw new NotSupportedException($"Browser {type} ist noch nicht implementiert.")
        };
    }
}


