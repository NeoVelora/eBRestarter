using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.UseCases.RemoveApiCredentials;
using eBRestarter.Core.Domain.Entities;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.Tests.Core.Application.UseCases.RemoveApiCredentials
{
    public class RemoveApiCredentialsUseCaseTests
    {
        private readonly Mock<IEVisitorConfigService> _mockConfigService;
        private readonly RemoveApiCredentialsUseCase _sut;

        public RemoveApiCredentialsUseCaseTests()
        {
            _mockConfigService = new Mock<IEVisitorConfigService>();
            _sut = new RemoveApiCredentialsUseCase(_mockConfigService.Object);
        }

        [Fact]
        public void Execute_ShouldClearCredentials_AndKeepOtherSettingsIntact()
        {
            var existingConfig = new AppConfig
            {
                Username = "HauptUser",
                Settings = new SettingsConfig
                {
                    ApiUsername = "MeinApiUser123",
                    ApiKey = "GeheimerSchluessel",
                    Theme = "Dark",
                    Language = 1
                }
            };

            _mockConfigService.Setup(c => c.LoadConfig()).Returns(existingConfig);

            AppConfig? savedConfig = null;
            _mockConfigService
                .Setup(c => c.SaveConfig(It.IsAny<AppConfig>()))
                .Callback<AppConfig>(config => savedConfig = config);

            _sut.Execute();

            _mockConfigService.Verify(c => c.LoadConfig(), Times.Once);
            _mockConfigService.Verify(c => c.SaveConfig(It.IsAny<AppConfig>()), Times.Once);

            savedConfig.ShouldNotBeNull();

            savedConfig.Settings.ApiUsername.ShouldBe(string.Empty);
            savedConfig.Settings.ApiKey.ShouldBe(string.Empty);

            savedConfig.Username.ShouldBe("HauptUser");
            savedConfig.Settings.Theme.ShouldBe("Dark");
            savedConfig.Settings.Language.ShouldBe(1);
        }

        [Fact]
        public void Execute_ShouldBubbleUpException_WhenLoadConfigFails()
        {
            _mockConfigService
                .Setup(c => c.LoadConfig())
                .Throws(new InvalidOperationException("Datei ist blockiert."));

            var exception = Should.Throw<InvalidOperationException>(() => _sut.Execute());
            exception.Message.ShouldBe("Datei ist blockiert.");

            _mockConfigService.Verify(c => c.SaveConfig(It.IsAny<AppConfig>()), Times.Never);
        }

        [Fact]
        public void Execute_ShouldBubbleUpException_WhenSaveConfigFails()
        {
            _mockConfigService.Setup(c => c.LoadConfig()).Returns(new AppConfig());
            _mockConfigService
                .Setup(c => c.SaveConfig(It.IsAny<AppConfig>()))
                .Throws(new UnauthorizedAccessException("Keine Schreibrechte."));

            var exception = Should.Throw<UnauthorizedAccessException>(() => _sut.Execute());
            exception.Message.ShouldBe("Keine Schreibrechte.");
        }
    }
}
