using eBRestarter.Core.Application.Interfaces;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

[SupportedOSPlatform("windows")]
public class WindowsPathService(IPathProvider pathProvider) : IPathService
{
    private readonly IPathProvider _pathProvider = pathProvider;

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
