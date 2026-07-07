using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Providers;

[SupportedOSPlatform("windows")]
public sealed class WindowsAppPathProvider(IAppPathProviderOutboundPort pathProvider) : IInboundPortOsAppPathProvider
{
    private readonly IAppPathProviderOutboundPort _pathProvider = pathProvider;

    private const string AppFolderName = "eBRestarter";
    private const string ConfigFileName = "eBRestarterConfig.json";

    public string RetrieveAppDataPath()
    {
        string localAppData = _pathProvider.RetrieveLocalAppDataDirectory();

        return Path.Combine(localAppData, "Skylar", AppFolderName);
    }

    public string RetrieveDownloadsPath()
    {
        return Path.Combine(_pathProvider.RetrieveUserProfileDirectory(), "Downloads");
    }

    public string RetrieveConfigFilePath()
    {
        return Path.Combine(RetrieveAppDataPath(), ConfigFileName);
    }

    public string RetrieveLogFilePath()
    {
        return Path.Combine(RetrieveAppDataPath(), "log.txt");
    }
}


