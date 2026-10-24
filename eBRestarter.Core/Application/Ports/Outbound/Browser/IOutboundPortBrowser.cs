using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.Core.Application.Ports.Outbound.Browser;

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

    bool Start(string url, string arguments = "");

    void Close();

    BrowserPaths ResolvePaths();

    bool IsExtensionInstalled(string? extensionId = null);
}


