using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Update;
using eBRestarter.Infrastructure.Api.Interfaces;
using eBRestarter.Infrastructure.ObjectArchetypes.Model;

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

    private readonly IOutboundPortApplicationLifetime _applicationLifetimeUseCase;
    private readonly IOutboundPortFileSystem _fileSystemPort;
    private readonly IOutboundPortHttpDownload _httpDownloadPort;
    private readonly ILogger<AdapterGitHubUpdateService> _logger;
    private readonly IOutboundPortOsProcessControl _processControlPort;
    private readonly IRestClient _restClientUseCase;


    public AdapterGitHubUpdateService(
        IOutboundPortApplicationLifetime applicationLifetimeUseCase,
        IOutboundPortFileSystem fileSystemPort,
        IOutboundPortHttpDownload httpDownloadPort,
        ILogger<AdapterGitHubUpdateService> logger,
        IOutboundPortOsProcessControl processControlPort,
        IRestClient restClientUseCase)
    {
        ArgumentNullException.ThrowIfNull(applicationLifetimeUseCase);
        ArgumentNullException.ThrowIfNull(fileSystemPort);
        ArgumentNullException.ThrowIfNull(httpDownloadPort);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(processControlPort);
        ArgumentNullException.ThrowIfNull(restClientUseCase);

        _applicationLifetimeUseCase = applicationLifetimeUseCase;
        _fileSystemPort = fileSystemPort;
        _httpDownloadPort = httpDownloadPort;
        _logger = logger;
        _processControlPort = processControlPort;
        _restClientUseCase = restClientUseCase;
    }


    public async Task<UpdateInfo> CheckForUpdateAsync()
    {
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(DefaultVersionMajor, DefaultVersionMinor, DefaultVersionBuild);

        var request = new ApiRequest
        {
            Url = GitHubApiUrl,
            Method = ObjectArchetypes.Enums.HttpMethod.GET
        };

        var response = await _restClientUseCase.ExecuteGetAsync(request).ConfigureAwait(false);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
        {
            _logger.LogWarning(UpdateCheckFailedLogMessage, response.StatusCode);

            return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
        }

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
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
            _logger.LogError(exception, ErrorParsingGitHubResponseLogMessage);

            return new UpdateInfo { IsUpdateAvailable = false };
        }
    }

    public Task DownloadAndInstallAsync(UpdateInfo updateInfo)
    {
        ArgumentNullException.ThrowIfNull(updateInfo);

        return DownloadAndInstallCoreAsync(updateInfo);
    }

    private async Task DownloadAndInstallCoreAsync(UpdateInfo updateInfo)
    {
        if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
        {
            _logger.LogError(NoDownloadUrlFoundLogMessage);
            return;
        }

        try
        {
            var tempDir = _fileSystemPort.ResolveEnvironmentPath(TempEnvironmentVariable);
            var updateFileName = string.Format(UpdateFileNameFormat, updateInfo.LatestVersion);
            var tempFile = _fileSystemPort.CombinePaths(tempDir, updateFileName);

            await _httpDownloadPort.DownloadFileAsync(
                updateInfo.DownloadUrl,
                tempFile,
                new Progress<DownloadProgressStatus>(),
                CancellationToken.None).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(UpdateDownloadedLogMessage, tempFile);
            }

            await _processControlPort.StartExecutableAsync(tempFile).ConfigureAwait(false);

            _applicationLifetimeUseCase.ExitApplication(0);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(DownloadAndInstallFailedExceptionMessage, exception);
        }
    }
}
