namespace eBRestarter.Core.Application.Interfaces;

public interface IBrowserExtensionDeploymentService
{
    void EnsureExtensionIsDeployed();

    string RetrieveExtensionFolderPath();
}
