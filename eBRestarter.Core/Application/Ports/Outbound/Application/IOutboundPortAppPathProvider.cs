namespace eBRestarter.Core.Application.Ports.Outbound.Application;

public interface IOutboundPortAppPathProvider
{
    string RetrieveAppDataDirectory();
    string RetrieveLocalAppDataDirectory();
    string RetrieveUserProfileDirectory();
    string RetrieveProgramFilesDirectory();
    string RetrieveProgramFilesX86Directory();
}


