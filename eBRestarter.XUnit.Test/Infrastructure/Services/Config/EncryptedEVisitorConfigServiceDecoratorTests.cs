using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.Security;
using eBRestarter.Core.Domain.Entities;
using eBRestarter.Infrastructure.Services.Config;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Config
{
    public class EncryptedEVisitorConfigServiceDecoratorTests
    {
        private readonly Mock<IEVisitorConfigService> _mockInnerService;
        private readonly Mock<IEncryptionService> _mockEncryptionService;
        private readonly Mock<ILogger<EncryptedEVisitorConfigServiceDecorator>> _mockLogger;
        private readonly EncryptedEVisitorConfigServiceDecorator _decorator;

        public EncryptedEVisitorConfigServiceDecoratorTests()
        {
            _mockInnerService = new Mock<IEVisitorConfigService>();
            _mockEncryptionService = new Mock<IEncryptionService>();
            _mockLogger = new Mock<ILogger<EncryptedEVisitorConfigServiceDecorator>>();

            _decorator = new EncryptedEVisitorConfigServiceDecorator(
                _mockInnerService.Object,
                _mockEncryptionService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void LoadConfig_ShouldDecryptApiKey_WhenPresent()
        {
            var rawConfig = new AppConfig();
            rawConfig.Settings.ApiKey = "EncryptedApiKey";

            _mockInnerService.Setup(s => s.LoadConfig()).Returns(rawConfig);
            _mockEncryptionService.Setup(e => e.Decrypt("EncryptedApiKey")).Returns("PlaintextApiKey");

            var result = _decorator.LoadConfig();

            result.Settings.ApiKey.ShouldBe("PlaintextApiKey");
        }

        [Fact]
        public void LoadConfig_ShouldHandleDecryptionFailure_Gracefully()
        {
            var rawConfig = new AppConfig();
            rawConfig.Settings.ApiKey = "CorruptApiKey";

            _mockInnerService.Setup(s => s.LoadConfig()).Returns(rawConfig);
            _mockEncryptionService.Setup(e => e.Decrypt("CorruptApiKey")).Returns(string.Empty);

            var result = _decorator.LoadConfig();

            result.Settings.ApiKey.ShouldBeEmpty();
        }

        [Fact]
        public void SaveConfig_ShouldEncryptApiKey_AndForwardToInner()
        {
            var configToSave = new AppConfig();
            configToSave.Settings.ApiKey = "PlaintextApiKey";

            _mockEncryptionService.Setup(e => e.Encrypt("PlaintextApiKey")).Returns("EncryptedApiKey");

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
