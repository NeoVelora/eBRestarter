using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Update;
using eBRestarter.Infrastructure.Api.Interfaces;
using eBRestarter.Infrastructure.ObjectArchetypes.Model;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace eBRestarter.Infrastructure.Adapters.Outbound.Update;

public sealed class AdapterGitHubUpdate(
    IRestClient restClientUseCase,
    IOutboundPortHttpDownload httpDownloadPort,
    IOutboundPortOsProcessControl processControlPort,
    IOutboundPortFileSystem fileSystemPort,
    IOutboundPortApplicationLifetime applicationLifetimeUseCase,
    ILogger<AdapterGitHubUpdate> logger) : IOutboundPortUpdate
{
    private const string RepoOwner = "NeoVelora";
    private const string RepoName = "eBRestarter";

    private const string GitHubApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

    private readonly IRestClient _restClientUseCase = restClientUseCase;
    private readonly IOutboundPortHttpDownload _httpDownloadPort = httpDownloadPort;
    private readonly IOutboundPortFileSystem _fileSystemPort = fileSystemPort;
    private readonly IOutboundPortOsProcessControl _processControlPort = processControlPort;
    private readonly IOutboundPortApplicationLifetime _applicationLifetimeUseCase = applicationLifetimeUseCase;
    private readonly ILogger<AdapterGitHubUpdate> _logger = logger;

    public async Task<UpdateInfo> CheckForUpdateAsync()
    {
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);

        var request = new ApiRequest
        {
            Url = GitHubApiUrl,
            Method = ObjectArchetypes.Enums.HttpMethod.GET
        };

        var response = await _restClientUseCase.ExecuteGetAsync(request);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
        {
            _logger.LogWarning("Update check failed: {Status}", response.StatusCode);

            return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
        }

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            string tagName = root.GetProperty("tag_name").GetString() ?? "0.0.0";
            var cleanVersion = tagName.TrimStart('v');

            if (!Version.TryParse(cleanVersion, out Version? latestVersion))
            {
                return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
            }

            var downloadUrl = string.Empty;

            if (root.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";

                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
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
                Changelog = root.GetProperty("body").GetString() ?? "",
                PublishedAt = root.GetProperty("published_at").GetDateTime()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing the GitHub response.");

            return new UpdateInfo { IsUpdateAvailable = false };
        }
    }

    public async Task DownloadAndInstallAsync(UpdateInfo updateInfo)
    {
        if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
        {
            _logger.LogError("No download URL found.");

            return;
        }

        var tempDir = _fileSystemPort.ResolveEnvironmentPath("TEMP");
        var tempFile = _fileSystemPort.CombinePaths(tempDir, $"eBRestarter_Update_{updateInfo.LatestVersion}.exe");

        await _httpDownloadPort.DownloadFileAsync(
            updateInfo.DownloadUrl,
            tempFile,
            new Progress<DownloadProgressStatus>(),
            CancellationToken.None);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Update downloaded to: {Path}", tempFile);
        }

        await Task.Run(() => _processControlPort.StartExecutable(tempFile));

        _applicationLifetimeUseCase.ExitApplication(0);
    }
}


