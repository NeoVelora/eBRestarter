using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.Security;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.Config;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.XUnit.Test.Infrastructure.Repositories.Config
{
    public class AdapterEncryptedEVisitorConfigRepositoryDecoratorTests
    {
        private readonly IOutboundPortEVisitorConfigRepository _mockInnerService;
        private readonly IOutboundPortEncryption _mockEncryptionUseCase;
        private readonly FakeLogger<AdapterEncryptedEVisitorConfigRepositoryDecorator> _mockLogger;
        private readonly AdapterEncryptedEVisitorConfigRepositoryDecorator _decorator;

        public AdapterEncryptedEVisitorConfigRepositoryDecoratorTests()
        {
            _mockInnerService = Substitute.For<IOutboundPortEVisitorConfigRepository>();
            _mockEncryptionUseCase = Substitute.For<IOutboundPortEncryption>();
            _mockLogger = new FakeLogger<AdapterEncryptedEVisitorConfigRepositoryDecorator>();

            _decorator = new AdapterEncryptedEVisitorConfigRepositoryDecorator(
                _mockInnerService,
                _mockEncryptionUseCase,
                _mockLogger
            );
        }

        [Fact]
        public void LoadConfig_ShouldDecryptApiKey_WhenPresent()
        {
            var rawConfig = new AppConfig();
            rawConfig.Settings.ApiKey = "EncryptedApiKey";

            _mockInnerService.LoadConfig().Returns(rawConfig);
            _mockEncryptionUseCase.Decrypt("EncryptedApiKey").Returns("PlaintextApiKey");

            var result = _decorator.LoadConfig();

            result.Settings.ApiKey.ShouldBe("PlaintextApiKey");
        }

        [Fact]
        public void LoadConfig_ShouldHandleDecryptionFailure_Gracefully()
        {
            var rawConfig = new AppConfig();
            rawConfig.Settings.ApiKey = "CorruptApiKey";

            _mockInnerService.LoadConfig().Returns(rawConfig);
            _mockEncryptionUseCase.Decrypt("CorruptApiKey").Returns(string.Empty);

            var result = _decorator.LoadConfig();

            result.Settings.ApiKey.ShouldBeEmpty();
        }

        [Fact]
        public void SaveConfig_ShouldEncryptApiKey_AndForwardToInner()
        {
            var configToSave = new AppConfig();
            configToSave.Settings.ApiKey = "PlaintextApiKey";

            _mockEncryptionUseCase.Encrypt("PlaintextApiKey").Returns("EncryptedApiKey");

            string capturedKey = null!;
            _mockInnerService.When(s => s.SaveConfig(Arg.Any<AppConfig>()))
                .Do(ci => capturedKey = ci.Arg<AppConfig>().Settings.ApiKey);

            _decorator.SaveConfig(configToSave);

            _mockInnerService.Received(1).SaveConfig(Arg.Any<AppConfig>());
            capturedKey.ShouldBe("EncryptedApiKey");
            configToSave.Settings.ApiKey.ShouldBe("PlaintextApiKey");
        }

        [Fact]
        public void ResetConfig_ShouldForwardToInner()
        {
            _decorator.ResetConfig();

            _mockInnerService.Received(1).ResetConfig();
        }
    }
}
