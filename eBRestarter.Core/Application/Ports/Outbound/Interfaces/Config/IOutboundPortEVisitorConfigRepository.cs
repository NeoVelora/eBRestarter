using eBRestarter.Core.Domain.ValueObjects;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;

public interface IOutboundPortEVisitorConfigRepository
{
    AppConfig LoadConfig();

    void SaveConfig(AppConfig config);

    void ResetConfig();
}


