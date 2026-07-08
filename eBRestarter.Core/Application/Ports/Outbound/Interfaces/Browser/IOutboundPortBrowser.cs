using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

public interface IOutboundPortBrowser
{
    string DisplayName { get; }
    string IconPath { get; }
    string DownloadUrl { get; }

    BrowserType Type { get; }
    string ProcessName { get; }
    string BrowserVersion { get; }
    bool IsInstalled { get; }
    string ExtensionInstallUrl { get; }

    void Start(string url, string arguments = "");
    void Close();

    BrowserPaths ResolvePaths();

    bool IsExtensionInstalled(string? extensionId = null);
}


