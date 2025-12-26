using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models.Records;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Infrastructure.Browsers.Abstract
{
    public abstract class BrowserBase : IBrowser
    {
        protected readonly IOperatingSystemFacade _os;
        protected readonly ILogger _logger;

        protected BrowserBase(IOperatingSystemFacade os, ILogger logger)
        {
            _os = os;
            _logger = logger;
        }

        public abstract BrowserType Type { get; }

        protected abstract string ProcessName { get; }
        protected abstract string RegistryKeyVersion { get; }
        protected abstract List<string> ExecutablePaths { get; }

        public virtual void Start(string url, string arguments = "")
        {
            try
            {
                var exePath = GetExecutablePath();
                _logger.LogInformation($"Starte {Type}...");

                _os.WindowsProcessControlService.StartExecutable(exePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Starten von {Type}");
            }
        }

        public virtual void Close()
        {
            _os.WindowsProcessControlService.CloseApplication(ProcessName);
        }

        public virtual string BrowserVersion
        {
            get
            {
                var raw = _os.WindowsRegistryService.GetLocalMachineValue(RegistryKeyVersion, "CurrentVersion")
                       ?? _os.WindowsRegistryService.GetCurrentUserValue(RegistryKeyVersion, "version");

                return CleanVersionString(raw?.ToString());
            }
        }

        public bool IsInstalled => ExecutablePaths.Any(path => _os.WindowsFileSystemService.FileExists(path));

        protected string GetExecutablePath()
        {
            return ExecutablePaths.FirstOrDefault(path => _os.WindowsFileSystemService.FileExists(path))
                   ?? throw new FileNotFoundException($"{Type} executable not found.");
        }

        public abstract BrowserPaths GetPaths();

        protected virtual string CleanVersionString(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Unknown";
            return System.Text.RegularExpressions.Regex.Replace(raw, "[^0-9.]", "");
        }
    }
}
