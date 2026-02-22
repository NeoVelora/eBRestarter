using eBRestarter.Core.Application.Interfaces;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

[SupportedOSPlatform("windows")]
public class WindowsPathService : IPathService
{
    private const string AppFolderName = "eBRestarter";
    private const string ConfigFileName = "eBRestarterConfig.json";

    public string GetAppDataPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Skylar", AppFolderName);
    }

    public string GetDownloadsPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    public string GetConfigFilePath()
    {
        return Path.Combine(GetAppDataPath(), ConfigFileName);
    }

    public string GetLogFilePath()
    {
        return Path.Combine(GetAppDataPath(), "log.txt");
    }
}
