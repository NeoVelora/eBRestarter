using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions;

public static partial class TaskExtensions
{
    [LoggerMessage(
        EventId = LogEventIds.RestarterCycle.FireAndForgetTaskFaulted,
        Level = LogLevel.Error,
        Message = "Fire-and-forget task {Context} faulted.")]
    private static partial void LogFireAndForgetTaskFaulted(ILogger logger, Exception exception, string context);

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
                    LogFireAndForgetTaskFaulted(log, completedTask.Exception.Flatten(), ctx);
                }
            },
            (logger, context),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        void LogFault(AggregateException exception)
        {
            LogFireAndForgetTaskFaulted(logger, exception.Flatten(), context);
        }
    }
}
