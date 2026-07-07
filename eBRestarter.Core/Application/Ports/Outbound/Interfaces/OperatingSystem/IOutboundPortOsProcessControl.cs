namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortOsProcessControl
{
    void RunInstaller(string installerPath);
    void StartExecutable(string exeFilePath);
    Task StartExecutableAsync(string exeFilePath);
    void OpenDirectoryInFileBrowser(string folderPath);
    void OpenUrlInBrowser(string exeFilePath, string arguments);
    void CloseApplication(string processName);
    Task CloseAllOpenProgramsAsync(int timeoutMilliseconds);
    void ShutdownComputer();
    bool IsProcessAlive(string processName);
}
