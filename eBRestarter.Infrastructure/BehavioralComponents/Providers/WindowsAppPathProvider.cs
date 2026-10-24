using System;
using System.IO;
using System.Runtime.Versioning;

using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Application;

namespace eBRestarter.Infrastructure.BehavioralComponents.Providers;

[SupportedOSPlatform("windows")]
public sealed class WindowsAppPathProvider(
    IOutboundPortAppPathProvider pathProvider)
    : IInboundPortOsAppPathProvider
{

    private const string AppFolderName = "eBRestarter";
    private const string ConfigFileName = "eBRestarterConfig.json";
    private const string DownloadsFolderName = "Downloads";
    private const string LogFileName = "log.txt";
    private const string SkylarFolderName = "Skylar";


    private readonly IOutboundPortAppPathProvider _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));


    public string RetrieveAppDataPath()
    {
        var localAppData = _pathProvider.RetrieveLocalAppDataDirectory();
        return Path.Combine(localAppData, SkylarFolderName, AppFolderName);
    }

    public string RetrieveConfigFilePath() => Path.Combine(RetrieveAppDataPath(), ConfigFileName);

    public string RetrieveDownloadsPath() => Path.Combine(_pathProvider.RetrieveUserProfileDirectory(), DownloadsFolderName);

    public string RetrieveLogFilePath() => Path.Combine(RetrieveAppDataPath(), LogFileName);
}
