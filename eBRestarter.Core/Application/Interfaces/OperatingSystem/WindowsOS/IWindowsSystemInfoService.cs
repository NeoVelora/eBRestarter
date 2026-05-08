namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

public interface IWindowsSystemInfoService
{
    string GetCurrentStandardBrowserName();
    string GetCurrentOsBuildVersion();
    string GetCurrentOsDisplayVersion();
    bool IsUserAdministrator();
}
