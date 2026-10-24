namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOutboundPortSystemInfoProvider
{
    string RetrieveCurrentStandardBrowserName();
    string RetrieveCurrentOsBuildVersion();
    string RetrieveCurrentOsDisplayVersion();
    bool IsUserAdministrator();
}


