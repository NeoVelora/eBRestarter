using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Core.Application.Interfaces.Update;
using eBRestarter.Core.Domain.Models;
using eBRestarter.Core.Domain.Models.Records;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace eBRestarter.Infrastructure.Services.Update
{
    public class GitHubUpdateAdapter : IUpdateService
    {
        private readonly IRestClientService _restClient;
        private readonly IWindowsProcessControlService _processService;
        private readonly ILogger<GitHubUpdateAdapter> _logger;

        private const string RepoOwner = "DeinGitHubName";
        private const string RepoName = "eBRestarter";

        private const string GitHubApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

        public GitHubUpdateAdapter(
            IRestClientService restClient,
            IWindowsProcessControlService processService,
            ILogger<GitHubUpdateAdapter> logger)
        {
            _restClient = restClient;
            _processService = processService;
            _logger = logger;
        }

        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);

            var request = new ApiRequest
            {
                Url = GitHubApiUrl,
                Method = Core.Domain.Enums.HttpMethod.GET
            };

            var response = await _restClient.ExecuteGetAsync(request);

            if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
            {
                _logger.LogWarning("Update-Check fehlgeschlagen: {Status}", response.StatusCode);

                return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
            }

            try
            {
                using var doc = JsonDocument.Parse(response.Content);
                var root = doc.RootElement;

                string tagName = root.GetProperty("tag_name").GetString() ?? "0.0.0";
                string cleanVersion = tagName.TrimStart('v');

                if (!Version.TryParse(cleanVersion, out Version? latestVersion))
                {
                    return new UpdateInfo { IsUpdateAvailable = false, CurrentVersion = currentVersion.ToString() };
                }

                string downloadUrl = string.Empty;
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
                _logger.LogError(ex, "Fehler beim Parsen der GitHub Response.");
                return new UpdateInfo { IsUpdateAvailable = false };
            }
        }

        public async Task DownloadAndInstallAsync(UpdateInfo updateInfo)
        {
            if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
            {
                _logger.LogError("Keine Download-URL gefunden.");
                return;
            }

            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"eBRestarter_Update_{updateInfo.LatestVersion}.exe");


                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "eBRestarter-App");

                    var data = await httpClient.GetByteArrayAsync(updateInfo.DownloadUrl);
                    await File.WriteAllBytesAsync(tempFile, data);
                }

                _logger.LogInformation("Update heruntergeladen nach: {Path}", tempFile);


                var startInfo = new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true,
                };

                Process.Start(startInfo);

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Download/Installieren des Updates.");
                throw;
            }
        }
    }
}
