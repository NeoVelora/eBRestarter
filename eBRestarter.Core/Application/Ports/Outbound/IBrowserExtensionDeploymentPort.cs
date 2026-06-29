namespace eBRestarter.Core.Application.Ports.Outbound;

public interface IBrowserExtensionDeploymentPort
{

    void EnsureExtensionIsDeployed();

    string RetrieveExtensionFolderPath();
}
