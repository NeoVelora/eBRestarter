using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem;

public interface IOperatingSystemFacade
{
    IWindowsProcessControlService WindowsProcessControlService { get; }
    IWindowsSystemInfoService WindowsSystemInfoService { get; }
    IWindowsRegistryService WindowsRegistryAdapter { get; }
    IWindowsStartupManagerService WindowsStartupManagerService { get; }
    IWindowsFileSystemService WindowsFileSystemServiceAdapter { get; }
    IWindowsAutoLogonService WindowsAutoLogonAdapter { get; }
}


