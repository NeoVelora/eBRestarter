using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models;
using System.Diagnostics;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

public class WindowsBrowserService(IBrowserFactory browserFactory) : IBrowserService
{
    private readonly IBrowserFactory _browserFactory = browserFactory ?? throw new ArgumentNullException(nameof(browserFactory));

    public async Task<IEnumerable<BrowserInfo>> FindInstalledBrowsersAsync()
    {
        var browsers = new List<BrowserInfo>();

        foreach (BrowserType type in Enum.GetValues<BrowserType>())
        {
            try
            {
                IBrowser browserLogic = _browserFactory.Create(type);

                bool isInstalled = browserLogic.IsInstalled;
                string version = isInstalled ? browserLogic.BrowserVersion : "Nicht installiert";
                string displayName = browserLogic.DisplayName;
                string iconPath = browserLogic.IconPath;
                string downloadUrl = browserLogic.DownloadUrl;

                var browserInfo = new BrowserInfo
                {
                    Type = type,
                    Name = displayName,
                    IsInstalled = isInstalled,
                    Version = version,
                    IconPath = iconPath,
                    DownloadUrl = downloadUrl,
                };

                browsers.Add(browserInfo);
            }
            catch (NotSupportedException)
            {
                continue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        return await Task.FromResult(browsers);
    }
}
