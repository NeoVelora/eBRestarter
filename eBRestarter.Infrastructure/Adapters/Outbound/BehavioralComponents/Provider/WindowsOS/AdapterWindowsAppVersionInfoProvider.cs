using System.Reflection;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS;

public sealed class AdapterWindowsAppVersionInfoProvider : IOutboundPortAppVersionInfoProvider
{
    private static readonly string FallbackVersion = new Version(1, 0, 0, 0).ToString();

    public string RetrieveAppVersion() =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? FallbackVersion;
}
