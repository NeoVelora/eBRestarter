using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Browsers.Abstract;

public abstract class ChromiumBrowserBaseAdapter(IOperatingSystemFacade os, ILogger logger) : BrowserBaseAdapter(os, logger)
{

    protected abstract string ExeFileName { get; }
    protected abstract string BrowserRegistryName { get; }
    protected abstract string UninstallSubKey { get; }
    protected abstract string ProgramFilesSubPath { get; }

    protected abstract string ExtensionId { get; }

    protected override List<string> ExecutablePaths
    {
        get
        {
            var paths = new List<string>();

            var appPathKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{ExeFileName}";
            var appPath = _os.WindowsRegistryAdapter.RetrieveLocalMachineValue(appPathKey, "")?.ToString();

            if (!string.IsNullOrEmpty(appPath)) { paths.Add(appPath); }

            var clientKey = $@"SOFTWARE\Clients\StartMenuInternet\{BrowserRegistryName}\shell\open\command";
            var clientPath = _os.WindowsRegistryAdapter.RetrieveLocalMachineValue(clientKey, "")?.ToString();

            if (!string.IsNullOrEmpty(clientPath)) { paths.Add(clientPath.Replace("\"", "").Trim()); }

            AddPathFromUninstallKey(paths, $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, true);
            AddPathFromUninstallKey(paths, $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, true);
            AddPathFromUninstallKey(paths, $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, false);

            var pathPart = _os.WindowsFileSystemServiceAdapter.CombinePaths(ProgramFilesSubPath, ExeFileName);

            paths.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(_os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("ProgramFiles"), pathPart));
            paths.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(_os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("ProgramFiles(x86)"), pathPart));
            paths.Add(_os.WindowsFileSystemServiceAdapter.CombinePaths(_os.WindowsFileSystemServiceAdapter.ResolveEnvironmentPath("LocalAppData"), pathPart));

            return [.. paths.Distinct()];
        }
    }

    public override bool IsExtensionInstalled(string? extensionId = null)
    {
        var idToCheck = string.IsNullOrEmpty(extensionId) ? ExtensionId : extensionId;

        if (string.IsNullOrEmpty(idToCheck)) return false;

        var paths = ResolvePaths();

        if (paths.ExtensionsDirs == null || paths.ExtensionsDirs.Count == 0) return false;

        foreach (var extensionsDir in paths.ExtensionsDirs)
        {
            if (!_os.WindowsFileSystemServiceAdapter.DirectoryExists(extensionsDir)) continue;


            if (extensionsDir.EndsWith(idToCheck, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var fullExtensionPath = _os.WindowsFileSystemServiceAdapter.CombinePaths(extensionsDir, idToCheck);

            if (_os.WindowsFileSystemServiceAdapter.DirectoryExists(fullExtensionPath))
            {
                return true;
            }
        }

        return false;
    }
}


