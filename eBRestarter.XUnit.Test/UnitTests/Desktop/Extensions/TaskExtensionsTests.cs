using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions;

namespace eBRestarter.XUnit.Test.UnitTests.Desktop.Extensions;

[Trait("Category", "Unit")]
public class TaskExtensionsTests
{
    [Fact]
    public void Forget_WhenTaskIsNull_ThrowsArgumentNullException()
    {
        Task task = null!;
        var logger = Substitute.For<ILogger>();

        Should.Throw<ArgumentNullException>(() => task.Forget(logger, "TestContext"));
    }

    [Fact]
    public void Forget_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        Task task = Task.CompletedTask;
        ILogger logger = null!;

        Should.Throw<ArgumentNullException>(() => task.Forget(logger, "TestContext"));
    }

    [Fact]
    public void Forget_WhenTaskCompletesSuccessfully_DoesNotLogError()
    {
        var logger = Substitute.For<ILogger>();
        var task = Task.CompletedTask;

        task.Forget(logger, "SuccessContext");

        logger.DidNotReceiveWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Forget_WhenTaskAlreadyFaulted_LogsErrorSynchronously()
    {
        var logger = Substitute.For<ILogger>();
        var expectedException = new InvalidOperationException("Pre-faulted task test");
        var task = Task.FromException(expectedException);

        task.Forget(logger, "FaultedContext");

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == LogEventIds.RestarterCycle.FireAndForgetTaskFaulted),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex.InnerException == expectedException || ex == expectedException),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Forget_WhenTaskFaultsAsynchronously_LogsError()
    {
        var logger = Substitute.For<ILogger>();
        var tcs = new TaskCompletionSource();
        var expectedException = new InvalidOperationException("Async fault test");

        tcs.Task.Forget(logger, "AsyncFaultContext");

        tcs.SetException(expectedException);

        await Task.Delay(50);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == LogEventIds.RestarterCycle.FireAndForgetTaskFaulted),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex.InnerException == expectedException || ex == expectedException),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
