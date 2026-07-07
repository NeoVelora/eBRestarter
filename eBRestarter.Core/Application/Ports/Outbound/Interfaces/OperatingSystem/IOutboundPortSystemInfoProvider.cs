namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortSystemInfoProvider
{
    string RetrieveCurrentStandardBrowserName();
    string RetrieveCurrentOsBuildVersion();
    string RetrieveCurrentOsDisplayVersion();
    bool IsUserAdministrator();
}


