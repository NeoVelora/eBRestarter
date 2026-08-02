using System;
using Microsoft.Extensions.DependencyInjection;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;

namespace eBRestarter.Infrastructure.BehavioralComponents.Factories;

public sealed class BrowserFactory(
    IServiceProvider serviceProvider)
    : IOutboundPortBrowserFactory
{

    private const string UnsupportedBrowserErrorMessagePattern = "Browser '{0}' is not supported by {1}.";


    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));


    public IOutboundPortBrowser Create(BrowserType type)
    {
        if (_serviceProvider.GetKeyedService<IOutboundPortBrowser>(type) is { } keyedBrowser)
        {
            return keyedBrowser;
        }

        return type switch
        {
            BrowserType.Chrome => _serviceProvider.GetRequiredService<AdapterChromeBrowserWrapper>(),
            BrowserType.Firefox => _serviceProvider.GetRequiredService<AdapterFirefoxBrowserWrapper>(),
            BrowserType.Edge => _serviceProvider.GetRequiredService<AdapterEdgeBrowserWrapper>(),
            BrowserType.Brave => _serviceProvider.GetRequiredService<AdapterBraveBrowserWrapper>(),
            BrowserType.Vivaldi => _serviceProvider.GetRequiredService<AdapterVivaldiBrowserWrapper>(),
            _ => throw new NotSupportedException(string.Format(UnsupportedBrowserErrorMessagePattern, type, nameof(BrowserFactory)))
        };
    }
}
