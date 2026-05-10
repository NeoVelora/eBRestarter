using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem;

public interface IOperatingSystemFacade
{
    IWindowsProcessControlService WindowsProcessControlService { get; }
    IWindowsSystemInfoService WindowsSystemInfoService { get; }
    IWindowsRegistryService WindowsRegistryService { get; }
    IWindowsStartupManagerService WindowsStartupManagerService { get; }
    IWindowsFileSystemService WindowsFileSystemService { get; }
    IWindowsAutoLogonService WindowsAutoLogonService { get; }
}
