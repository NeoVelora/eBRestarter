using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;

public abstract partial class AdapterBrowserBaseWrapper(
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortSystemConfigurationRepository settingsPort,
    IOutboundPortFileSystem fileSystemPort,
    ILogger logger)
    : IOutboundPortBrowser
{

    private const string CurrentVersionValueName = "CurrentVersion";
    private const string DefaultVersion = "Unknown";
    private const string InstallLocationValueName = "InstallLocation";
    private const string VersionValueName = "version";


    protected readonly IOutboundPortFileSystem _fileSystemPort = fileSystemPort;
    protected readonly ILogger _logger = logger;
    protected readonly IOutboundPortOsProcessControl _processControlPort = processControlPort;
    protected readonly IOutboundPortSystemConfigurationRepository _settingsPort = settingsPort;


    public bool IsInstalled => ExecutablePaths.Any(path => _fileSystemPort.FileExists(path));

    public virtual string BrowserVersion
    {
        get
        {
            var rawVersionValue = _settingsPort.GetUserValue(RegistryKeyVersion, VersionValueName)
                                  ?? _settingsPort.GetUserValue(RegistryKeyVersion, CurrentVersionValueName);

            return CleanVersionString(rawVersionValue?.ToString());
        }
    }

    public abstract string DisplayName { get; }
    public abstract string DownloadUrl { get; }
    public abstract string ExtensionInstallUrl { get; }
    public abstract string IconPath { get; }
    public abstract string ProcessName { get; }
    protected abstract string RegistryKeyVersion { get; }

    public abstract BrowserType Type { get; }

    protected abstract List<string> ExecutablePaths { get; }


    public virtual void Close()
    {
        _processControlPort.CloseApplication(ProcessName);
    }

    public abstract bool IsExtensionInstalled(string? extensionId = null);

    public abstract BrowserPaths ResolvePaths();

    public virtual void Start(string url, string arguments = "")
    {
        try
        {
            var exePath = RetrieveExecutablePath();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Starting {BrowserType} with URL {Url}...", Type, url);
            }

            var finalArguments = string.IsNullOrWhiteSpace(arguments)
                ? url.Trim()
                : $"{url} {arguments}".Trim();

            _processControlPort.OpenUrlInBrowser(exePath, finalArguments);
        }
        catch (Exception exception)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(exception, "Error while starting {BrowserType}", Type);
            }
        }
    }

    protected void AddPathFromUninstallKey(
        List<string> paths,
        string subKey,
        string exeName,
        bool isHklm)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var installLocationValue = isHklm
            ? _settingsPort.GetSystemValue(subKey, InstallLocationValueName)
            : _settingsPort.GetUserValue(subKey, InstallLocationValueName);

        if (installLocationValue?.ToString() is not { Length: > 0 } installLocation)
        {
            return;
        }

        var fullPath = _fileSystemPort.CombinePaths(installLocation, exeName);
        paths.Add(fullPath);
    }

    protected virtual string CleanVersionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultVersion;
        }

        var cleanRaw = raw.Trim();

        try
        {
            var match = VersionRegex().Match(cleanRaw);

            if (match.Success)
            {
                return match.Value;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return DefaultVersion;
        }

        return raw;
    }

    protected string RetrieveExecutablePath()
    {
        return ExecutablePaths.FirstOrDefault(path => _fileSystemPort.FileExists(path))
               ?? throw new FileNotFoundException($"{Type} executable not found.");
    }

    [GeneratedRegex(@"^[\d\.]+", RegexOptions.NonBacktracking, matchTimeoutMilliseconds: 100)]
    private static partial Regex VersionRegex();
}
