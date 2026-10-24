using System;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Ports.Inbound.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Config;

namespace eBRestarter.Core.Application.UseCases.Browser;

public sealed class InitializeBrowserCleanupUseCase(
    IOutboundPortEVisitorConfigRepository configService,
    TimeProvider timeProvider)
    : IUseCaseInitializeBrowserCleanup
{
    private readonly IOutboundPortEVisitorConfigRepository _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public Task ExecuteAsync()
    {
        var config = _configService.LoadConfig();

        int intervalDays = config.Browser?.DeleteBrowserCacheIntervalDays ?? 0;
        DateTime nextDate = config.Browser?.NextBrowserDeleteCacheDate ?? DateTime.MinValue;
        DateTime today = _timeProvider.GetLocalNow().Date;

        if (nextDate == today && intervalDays > 0)
        {
            nextDate = today.AddDays(intervalDays);
            config.Browser?.SetNextCleanupDate(nextDate);
            _configService.SaveConfig(config);
        }

        return Task.CompletedTask;
    }
}
