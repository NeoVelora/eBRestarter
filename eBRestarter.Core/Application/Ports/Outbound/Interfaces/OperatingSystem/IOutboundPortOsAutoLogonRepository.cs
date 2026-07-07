namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortOsAutoLogonRepository
{
    void EnableAutoLogon(string username, string domain, string password);
    bool IsPasswordlessAuthEnabled();
    void SetPasswordlessAuth(bool enable);
    void DisableAutoLogon();
    bool IsAutoLogonEnabled();
}
