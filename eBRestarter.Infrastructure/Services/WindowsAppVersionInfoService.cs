using eBRestarter.Core.Application.Interfaces;
using System.Reflection;

namespace eBRestarter.Infrastructure.Services;

public class WindowsAppVersionInfoService : IAppVersionInfoService
{
    public string RetrieveAppVersion()
    {
        var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;

        if (assemblyVersion is not null)
        {
            return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}.{assemblyVersion.Revision}";
        }

        return new Version(1, 0, 0, 0).ToString();
    }
}
