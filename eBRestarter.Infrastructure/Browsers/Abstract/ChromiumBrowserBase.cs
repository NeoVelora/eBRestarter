using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Infrastructure.Browsers.Abstract
{


    public abstract class ChromiumBrowserBase : BrowserBase
    {
        protected ChromiumBrowserBase(IOperatingSystemFacade os, ILogger logger) : base(os, logger) { }

        protected abstract string ExeFileName { get; }
        protected abstract string BrowserRegistryName { get; }
        protected abstract string UninstallSubKey { get; }
        protected abstract string ProgramFilesSubPath { get; }

        protected abstract string ExtensionId { get; }

        public override bool IsExtensionInstalled(string? extensionId = null)
        {
            var idToCheck = string.IsNullOrEmpty(extensionId) ? ExtensionId : extensionId;

            if (string.IsNullOrEmpty(idToCheck)) return false;

            var paths = GetPaths();

            if (paths.ExtensionsDirs == null || paths.ExtensionsDirs.Count == 0) return false;

            foreach (var extensionsDir in paths.ExtensionsDirs)
            {
                if (!_os.WindowsFileSystemService.DirectoryExists(extensionsDir)) continue;


                if (extensionsDir.EndsWith(idToCheck, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var fullExtensionPath = _os.WindowsFileSystemService.CombinePaths(extensionsDir, idToCheck);
                if (_os.WindowsFileSystemService.DirectoryExists(fullExtensionPath))
                {
                    return true;
                }
            }

            return false;
        }

        protected override List<string> ExecutablePaths
        {
            get
            {
                var paths = new List<string>();

                var appPathKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{ExeFileName}";
                var appPath = _os.WindowsRegistryService.GetLocalMachineValue(appPathKey, "")?.ToString();
                if (!string.IsNullOrEmpty(appPath)) paths.Add(appPath);

                var clientKey = $@"SOFTWARE\Clients\StartMenuInternet\{BrowserRegistryName}\shell\open\command";
                var clientPath = _os.WindowsRegistryService.GetLocalMachineValue(clientKey, "")?.ToString();
                if (!string.IsNullOrEmpty(clientPath)) paths.Add(clientPath.Replace("\"", "").Trim());

                AddPathFromUninstallKey(paths, $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, true);
                AddPathFromUninstallKey(paths, $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, true);
                AddPathFromUninstallKey(paths, $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{UninstallSubKey}", ExeFileName, false);

                var pathPart = _os.WindowsFileSystemService.CombinePaths(ProgramFilesSubPath, ExeFileName);

                paths.Add(_os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("ProgramFiles"), pathPart));
                paths.Add(_os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("ProgramFiles(x86)"), pathPart));
                paths.Add(_os.WindowsFileSystemService.CombinePaths(_os.WindowsFileSystemService.GetEnvironmentPath("LocalAppData"), pathPart));

                return paths.Distinct().ToList();
            }
        }
    }


}
