using eBRestarter.Core.Domain.ValueObjects;

namespace eBRestarter.Core.Application.Ports.Outbound.Config;

public interface IOutboundPortEVisitorConfigRepository
{
    AppConfig LoadConfig();

    void SaveConfig(AppConfig config);

    void ResetConfig();
}
