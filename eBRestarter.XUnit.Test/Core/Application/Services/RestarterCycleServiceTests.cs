using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Domain.Handlers;
using eBRestarter.Core.Application.Ports.Inbound.Validators;
using eBRestarter.Infrastructure.Adapters.Validators;
using eBRestarter.Infrastructure.Api;
using eBRestarter.Infrastructure.Browser;
using eBRestarter.Infrastructure.OperatingSystem;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Inbound.Services;
using eBRestarter.Core.Application.Services;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Handlers;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.XUnit.Test.Core.Application.Services
{
    public class RestarterCycleServiceTests
    {
        private readonly Mock<IBrowserFactoryOutboundPort> _mockBrowserFactory;
        private readonly Mock<IInboundPortLocalizationProvider> _mockLocalizationService;
        private readonly Mock<IEVisitorConfigRepositoryOutboundPort> _mockConfigService;
        private readonly Mock<IBrowserCleanupScheduleHandler> _mockCleanupScheduleHandler;
        private readonly Mock<IOsProcessControlOutboundPort> _mockProcessService;

        private readonly Mock<IBrowserOutboundPort> _mockBrowser;
        private readonly FakeTimeProvider _fakeTimeProvider;
        private readonly RestarterCycleService _sut;

        public RestarterCycleServiceTests()
        {
            _mockBrowserFactory = new Mock<IBrowserFactoryOutboundPort>();
            _mockLocalizationService = new Mock<IInboundPortLocalizationProvider>();
            _mockConfigService = new Mock<IEVisitorConfigRepositoryOutboundPort>();
            _mockCleanupScheduleHandler = new Mock<IBrowserCleanupScheduleHandler>();
            _mockProcessService = new Mock<IOsProcessControlOutboundPort>();
            _mockBrowser = new Mock<IBrowserOutboundPort>();

            _fakeTimeProvider = new FakeTimeProvider();

            _mockLocalizationService.Setup(l => l.RetrieveString(It.IsAny<string>())).Returns((string key) => key);
            _mockBrowserFactory.Setup(f => f.Create(It.IsAny<BrowserType>())).Returns(_mockBrowser.Object);
            var dummyConfig = new AppConfig { Browser = null, Username = "TestUser" };
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(dummyConfig);

            var delayStrategy = new DelayPhaseHandler(_fakeTimeProvider);
            var runStrategy = new RunBrowserPhaseHandler(_fakeTimeProvider, _mockProcessService.Object, _mockConfigService.Object);

            _sut = new RestarterCycleService(
                _mockBrowserFactory.Object,
                _mockLocalizationService.Object,
                _mockConfigService.Object,
                _mockCleanupScheduleHandler.Object,
                _fakeTimeProvider,
                new FluentValidationAdapter<ManageRestarterCycleRequest>(new ManageRestarterCycleValidator()),
                delayStrategy,
                runStrategy);
        }

        private async Task AdvanceTimeAsync(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                _fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
                await Task.Delay(10);
            }
        }

        [Fact]
        public async Task StartAsync_ShouldStopCleanly_WhenStopIsCalled()
        {
            var request = new ManageRestarterCycleRequest(eBRestarter.Core.Application.Enums.BrowserType.Firefox, "User", 10, 5, false);
            var emittedEvents = new List<RestarterCycleProgress>();
            _sut.ProgressChanged += (s, e) => emittedEvents.Add(e);

            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);

            await AdvanceTimeAsync(1);
            _sut.Stop();
            await cycleTask;

            var finalEvent = emittedEvents.Last();
            finalEvent.State.ShouldBe(RestartTaskState.Idle);
            _mockBrowser.Verify(b => b.Close(), Times.Never);
        }

        [Fact]
        public async Task StartAsync_ShouldExecuteFullCycleAndLaunchBrowser()
        {
            var request = new ManageRestarterCycleRequest(eBRestarter.Core.Application.Enums.BrowserType.Firefox, "TestUser", 10, 5, false);


            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);


            await AdvanceTimeAsync(6);

            _mockBrowser.Verify(b => b.Start($"{WebLinks.EVisitorSurflink}TestUser", It.IsAny<string>()), Times.Once);

            await AdvanceTimeAsync(11);

            _mockBrowser.Verify(b => b.Close(), Times.Once);

            _sut.Stop();
            await cycleTask;
        }

        [Fact]
        public async Task RunBrowserPhase_ShouldDetectCrash_AndTriggerCrashCooldown()
        {
            var request = new ManageRestarterCycleRequest(eBRestarter.Core.Application.Enums.BrowserType.Firefox, "TestUser", 60, 10, CheckBrowserAliveRoutine: true);
            var emittedEvents = new List<RestarterCycleProgress>();
            _sut.ProgressChanged += (s, e) => emittedEvents.Add(e);

            _mockProcessService.Setup(p => p.IsProcessAlive("firefox")).Returns(false);

            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);

            await AdvanceTimeAsync(6);
            await AdvanceTimeAsync(4);

            emittedEvents.ShouldContain(e => e.State == RestartTaskState.Cooldown && e.StatusMessage.Contains("wurde geschlossen"));

            _sut.Stop();
            await cycleTask;
        }

        [Fact]
        public async Task Cycle_ShouldTriggerCleanupCallback_AndSaveNewDate()
        {
            var request = new ManageRestarterCycleRequest(eBRestarter.Core.Application.Enums.BrowserType.Firefox, "TestUser", 60, 10, false);
            bool callbackExecuted = false;

            var dummyConfig = new AppConfig { Browser = new BrowserConfig() };
            dummyConfig.Browser.UpdateCleanupSettings(7, _fakeTimeProvider);
            dummyConfig.Browser.SetNextCleanupDate(DateTime.MinValue);
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(dummyConfig);
            _mockCleanupScheduleHandler.Setup(c => c.ShouldRunCleanupNow(7, DateTime.MinValue)).Returns(true);
            _mockCleanupScheduleHandler.Setup(c => c.CalculateNextCleanupDateAfterRun(It.IsAny<DateTime>(), 7)).Returns(DateTime.UtcNow.AddDays(1));

            var cycleTask = _sut.StartAsync(request, () =>
            {
                callbackExecuted = true;
                return Task.CompletedTask;
            });

            await AdvanceTimeAsync(6);
            await AdvanceTimeAsync(11);
            await AdvanceTimeAsync(7);

            callbackExecuted.ShouldBeTrue();
            _mockBrowser.Verify(b => b.Start($"{WebLinks.EVisitorSurflink}TestUser", It.IsAny<string>()), Times.Once);

            _sut.Stop();
            await cycleTask;
        }

        [Fact]
        public async Task RunCycle_ShouldReloadConfig_BeforeEveryNewIteration()
        {
            var request = new ManageRestarterCycleRequest(eBRestarter.Core.Application.Enums.BrowserType.Chrome, "OldUser", 10, 5, false);

            var config1 = new AppConfig { Username = "OldUser", Browser = null };
            var config2 = new AppConfig { Username = "NewUser", Browser = null };

            int loadConfigCallCount = 0;
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(() =>
            {
                loadConfigCallCount++;
                return loadConfigCallCount <= 3 ? config1 : config2;
            });

            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);

            await AdvanceTimeAsync(6);
            await AdvanceTimeAsync(11);
            await AdvanceTimeAsync(7);

            _mockBrowser.Verify(b => b.Start($"{WebLinks.EVisitorSurflink}NewUser", It.IsAny<string>()), Times.Once);

            _sut.Stop();
            await cycleTask;
        }
    }
}


