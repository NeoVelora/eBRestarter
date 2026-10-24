using NSubstitute;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.UseCases.System;
using eBRestarter.Core.Domain.ValueObjects;

namespace eBRestarter.Tests.Core.Application.UseCases.ToggleAppAutoStart
{
    public class ToggleAppAutoStartUseCaseTests
    {
        private readonly IOutboundPortAutoStartRepository _mockStartupManager;
        private readonly IOutboundPortEVisitorConfigRepository _mockConfigService;
        private readonly ToggleAppAutoStartUseCase _sut;

        public ToggleAppAutoStartUseCaseTests()
        {
            _mockStartupManager = Substitute.For<IOutboundPortAutoStartRepository>();
            _mockConfigService = Substitute.For<IOutboundPortEVisitorConfigRepository>();

            _sut =
            new ToggleAppAutoStartUseCase(
                _mockConfigService,
                _mockStartupManager);
        }

        [Fact]
        public async Task InitializeAndGetStateAsync_ShouldEnableInOs_WhenConfigIsTrueAndOsIsFalse()
        {
            var config = new AppConfig { Settings = new SettingsConfig { StartWithWindows = true } };
            _mockConfigService.LoadConfig().Returns(config);

            _mockStartupManager.IsAutoStartEnabledAsync().Returns(false);

            var result = await _sut.InitializeAndGetStateAsync();

            result.ShouldBeTrue();

            await _mockStartupManager.Received(1).EnableAutoStartAsync();
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        public async Task InitializeAndGetStateAsync_ShouldReturnOsState_WhenNoSyncNeeded(
            bool configState, bool osState, bool expectedResult)
        {
            var config = new AppConfig { Settings = new SettingsConfig { StartWithWindows = configState } };
            _mockConfigService.LoadConfig().Returns(config);
            _mockStartupManager.IsAutoStartEnabledAsync().Returns(osState);

            var result = await _sut.InitializeAndGetStateAsync();

            result.ShouldBe(expectedResult);

            await _mockStartupManager.DidNotReceive().EnableAutoStartAsync();
            await _mockStartupManager.DidNotReceive().DisableAutoStartAsync();
        }

        [Fact]
        public async Task ToggleAsync_ShouldEnableInOs_AndSaveConfig_WhenTrue()
        {
            var initialConfig = new AppConfig { Settings = new SettingsConfig { StartWithWindows = false } };
            _mockConfigService.LoadConfig().Returns(initialConfig);

            AppConfig? savedConfig = null;
            _mockConfigService.When(s => s.SaveConfig(Arg.Any<AppConfig>()))
                .Do(ci => savedConfig = ci.Arg<AppConfig>());

            await _sut.ToggleAsync(true);

            await _mockStartupManager.Received(1).EnableAutoStartAsync();
            await _mockStartupManager.DidNotReceive().DisableAutoStartAsync();

            _mockConfigService.Received(1).SaveConfig(Arg.Any<AppConfig>());
            savedConfig.ShouldNotBeNull();
            savedConfig.Settings.StartWithWindows.ShouldBeTrue();
        }

        [Fact]
        public async Task ToggleAsync_ShouldDisableInOs_AndSaveConfig_WhenFalse()
        {
            var initialConfig = new AppConfig { Settings = new SettingsConfig { StartWithWindows = true } };
            _mockConfigService.LoadConfig().Returns(initialConfig);

            AppConfig? savedConfig = null;
            _mockConfigService.When(s => s.SaveConfig(Arg.Any<AppConfig>()))
                .Do(ci => savedConfig = ci.Arg<AppConfig>());

            await _sut.ToggleAsync(false);

            await _mockStartupManager.Received(1).DisableAutoStartAsync();
            await _mockStartupManager.DidNotReceive().EnableAutoStartAsync();

            _mockConfigService.Received(1).SaveConfig(Arg.Any<AppConfig>());
            savedConfig.ShouldNotBeNull();
            savedConfig.Settings.StartWithWindows.ShouldBeFalse();
        }
    }
}


