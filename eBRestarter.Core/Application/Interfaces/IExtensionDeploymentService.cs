using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces;

public interface IBrowserExtensionDeploymentService
{
    void EnsureExtensionIsDeployed();

    string GetExtensionFolderPath();
}
