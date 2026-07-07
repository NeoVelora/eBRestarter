using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using System.Reflection;

namespace eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Providers;

public sealed class AdapterWindowsAppVersionInfoProvider : IOutboundPortAppVersionInfoProvider
{
    public string RetrieveAppVersion()
    {
        var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;

        if (assemblyVersion is not null)
        {
            return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}.{assemblyVersion.Revision}";
        }

        return new Version(1, 0, 0, 0).ToString();
    }
}


