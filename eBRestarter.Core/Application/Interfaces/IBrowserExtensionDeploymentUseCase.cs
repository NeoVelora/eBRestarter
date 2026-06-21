namespace eBRestarter.Core.Application.Interfaces;

public interface IBrowserExtensionDeploymentUseCase
{
    void EnsureExtensionIsDeployed();

    string RetrieveExtensionFolderPath();
}
