using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions;

public static class TaskExtensions
{
    public static void Forget(this Task task, ILogger logger, string context)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(logger);

        if (task.IsCompleted)
        {
            if (task.IsFaulted && task.Exception is not null)
            {
                LogFault(task.Exception);
            }
            return;
        }

        _ = task.ContinueWith(
            static (completedTask, state) =>
            {
                var (log, ctx) = ((ILogger, string))state!;
                if (completedTask.Exception is not null)
                {
                    log.LogError(
                        LogEventIds.RestarterCycle.FireAndForgetTaskFaulted,
                        completedTask.Exception.Flatten(),
                        "Fire-and-forget task {Context} faulted.",
                        ctx);
                }
            },
            (logger, context),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        void LogFault(AggregateException exception)
        {
            logger.LogError(
                LogEventIds.RestarterCycle.FireAndForgetTaskFaulted,
                exception.Flatten(),
                "Fire-and-forget task {Context} faulted.",
                context);
        }
    }
}
