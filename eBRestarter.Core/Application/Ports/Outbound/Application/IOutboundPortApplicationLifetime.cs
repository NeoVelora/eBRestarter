namespace eBRestarter.Core.Application.Ports.Outbound.Application;

public interface IOutboundPortApplicationLifetime
{
    void ExitApplication(int exitCode);
}
