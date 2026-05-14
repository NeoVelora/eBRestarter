namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

public interface IWindowsSystemInfoService
{
    string RetrieveCurrentStandardBrowserName();
    string RetrieveCurrentOsBuildVersion();
    string RetrieveCurrentOsDisplayVersion();
    bool IsUserAdministrator();
}
