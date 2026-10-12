using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;

namespace eBRestarter.Infrastructure.BehavioralComponents.Factories;

public sealed class BrowserFactory : IOutboundPortBrowserFactory
{

    private const string UnsupportedBrowserErrorMessagePattern = "Browser '{0}' is not supported by {1}.";

    private static readonly FrozenDictionary<BrowserType, object> BoxedBrowserTypeKeys =
        Enum.GetValues<BrowserType>()
            .ToFrozenDictionary(
                static browserType => browserType,
                static browserType => (object)browserType,
                EqualityComparer<BrowserType>.Default);


    private readonly IServiceProvider _serviceProvider;


    public BrowserFactory(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }


    public IOutboundPortBrowser Create(BrowserType type)
    {
        if (BoxedBrowserTypeKeys.TryGetValue(type, out object? cachedKey)
            && _serviceProvider.GetKeyedService<IOutboundPortBrowser>(cachedKey) is { } keyedBrowser)
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
