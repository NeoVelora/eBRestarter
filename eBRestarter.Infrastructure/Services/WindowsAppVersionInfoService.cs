using eBRestarter.Core.Application.Interfaces;
using System.Reflection;
using Windows.ApplicationModel;

namespace eBRestarter.Infrastructure.Services;

public class WindowsAppVersionInfoService : IAppVersionInfoService
{
    public string GetAppVersion()
    {
        try
        {
            PackageVersion version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch
        {
            var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;

            if (assemblyVersion != null)
            {
                return $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}.{assemblyVersion.Revision}";
            }

            return "1.0.0.0";
        }
    }
}
