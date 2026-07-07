using eBRestarter.Core.Domain.Entities;

namespace eBRestarter.Core.Application.Ports.Outbound.Config;

public interface IOutboundPortEVisitorConfigRepository
{
    AppConfig LoadConfig();

    void SaveConfig(AppConfig config);

    void ResetConfig();
}


