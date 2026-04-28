using eBRestarter.Core.Application.Models.Config;

namespace eBRestarter.Core.Application.Interfaces.Config;

public interface IEVisitorConfigService
{
    AppConfig LoadConfig();

    void SaveConfig(AppConfig config);

    void ResetConfig();
}
