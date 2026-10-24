using System;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Cycle;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

namespace eBRestarter.Core.Application.BehavioralComponents.Handlers;

public sealed class RunBrowserPhaseHandler(
    IOutboundPortEVisitorConfigRepository configService,
    IOutboundPortOsProcessControl processService,
    TimeProvider timeProvider) : IRunBrowserPhaseHandler
{
    private static readonly TimeSpan OneSecondInterval = TimeSpan.FromSeconds(1);

    private const int StartupGracePeriodSeconds = 2;

    private const int BrowserAliveCheckIntervalSeconds = 5;

    private readonly IOutboundPortEVisitorConfigRepository _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    private readonly IOutboundPortOsProcessControl _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<BrowserPhaseResult> ExecuteAsync(
        ManageRestarterCycleRequest request,
        IOutboundPortBrowser? currentBrowser,
        Action<RestartTaskState, int, string?> reportProgress,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(reportProgress);

        string processName = currentBrowser?.ProcessName ?? string.Empty;
        var appConfig = _configService.LoadConfig();

        bool isCleanupActive = appConfig?.Browser?.DeleteBrowserCacheIntervalDays > 0;
        DateTime nextCleanupDate = appConfig?.Browser?.NextBrowserDeleteCacheDate ?? DateTime.MaxValue;

        using var timer = new PeriodicTimer(OneSecondInterval, _timeProvider);

        for (int secondsRemaining = request.RuntimeSeconds; secondsRemaining >= 0; secondsRemaining--)
        {
            token.ThrowIfCancellationRequested();

            bool isAliveCheckDue =
                request.CheckBrowserAliveRoutine &&
                secondsRemaining < request.RuntimeSeconds - StartupGracePeriodSeconds &&
                secondsRemaining % BrowserAliveCheckIntervalSeconds == 0;

            if (isAliveCheckDue && !_processService.IsProcessAlive(processName))
            {
                return BrowserPhaseResult.BrowserClosed;
            }

            if (isCleanupActive && _timeProvider.GetLocalNow().DateTime >= nextCleanupDate)
            {
                return BrowserPhaseResult.CleanupDue;
            }

            reportProgress(RestartTaskState.Running, secondsRemaining, null);

            if (secondsRemaining > 0)
            {
                await timer.WaitForNextTickAsync(token).ConfigureAwait(false);
            }
        }

        return BrowserPhaseResult.Completed;
    }
}
