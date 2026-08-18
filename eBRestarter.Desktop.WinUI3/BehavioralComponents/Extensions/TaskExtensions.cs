using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions;

public static class TaskExtensions
{
#pragma warning disable S3168 // "async" methods should not return "void" - siehe <remarks> oben
    public static async void Forget(this Task task, ILogger logger, string context)
#pragma warning restore S3168
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(
                LogEventIds.RestarterCycle.FireAndForgetTaskFaulted,
                exception,
                "Fire-and-forget task {Context} faulted.",
                context);
        }
    }
}
