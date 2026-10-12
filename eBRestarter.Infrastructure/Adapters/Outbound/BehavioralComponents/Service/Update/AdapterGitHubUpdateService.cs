using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Update;
using eBRestarter.Infrastructure.Api.Interfaces;
using eBRestarter.Infrastructure.ObjectArchetypes.Model;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Service.Update;

public sealed class AdapterGitHubUpdateService : IOutboundPortUpdate
{
    private const int DefaultVersionBuild = 0;
    private const int DefaultVersionMajor = 1;
    private const int DefaultVersionMinor = 0;
    private const string DefaultZeroVersionString = "0.0.0";
    private const string DownloadAndInstallFailedExceptionMessage = "Failed to download or install the application update.";
    private const string ErrorParsingGitHubResponseLogMessage = "Error parsing the GitHub response.";
    private const string ExecutableFileExtension = ".exe";
    private const string GitHubApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
    private const string JsonPropertyAssets = "assets";
    private const string JsonPropertyBody = "body";
    private const string JsonPropertyBrowserDownloadUrl = "browser_download_url";
    private const string JsonPropertyName = "name";
    private const string JsonPropertyPublishedAt = "published_at";
    private const string JsonPropertyTagName = "tag_name";
    private const string MsiInstallerFileExtension = ".msi";
    private const string NoDownloadUrlFoundLogMessage = "No download URL found in update information.";
    private const string RepoName = "eBRestarter";
    private const string RepoOwner = "NeoVelora";
    private const string TempEnvironmentVariable = "TEMP";
    private const string UpdateCheckFailedLogMessage = "Update check failed: {Status}";
    private const string UpdateDownloadedLogMessage = "Update downloaded to: {Path}";
    private const string UpdateFileNameFormat = "eBRestarter_Update_{0}.exe";
    private const char VersionTagPrefix = 'v';

    private const int MaximumUpdateJsonCharacterCount = 1024 * 1024;
    private const string UpdatePayloadTooLargeLogMessage = "GitHub release payload exceeds maximum allowed size of 1 MB.";

    private static readonly JsonDocumentOptions StrictDocumentOptions = new()
    {
        MaxDepth = 32,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private const string RejectedDownloadHostLogMessage = "Refusing update download: {Url} does not point to an approved GitHub release host.";
    private const string RejectedDownloadSchemeLogMessage = "Refusing update download: {Url} does not use HTTPS.";
    private const string SignatureRejectedAbortLogMessage = "Update aborted: the downloaded package {Path} does not carry a valid, trusted code signature. The file was deleted and NOT executed.";
    private const string UpdateStagingFolderFormat = "eBRestarter_Update_{0}";

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
            _logger.LogWarning(LogEventIds.Update.UpdateCheckFailed, UpdateCheckFailedLogMessage, response.StatusCode);

            return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
        }

        if (response.Content.Length > MaximumUpdateJsonCharacterCount)
        {
            _logger.LogWarning(LogEventIds.Update.UpdateResponseParsingFailed, UpdatePayloadTooLargeLogMessage);

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
            _logger.LogError(LogEventIds.Update.UpdateResponseParsingFailed, exception, ErrorParsingGitHubResponseLogMessage);

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
            _logger.LogError(LogEventIds.Update.UpdateDownloadUrlMissing, NoDownloadUrlFoundLogMessage);
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

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(LogEventIds.Update.UpdateDownloaded, UpdateDownloadedLogMessage, tempFile);
            }

            if (!_signatureVerifier.IsTrustedPublisher(tempFile))
            {
                _logger.LogError(LogEventIds.Security.SignatureVerificationRejected, SignatureRejectedAbortLogMessage, tempFile);
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
            _logger.LogError(LogEventIds.Security.DownloadSchemeRejected, RejectedDownloadSchemeLogMessage, downloadUrl);
            return false;
        }

        if (Array.Exists(ApprovedDownloadHosts, approvedHost => string.Equals(uri.Host, approvedHost, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        _logger.LogError(LogEventIds.Security.DownloadHostRejected, RejectedDownloadHostLogMessage, downloadUrl);

        return false;
    }
}
