namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;

public interface IOutboundPortApplicationLifetime
{
    void ExitApplication(int exitCode);
}
