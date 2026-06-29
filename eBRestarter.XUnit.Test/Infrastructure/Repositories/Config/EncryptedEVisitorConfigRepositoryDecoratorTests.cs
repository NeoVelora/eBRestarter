using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.Security;
using eBRestarter.Core.Application.Ports.Outbound.Security;
using eBRestarter.Core.Domain.Entities;
using eBRestarter.Infrastructure.Repositories.Config;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Repositories.Config
{
    public class EncryptedEVisitorConfigRepositoryDecoratorTests
    {
        private readonly Mock<IEVisitorConfigPort> _mockInnerService;
        private readonly Mock<IEncryptionPort> _mockEncryptionUseCase;
        private readonly Mock<ILogger<EncryptedEVisitorConfigRepositoryDecorator>> _mockLogger;
        private readonly EncryptedEVisitorConfigRepositoryDecorator _decorator;

        public EncryptedEVisitorConfigRepositoryDecoratorTests()
        {
            _mockInnerService = new Mock<IEVisitorConfigPort>();
            _mockEncryptionUseCase = new Mock<IEncryptionPort>();
            _mockLogger = new Mock<ILogger<EncryptedEVisitorConfigRepositoryDecorator>>();

            _decorator = new EncryptedEVisitorConfigRepositoryDecorator(
                _mockInnerService.Object,
                _mockEncryptionUseCase.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void LoadConfig_ShouldDecryptApiKey_WhenPresent()
        {
            var rawConfig = new AppConfig();
            rawConfig.Settings.ApiKey = "EncryptedApiKey";

            _mockInnerService.Setup(s => s.LoadConfig()).Returns(rawConfig);
            _mockEncryptionUseCase.Setup(e => e.Decrypt("EncryptedApiKey")).Returns("PlaintextApiKey");

            var result = _decorator.LoadConfig();

            result.Settings.ApiKey.ShouldBe("PlaintextApiKey");
        }

        [Fact]
        public void LoadConfig_ShouldHandleDecryptionFailure_Gracefully()
        {
            var rawConfig = new AppConfig();
            rawConfig.Settings.ApiKey = "CorruptApiKey";

            _mockInnerService.Setup(s => s.LoadConfig()).Returns(rawConfig);
            _mockEncryptionUseCase.Setup(e => e.Decrypt("CorruptApiKey")).Returns(string.Empty);

            var result = _decorator.LoadConfig();

            result.Settings.ApiKey.ShouldBeEmpty();
        }

        [Fact]
        public void SaveConfig_ShouldEncryptApiKey_AndForwardToInner()
        {
            var configToSave = new AppConfig();
            configToSave.Settings.ApiKey = "PlaintextApiKey";

            _mockEncryptionUseCase.Setup(e => e.Encrypt("PlaintextApiKey")).Returns("EncryptedApiKey");

            string capturedKey = null!;
            _mockInnerService
                .Setup(s => s.SaveConfig(It.IsAny<AppConfig>()))
                .Callback<AppConfig>(c => capturedKey = c.Settings.ApiKey);

            _decorator.SaveConfig(configToSave);

            _mockInnerService.Verify(s => s.SaveConfig(It.IsAny<AppConfig>()), Times.Once);
            capturedKey.ShouldBe("EncryptedApiKey");
            configToSave.Settings.ApiKey.ShouldBe("PlaintextApiKey");
        }

        [Fact]
        public void ResetConfig_ShouldForwardToInner()
        {
            _decorator.ResetConfig();

            _mockInnerService.Verify(s => s.ResetConfig(), Times.Once);
        }
    }
}


