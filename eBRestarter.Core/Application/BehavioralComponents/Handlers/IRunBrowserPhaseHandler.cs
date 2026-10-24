using System;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Cycle;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Browser;

namespace eBRestarter.Core.Application.BehavioralComponents.Handlers;

public interface IRunBrowserPhaseHandler
{
    Task<BrowserPhaseResult> ExecuteAsync(
        ManageRestarterCycleRequest request,
        IOutboundPortBrowser? currentBrowser,
        Action<RestartTaskState, int, string?> reportProgress,
        CancellationToken token);
}
