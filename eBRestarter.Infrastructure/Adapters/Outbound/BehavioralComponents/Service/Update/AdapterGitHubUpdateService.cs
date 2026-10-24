using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Update;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Security;
using eBRestarter.Core.Application.Ports.Outbound.Update;
using eBRestarter.Infrastructure.Api.Interfaces;
using eBRestarter.Infrastructure.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.ObjectArchetypes.Model;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Service.Update;

public sealed partial class AdapterGitHubUpdateService : IOutboundPortUpdate
{
    private const int DefaultVersionBuild = 0;
    private const int DefaultVersionMajor = 1;
    private const int DefaultVersionMinor = 0;
    private const string DefaultZeroVersionString = "0.0.0";
    private const string DownloadAndInstallFailedExceptionMessage = "Failed to download or install the application update.";
    private const string ExecutableFileExtension = ".exe";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
    private const string JsonPropertyAssets = "assets";
    private const string JsonPropertyBody = "body";
    private const string JsonPropertyBrowserDownloadUrl = "browser_download_url";
    private const string JsonPropertyName = "name";
    private const string JsonPropertyPublishedAt = "published_at";
    private const string JsonPropertyTagName = "tag_name";
    private const string MsiInstallerFileExtension = ".msi";
    private const string RepoName = "eBRestarter";
    private const string RepoOwner = "NeoVelora";
    private const string TempEnvironmentVariable = "TEMP";
    private const string UpdateFileNameFormat = "eBRestarter_Update_{0}.exe";
    private const char VersionTagPrefix = 'v';

    private const int MaximumUpdateJsonCharacterCount = 1024 * 1024;
    private const string UpdateStagingFolderFormat = "eBRestarter_Update_{0}";

    private static readonly JsonDocumentOptions StrictDocumentOptions = new()
    {
        MaxDepth = 32,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private static readonly string[] ApprovedDownloadHosts =
    [
        "github.com",
        "www.github.com",
        "objects.githubusercontent.com",
        "release-assets.githubusercontent.com"
    ];

    private readonly IOutboundPortApplicationLifetime _applicationLifetimeUseCase;
    private readonly IOutboundPortFileSystem _fileSystemPort;
    private readonly IOutboundPortHttpDownload _httpDownloadPort;
    private readonly ILogger<AdapterGitHubUpdateService> _logger;
    private readonly IOutboundPortOsProcessControl _processControlPort;
    private readonly IRestClient _restClientUseCase;
    private readonly IOutboundPortExecutableSignatureVerifier _signatureVerifier;


    public AdapterGitHubUpdateService(
        IOutboundPortApplicationLifetime applicationLifetimeUseCase,
        IOutboundPortFileSystem fileSystemPort,
        IOutboundPortHttpDownload httpDownloadPort,
        ILogger<AdapterGitHubUpdateService> logger,
        IOutboundPortOsProcessControl processControlPort,
        IRestClient restClientUseCase,
        IOutboundPortExecutableSignatureVerifier signatureVerifier)
    {
        ArgumentNullException.ThrowIfNull(applicationLifetimeUseCase);
        ArgumentNullException.ThrowIfNull(fileSystemPort);
        ArgumentNullException.ThrowIfNull(httpDownloadPort);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(processControlPort);
        ArgumentNullException.ThrowIfNull(restClientUseCase);
        ArgumentNullException.ThrowIfNull(signatureVerifier);

        _applicationLifetimeUseCase = applicationLifetimeUseCase;
        _fileSystemPort = fileSystemPort;
        _httpDownloadPort = httpDownloadPort;
        _logger = logger;
        _processControlPort = processControlPort;
        _restClientUseCase = restClientUseCase;
        _signatureVerifier = signatureVerifier;
    }


    public async Task<UpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(DefaultVersionMajor, DefaultVersionMinor, DefaultVersionBuild);

        var request = new ApiRequest
        {
            Url = GitHubApiUrl,
            Method = ObjectArchetypes.Enums.HttpMethod.GET
        };

        var response = await _restClientUseCase.ExecuteGetAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
        {
            LogUpdateCheckFailed(_logger, response.StatusCode);

            return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
        }

        if (response.Content.Length > MaximumUpdateJsonCharacterCount)
        {
            LogUpdatePayloadTooLarge(_logger);

            return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
        }

        try
        {
            using var doc = JsonDocument.Parse(response.Content, StrictDocumentOptions);
            var root = doc.RootElement;

            string tagName = root.GetProperty(JsonPropertyTagName).GetString() ?? DefaultZeroVersionString;
            var cleanVersion = tagName.TrimStart(VersionTagPrefix);

            if (!Version.TryParse(cleanVersion, out Version? latestVersion))
            {
                return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
            }

            var downloadUrl = string.Empty;

            if (root.TryGetProperty(JsonPropertyAssets, out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty(JsonPropertyName).GetString() ?? string.Empty;

                    if (name.EndsWith(ExecutableFileExtension, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith(MsiInstallerFileExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty(JsonPropertyBrowserDownloadUrl).GetString() ?? string.Empty;
                        break;
                    }
                }
            }

            bool updateAvailable = latestVersion > currentVersion;

            return new UpdateInfo
            {
                IsUpdateAvailable = updateAvailable,
                CurrentVersion = currentVersion.ToString(),
                LatestVersion = cleanVersion,
                DownloadUrl = downloadUrl,
                Changelog = root.GetProperty(JsonPropertyBody).GetString() ?? string.Empty,
                PublishedAt = root.GetProperty(JsonPropertyPublishedAt).GetDateTime()
            };
        }
        catch (Exception exception)
        {
            LogUpdateResponseParsingFailed(_logger, exception);

            return new UpdateInfo { IsUpdateAvailable = false };
        }
    }

    public Task DownloadAndInstallAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        return DownloadAndInstallCoreAsync(updateInfo, cancellationToken);
    }

    private async Task DownloadAndInstallCoreAsync(UpdateInfo updateInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
        {
            LogUpdateDownloadUrlMissing(_logger);
            return;
        }

        if (!IsApprovedDownloadUrl(updateInfo.DownloadUrl))
        {
            return;
        }

        try
        {
            var tempDir = _fileSystemPort.ResolveEnvironmentPath(TempEnvironmentVariable);
            var stagingFolderName = string.Format(CultureInfo.InvariantCulture, UpdateStagingFolderFormat, Guid.NewGuid().ToString("N"));
            var stagingDirectory = _fileSystemPort.CombinePaths(tempDir, stagingFolderName);

            _fileSystemPort.CreateDirectory(stagingDirectory);

            var updateFileName = string.Format(CultureInfo.InvariantCulture, UpdateFileNameFormat, updateInfo.LatestVersion);
            var tempFile = _fileSystemPort.CombinePaths(stagingDirectory, updateFileName);

            await _httpDownloadPort.DownloadFileAsync(
                updateInfo.DownloadUrl,
                tempFile,
                new Progress<DownloadProgressStatus>(),
                cancellationToken).ConfigureAwait(false);

            LogUpdateDownloaded(_logger, tempFile);

            if (!_signatureVerifier.IsTrustedPublisher(tempFile))
            {
                LogSignatureVerificationRejected(_logger, tempFile);
                _fileSystemPort.DeleteFile(tempFile);

                return;
            }

            await _processControlPort.StartExecutableAsync(tempFile).ConfigureAwait(false);

            _applicationLifetimeUseCase.ExitApplication(0);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(DownloadAndInstallFailedExceptionMessage, exception);
        }
    }

    private bool IsApprovedDownloadUrl(string downloadUrl)
    {
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            LogDownloadSchemeRejected(_logger, downloadUrl);
            return false;
        }

        if (Array.Exists(ApprovedDownloadHosts, approvedHost => string.Equals(uri.Host, approvedHost, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        LogDownloadHostRejected(_logger, downloadUrl);

        return false;
    }


    [LoggerMessage(
        EventId = LogEventIds.Update.UpdateCheckFailed,
        Level = LogLevel.Warning,
        Message = "Update check failed: {Status}")]
    private static partial void LogUpdateCheckFailed(ILogger logger, ResponseCode status);

    [LoggerMessage(
        EventId = LogEventIds.Update.UpdatePayloadTooLarge,
        Level = LogLevel.Warning,
        Message = "GitHub release payload exceeds maximum allowed size of 1 MB.")]
    private static partial void LogUpdatePayloadTooLarge(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.Update.UpdateResponseParsingFailed,
        Level = LogLevel.Error,
        Message = "Error parsing the GitHub response.")]
    private static partial void LogUpdateResponseParsingFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.Update.UpdateDownloadUrlMissing,
        Level = LogLevel.Error,
        Message = "No download URL found in update information.")]
    private static partial void LogUpdateDownloadUrlMissing(ILogger logger);

    [LoggerMessage(
        EventId = LogEventIds.Update.UpdateDownloaded,
        Level = LogLevel.Information,
        Message = "Update downloaded to: {Path}")]
    private static partial void LogUpdateDownloaded(ILogger logger, string path);

    [LoggerMessage(
        EventId = LogEventIds.Security.SignatureVerificationRejected,
        Level = LogLevel.Error,
        Message = "Update aborted: the downloaded package {Path} does not carry a valid, trusted code signature. The file was deleted and NOT executed.")]
    private static partial void LogSignatureVerificationRejected(ILogger logger, string path);

    [LoggerMessage(
        EventId = LogEventIds.Security.DownloadSchemeRejected,
        Level = LogLevel.Error,
        Message = "Refusing update download: {Url} does not use HTTPS.")]
    private static partial void LogDownloadSchemeRejected(ILogger logger, string url);

    [LoggerMessage(
        EventId = LogEventIds.Security.DownloadHostRejected,
        Level = LogLevel.Error,
        Message = "Refusing update download: {Url} does not point to an approved GitHub release host.")]
    private static partial void LogDownloadHostRejected(ILogger logger, string url);
}
