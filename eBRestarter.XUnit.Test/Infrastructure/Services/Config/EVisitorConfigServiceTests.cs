using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.Security;
using eBRestarter.Core.Domain.Entities;
using eBRestarter.Infrastructure.Services.Config;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Config
{
    public class EVisitorConfigServiceTests : IDisposable
    {
        private readonly string _tempFilePath;
        private readonly Mock<IPathService> _mockPathService;
        private readonly Mock<IEncryptionService> _mockEncryptionService;
        private readonly Mock<ILogger<EVisitorConfigService>> _mockLogger;

        public EVisitorConfigServiceTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"TestConfig_{Guid.NewGuid()}.json");

            _mockPathService = new Mock<IPathService>();
            _mockEncryptionService = new Mock<IEncryptionService>();
            _mockLogger = new Mock<ILogger<EVisitorConfigService>>();

            _mockPathService.Setup(p => p.GetConfigFilePath()).Returns(_tempFilePath);
        }

        [Fact]
        public void SaveConfig_ShouldEncryptApiKey_AndWriteJsonToFile()
        {
            var service = new EVisitorConfigService(_mockPathService.Object, _mockEncryptionService.Object, _mockLogger.Object);

            var configToSave = new AppConfig();
            configToSave.Settings.ApiKey = "SecretPlaintextKey123";

            _mockEncryptionService
                .Setup(e => e.Encrypt(It.IsAny<string>()))
                .Returns("EncryptedSecretData789");

            service.SaveConfig(configToSave);

            File.Exists(_tempFilePath).ShouldBeTrue();

            string savedJson = File.ReadAllText(_tempFilePath);

            savedJson.ShouldContain("EncryptedSecretData789");
            savedJson.ShouldNotContain("SecretPlaintextKey123");
        }

        [Fact]
        public void LoadConfig_ShouldCreateAndReturnDefault_WhenFileDoesNotExist()
        {
            if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);

            var service = new EVisitorConfigService(_mockPathService.Object, _mockEncryptionService.Object, _mockLogger.Object);

            var result = service.LoadConfig();

            result.ShouldNotBeNull();
            File.Exists(_tempFilePath).ShouldBeTrue();
        }

        [Fact]
        public void LoadConfig_ShouldDecryptApiKey_WhenFileIsValid()
        {
            string fakeEncryptedJson = @"{
                ""Settings"": {
                    ""ApiKey"": ""VerschlüsselterWert""
                }
            }";
            File.WriteAllText(_tempFilePath, fakeEncryptedJson);

            _mockEncryptionService
                .Setup(e => e.Decrypt("VerschlüsselterWert"))
                .Returns("EntschlüsselterKlartext");

            var service = new EVisitorConfigService(_mockPathService.Object, _mockEncryptionService.Object, _mockLogger.Object);

            var result = service.LoadConfig();

            result.Settings.ApiKey.ShouldBe("EntschlüsselterKlartext");
        }

        [Fact]
        public void LoadConfig_ShouldHandleDecryptionFailure_Gracefully()
        {
            string fakeEncryptedJson = @"{ ""Settings"": { ""ApiKey"": ""FalscherPC_Key"" } }";
            File.WriteAllText(_tempFilePath, fakeEncryptedJson);

            _mockEncryptionService
                .Setup(e => e.Decrypt(It.IsAny<string>()))
                .Returns(string.Empty);

            var service = new EVisitorConfigService(_mockPathService.Object, _mockEncryptionService.Object, _mockLogger.Object);

            var result = service.LoadConfig();

            result.ShouldNotBeNull();
            result.Settings.ApiKey.ShouldBeEmpty();

        }

        [Fact]
        public void LoadConfig_ShouldReturnDefault_WhenJsonIsBroken()
        {
            File.WriteAllText(_tempFilePath, "Das hier ist definitiv { kein gültiges ] JSON!");

            var service = new EVisitorConfigService(_mockPathService.Object, _mockEncryptionService.Object, _mockLogger.Object);

            var result = service.LoadConfig();

            result.ShouldNotBeNull();
        }

        [Fact]
        public void ResetConfig_ShouldWriteDefaultConfigToFile()
        {
            File.WriteAllText(_tempFilePath, "Alte, kaputte Daten");

            var service = new EVisitorConfigService(_mockPathService.Object, _mockEncryptionService.Object, _mockLogger.Object);

            service.ResetConfig();

            string newContent = File.ReadAllText(_tempFilePath);
            newContent.ShouldNotContain("Alte, kaputte Daten");
            newContent.ShouldContain("{");
        }
        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
    }
}