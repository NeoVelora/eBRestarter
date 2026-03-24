namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

public interface IWindowsAutoLogonService
{
    void EnableAutoLogon(string username, string domain, string password);

    bool IsWindowsHelloPasswordlessEnabled();

    void DisableAutoLogon();

    bool IsAutoLogonEnabled();
}
