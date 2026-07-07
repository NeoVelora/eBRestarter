namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IInboundPortOsAppPathProvider
{
    string RetrieveAppDataPath();
    string RetrieveDownloadsPath();
    string RetrieveConfigFilePath();
    string RetrieveLogFilePath();
}
