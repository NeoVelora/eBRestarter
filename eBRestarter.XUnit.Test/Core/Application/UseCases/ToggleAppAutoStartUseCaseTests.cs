using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.ToggleAppAutoStart;
using eBRestarter.Core.Application.UseCases.ToggleAppAutoStart;
using eBRestarter.Core.Application.Models.Errors;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Domain.Entities;
using Moq;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.Tests.Core.Application.UseCases.ToggleAppAutoStart
{
    public class ToggleAppAutoStartUseCaseTests
    {
        private readonly Mock<IAutoStartPort> _mockStartupManager;
        private readonly Mock<IEVisitorConfigPort> _mockConfigService;
        private readonly ToggleAppAutoStartUseCase _sut;

        public ToggleAppAutoStartUseCaseTests()
        {
            _mockStartupManager = new Mock<IAutoStartPort>();
            _mockConfigService = new Mock<IEVisitorConfigPort>();

            _sut = new ToggleAppAutoStartUseCase(
                _mockStartupManager.Object,
                _mockConfigService.Object);
        }

        [Fact]
        public async Task InitializeAndGetStateAsync_ShouldEnableInOs_WhenConfigIsTrueAndOsIsFalse()
        {
            var config = new AppConfig { Settings = new SettingsConfig { StartWithWindows = true } };
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(config);

            _mockStartupManager.Setup(s => s.IsAutoStartEnabledAsync()).ReturnsAsync(false);

            var result = await _sut.InitializeAndGetStateAsync();

            result.ShouldBeTrue();

            _mockStartupManager.Verify(s => s.EnableAutoStartAsync(), Times.Once);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        public async Task InitializeAndGetStateAsync_ShouldReturnOsState_WhenNoSyncNeeded(
            bool configState, bool osState, bool expectedResult)
        {
            var config = new AppConfig { Settings = new SettingsConfig { StartWithWindows = configState } };
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(config);
            _mockStartupManager.Setup(s => s.IsAutoStartEnabledAsync()).ReturnsAsync(osState);

            var result = await _sut.InitializeAndGetStateAsync();

            result.ShouldBe(expectedResult);

            _mockStartupManager.Verify(s => s.EnableAutoStartAsync(), Times.Never);
            _mockStartupManager.Verify(s => s.DisableAutoStartAsync(), Times.Never);
        }

        [Fact]
        public async Task ToggleAsync_ShouldEnableInOs_AndSaveConfig_WhenTrue()
        {
            var initialConfig = new AppConfig { Settings = new SettingsConfig { StartWithWindows = false } };
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(initialConfig);

            AppConfig? savedConfig = null;
            _mockConfigService.Setup(c => c.SaveConfig(It.IsAny<AppConfig>()))
                              .Callback<AppConfig>(c => savedConfig = c);

            await _sut.ToggleAsync(true);

            _mockStartupManager.Verify(s => s.EnableAutoStartAsync(), Times.Once);
            _mockStartupManager.Verify(s => s.DisableAutoStartAsync(), Times.Never);

            _mockConfigService.Verify(c => c.SaveConfig(It.IsAny<AppConfig>()), Times.Once);
            savedConfig.ShouldNotBeNull();
            savedConfig.Settings.StartWithWindows.ShouldBeTrue();
        }

        [Fact]
        public async Task ToggleAsync_ShouldDisableInOs_AndSaveConfig_WhenFalse()
        {
            var initialConfig = new AppConfig { Settings = new SettingsConfig { StartWithWindows = true } };
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(initialConfig);

            AppConfig? savedConfig = null;
            _mockConfigService.Setup(c => c.SaveConfig(It.IsAny<AppConfig>()))
                              .Callback<AppConfig>(c => savedConfig = c);

            await _sut.ToggleAsync(false);

            _mockStartupManager.Verify(s => s.DisableAutoStartAsync(), Times.Once);
            _mockStartupManager.Verify(s => s.EnableAutoStartAsync(), Times.Never);

            _mockConfigService.Verify(c => c.SaveConfig(It.IsAny<AppConfig>()), Times.Once);
            savedConfig.ShouldNotBeNull();
            savedConfig.Settings.StartWithWindows.ShouldBeFalse();
        }
    }
}


