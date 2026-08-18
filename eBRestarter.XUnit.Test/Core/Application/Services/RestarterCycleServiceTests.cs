using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.BehavioralComponents.Handlers;
using eBRestarter.Core.Application.BehavioralComponents.Services;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Handlers;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Infrastructure.BehavioralComponents.Validators;
using eBRestarter.Infrastructure.Common.Statics;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Validators;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;

namespace eBRestarter.XUnit.Test.Core.Application.Services
{
    public class RestarterCycleServiceTests
    {
        private const int TestTimeoutMilliseconds = 15000;

        private readonly IOutboundPortBrowserFactory _mockBrowserFactory;
        private readonly IInboundPortLocalizationProvider _mockLocalizationService;
        private readonly IOutboundPortEVisitorConfigRepository _mockConfigService;
        private readonly IBrowserCleanupScheduleHandler _mockCleanupScheduleHandler;
        private readonly IOutboundPortOsProcessControl _mockProcessService;

        private readonly IOutboundPortBrowser _mockBrowser;
        private readonly FakeTimeProvider _fakeTimeProvider;
        private readonly RestarterCycleService _sut;

        public RestarterCycleServiceTests()
        {
            _mockBrowserFactory = Substitute.For<IOutboundPortBrowserFactory>();
            _mockLocalizationService = Substitute.For<IInboundPortLocalizationProvider>();
            _mockConfigService = Substitute.For<IOutboundPortEVisitorConfigRepository>();
            _mockCleanupScheduleHandler = Substitute.For<IBrowserCleanupScheduleHandler>();
            _mockProcessService = Substitute.For<IOutboundPortOsProcessControl>();
            _mockBrowser = Substitute.For<IOutboundPortBrowser>();

            _mockBrowser.Start(Arg.Any<string>()).Returns(true);
            _mockBrowser.Start(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

            _fakeTimeProvider = new FakeTimeProvider();

            _mockLocalizationService.RetrieveString(Arg.Any<string>()).Returns(ci => ci.Arg<string>());
            _mockBrowserFactory.Create(Arg.Any<BrowserType>()).Returns(_mockBrowser);
            var dummyConfig = new AppConfig { Browser = null!, Username = "TestUser" };
            _mockConfigService.LoadConfig().Returns(dummyConfig);

            var delayStrategy = new DelayPhaseHandler(_fakeTimeProvider);
            var runStrategy = new RunBrowserPhaseHandler(_mockConfigService, _mockProcessService, _fakeTimeProvider);

            _sut = new RestarterCycleService(
                _mockCleanupScheduleHandler,
                _mockBrowserFactory,
                _mockConfigService,
                delayStrategy,
                _mockLocalizationService,
                Substitute.For<IOutboundPortApplicationLogger<RestarterCycleService>>(),
                runStrategy,
                _fakeTimeProvider,
                new AdapterFluentValidationWrapper<ManageRestarterCycleRequest>(new ManageRestarterCycleValidator()));
        }

        private async Task AdvanceTimeAsync(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                _fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
                await Task.Delay(10);
            }
        }

        [Fact(Timeout = TestTimeoutMilliseconds)]
        public async Task StartAsync_ShouldStopCleanly_WhenStopIsCalled()
        {
            var request = new ManageRestarterCycleRequest(BrowserType.Firefox, "User", 10, 5, false);
            var emittedEvents = new List<RestarterCycleProgress>();
            _sut.ProgressChanged += (s, e) => emittedEvents.Add(e);

            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);

            await AdvanceTimeAsync(1);
            _sut.Stop();
            await cycleTask;

            var finalEvent = emittedEvents.Last();
            finalEvent.State.ShouldBe(RestartTaskState.Idle);
            _mockBrowser.DidNotReceive().Close();
        }

        [Fact(Timeout = TestTimeoutMilliseconds)]
        public async Task StartAsync_ShouldExecuteFullCycleAndLaunchBrowser()
        {
            var request = new ManageRestarterCycleRequest(BrowserType.Firefox, "TestUser", 10, 5, false);


            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);


            await AdvanceTimeAsync(6);

            _mockBrowser.Received(1).Start($"{WebLinks.EVisitorSurflink}TestUser", Arg.Any<string>());

            await AdvanceTimeAsync(11);

            _mockBrowser.Received(1).Close();

            _sut.Stop();
            await cycleTask;
        }

        [Fact(Timeout = TestTimeoutMilliseconds)]
        public async Task RunBrowserPhase_ShouldDetectCrash_AndTriggerCrashCooldown()
        {
            var request = new ManageRestarterCycleRequest(BrowserType.Firefox, "TestUser", 60, 10, CheckBrowserAliveRoutine: true);
            var emittedEvents = new List<RestarterCycleProgress>();
            _sut.ProgressChanged += (s, e) => emittedEvents.Add(e);

            _mockProcessService.IsProcessAlive("firefox").Returns(false);

            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);

            await AdvanceTimeAsync(6);
            await AdvanceTimeAsync(4);

            emittedEvents.ShouldContain(e => e.State == RestartTaskState.Cooldown && e.StatusMessage.Contains("wurde geschlossen"));

            _sut.Stop();
            await cycleTask;
        }

        [Fact(Timeout = TestTimeoutMilliseconds)]
        public async Task Cycle_ShouldTriggerCleanupCallback_AndSaveNewDate()
        {
            var request = new ManageRestarterCycleRequest(BrowserType.Firefox, "TestUser", 60, 10, false);
            bool callbackExecuted = false;

            var dummyConfig = new AppConfig { Browser = new BrowserConfig() };
            dummyConfig.Browser.UpdateCleanupSettings(7, _fakeTimeProvider);
            dummyConfig.Browser.SetNextCleanupDate(DateTime.MinValue);
            _mockConfigService.LoadConfig().Returns(dummyConfig);
            _mockCleanupScheduleHandler.ShouldRunCleanupNow(7, DateTime.MinValue).Returns(true);
            _mockCleanupScheduleHandler.CalculateNextCleanupDateAfterRun(Arg.Any<DateTime>(), 7).Returns(DateTime.UtcNow.AddDays(1));

            var cycleTask = _sut.StartAsync(request, () =>
            {
                callbackExecuted = true;
                return Task.CompletedTask;
            });

            await AdvanceTimeAsync(6);
            await AdvanceTimeAsync(11);
            await AdvanceTimeAsync(7);

            callbackExecuted.ShouldBeTrue();
            _mockBrowser.Received(1).Start($"{WebLinks.EVisitorSurflink}TestUser", Arg.Any<string>());

            _sut.Stop();
            await cycleTask;
        }

        [Fact(Timeout = TestTimeoutMilliseconds)]
        public async Task RunCycle_ShouldReloadConfig_BeforeEveryNewIteration()
        {
            var request = new ManageRestarterCycleRequest(BrowserType.Chrome, "OldUser", 10, 5, false);

            var config1 = new AppConfig { Browser = null!, Username = "OldUser" };
            var config2 = new AppConfig { Browser = null!, Username = "NewUser" };

            int loadConfigCallCount = 0;
            _mockConfigService.LoadConfig().Returns(_ =>
            {
                loadConfigCallCount++;
                return loadConfigCallCount <= 3 ? config1 : config2;
            });

            var cycleTask = _sut.StartAsync(request, () => Task.CompletedTask);

            await AdvanceTimeAsync(6);
            await AdvanceTimeAsync(11);
            await AdvanceTimeAsync(7);

            _mockBrowser.Received(1).Start($"{WebLinks.EVisitorSurflink}NewUser", Arg.Any<string>());

            _sut.Stop();
            await cycleTask;
        }
    }
}


