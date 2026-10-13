using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using Xunit;
using eBRestarter.Core.Application.BehavioralComponents.Services;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.ValueObjects;

namespace eBRestarter.XUnit.Test.Core.Application.Services
{
    public class ComputerRestartServiceTests : IDisposable
    {
        private readonly IOutboundPortApplicationLifetime _applicationLifetimeMock;
        private readonly IOutboundPortEVisitorConfigRepository _configServiceMock;
        private readonly IOutboundPortApplicationLogger<ComputerRestartService> _loggerMock;
        private readonly IOutboundPortOsProcessControl _processServiceMock;
        private readonly FakeTimeProvider _fakeTimeProvider;
        private readonly ComputerRestartService _sut;

        public ComputerRestartServiceTests()
        {
            _applicationLifetimeMock = Substitute.For<IOutboundPortApplicationLifetime>();
            _configServiceMock = Substitute.For<IOutboundPortEVisitorConfigRepository>();
            _loggerMock = Substitute.For<IOutboundPortApplicationLogger<ComputerRestartService>>();
            _processServiceMock = Substitute.For<IOutboundPortOsProcessControl>();
            _fakeTimeProvider = new FakeTimeProvider();

            var appConfig = new AppConfig
            {
                Computer = new Computer()
            };
            _configServiceMock.LoadConfig().Returns(appConfig);

            _sut = new ComputerRestartService(
                _applicationLifetimeMock,
                _configServiceMock,
                _loggerMock,
                _processServiceMock,
                _fakeTimeProvider);
        }

        [Fact]
        public async Task StartScheduler_WhenStoppedGracefully_StopsWithoutFaultLogging()
        {
            _sut.StartScheduler();

            _fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(20);

            await _sut.StopSchedulerAsync();

            _loggerMock.DidNotReceive().LogError(
                LogEventIds.RestarterCycle.RestartSchedulerLoopFaulted,
                Arg.Any<Exception>(),
                Arg.Any<string>());

            _loggerMock.Received().LogInformation(
                LogEventIds.RestarterCycle.RestartSchedulerStopped,
                Arg.Is<string>(msg => msg.Contains("Computer Restart Scheduler stopped.")));
        }

        [Fact]
        public async Task StopSchedulerAsync_WhenCalledMultipleTimes_IsIdempotent()
        {
            _sut.StartScheduler();
            await _sut.StopSchedulerAsync();
            await _sut.StopSchedulerAsync();

            _loggerMock.Received(1).LogInformation(
                LogEventIds.RestarterCycle.RestartSchedulerStopped,
                Arg.Is<string>(msg => msg.Contains("Computer Restart Scheduler stopped.")));
        }

        [Fact]
        public async Task LoopAsync_WhenTickThrowsUnexpectedException_LogsFault()
        {
            _configServiceMock.LoadConfig().Returns(_ => throw new InvalidOperationException("Config database unavailable"));

            _sut.StartScheduler();

            _fakeTimeProvider.Advance(TimeSpan.FromSeconds(31));
            await Task.Delay(50);

            await _sut.StopSchedulerAsync();

            _loggerMock.Received(1).LogError(
                LogEventIds.RestarterCycle.RestartSchedulerLoopFaulted,
                Arg.Any<InvalidOperationException>(),
                Arg.Is<string>(msg => msg.Contains("An unhandled fault occurred within the restart scheduler loop runtime.")));
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
