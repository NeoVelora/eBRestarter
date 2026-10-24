using System;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Ports.Inbound.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

namespace eBRestarter.Core.Application.UseCases.System;

public sealed class ToggleAppAutoStartUseCase(
    IOutboundPortEVisitorConfigRepository configService,
    IOutboundPortAutoStartRepository startupManagerService)
    : IUseCaseToggleAppAutoStart
{
    private readonly IOutboundPortEVisitorConfigRepository _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    private readonly IOutboundPortAutoStartRepository _startupManagerService = startupManagerService ?? throw new ArgumentNullException(nameof(startupManagerService));

    public async Task<bool> InitializeAndGetStateAsync()
    {
        var config = _configService.LoadConfig();
        bool isEnabledInOs = await _startupManagerService.IsAutoStartEnabledAsync().ConfigureAwait(false);

        if (config?.Settings?.StartWithWindows == true && !isEnabledInOs)
        {
            await _startupManagerService.EnableAutoStartAsync().ConfigureAwait(false);
            return true;
        }

        return isEnabledInOs;
    }

    public async Task ToggleAsync(bool shouldEnable)
    {
        if (shouldEnable)
        {
            await _startupManagerService.EnableAutoStartAsync().ConfigureAwait(false);
        }
        else
        {
            await _startupManagerService.DisableAutoStartAsync().ConfigureAwait(false);
        }

        var config = _configService.LoadConfig();

        if (config?.Settings is not null)
        {
            config.Settings.StartWithWindows = shouldEnable;
            _configService.SaveConfig(config);
        }
    }
}
