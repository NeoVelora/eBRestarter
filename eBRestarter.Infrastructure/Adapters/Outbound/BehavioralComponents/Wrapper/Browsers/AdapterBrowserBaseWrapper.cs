using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

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

    public virtual bool Start(string url, string arguments = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!TryRetrieveExecutablePath(out string? exePath))
        {
            LogBrowserExecutableMissing(_logger, Type);

            return false;
        }

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                LogStartingBrowser(_logger, Type, LogRedaction.MaskUrlUserSegment(url));
            }

            var quotedUrl = $"\"{url.Trim()}\"";

            var finalArguments = string.IsNullOrWhiteSpace(arguments)
                ? quotedUrl
                : $"{quotedUrl} {arguments}".Trim();

            _processControlPort.OpenUrlInBrowser(exePath, finalArguments);

            return true;
        }
        catch (Exception exception)
        {
            LogBrowserStartFailed(_logger, exception, Type);

            return false;
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
        catch (RegexMatchTimeoutException exception)
        {
            LogBrowserVersionParsingTimedOut(_logger, exception, Type);

            return DefaultVersion;
        }

        return raw;
    }

    protected bool TryRetrieveExecutablePath([NotNullWhen(true)] out string? executablePath)
    {
        executablePath = ExecutablePaths.FirstOrDefault(path => _fileSystemPort.FileExists(path));

        return executablePath is not null;
    }


    [LoggerMessage(
        EventId = LogEventIds.Browser.BrowserExecutableNotFound,
        Level = LogLevel.Warning,
        Message = "{BrowserType} was not started because no installed executable could be located.")]
    private static partial void LogBrowserExecutableMissing(ILogger logger, BrowserType browserType);

    [LoggerMessage(
        EventId = LogEventIds.Browser.BrowserStarting,
        Level = LogLevel.Debug,
        Message = "Starting {BrowserType} with URL {Url}...")]
    private static partial void LogStartingBrowser(ILogger logger, BrowserType browserType, string url);

    [LoggerMessage(
        EventId = LogEventIds.Browser.BrowserStartFailed,
        Level = LogLevel.Error,
        Message = "Error while starting {BrowserType}")]
    private static partial void LogBrowserStartFailed(ILogger logger, Exception exception, BrowserType browserType);

    [LoggerMessage(
        EventId = LogEventIds.Browser.BrowserVersionParsingTimedOut,
        Level = LogLevel.Warning,
        Message = "Regex matching timed out while evaluating browser version for {BrowserType}. Falling back to default version.")]
    private static partial void LogBrowserVersionParsingTimedOut(ILogger logger, Exception exception, BrowserType browserType);

    [GeneratedRegex(@"^[\d\.]+", RegexOptions.NonBacktracking, matchTimeoutMilliseconds: 100)]
    private static partial Regex VersionRegex();
}
