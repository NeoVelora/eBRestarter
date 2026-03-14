using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.Security;
using eBRestarter.Core.Domain.Models.Records.Config;
using eBRestarter.Infrastructure.Services.Config;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services;

public class EVisitorConfigServiceTests : IDisposable
{
    private readonly Mock<IPathService> _mockPathService;
    private readonly Mock<IPathProvider> _mockPathProvider;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<ILogger<EVisitorConfigService>> _mockLogger;
    private readonly string _tempConfigFile;

    public EVisitorConfigServiceTests()
    {
        _mockPathService = new Mock<IPathService>();
        _mockPathProvider = new Mock<IPathProvider>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockLogger = new Mock<ILogger<EVisitorConfigService>>();

        _tempConfigFile = Path.Combine(Path.GetTempPath(), "EVisitorConfigServiceTests_" + Guid.NewGuid().ToString() + ".json");
        _mockPathService.Setup(p => p.GetConfigFilePath()).Returns(_tempConfigFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigFile))
        {
            try { File.Delete(_tempConfigFile); } catch { }
        }
    }

    [Fact]
    public void LoadConfig_WhenFileDoesNotExist_ShouldCreateDefaultAndReturnIt()
    {
        var sut = new EVisitorConfigService(_mockPathService.Object, _mockPathProvider.Object, _mockEncryptionService.Object, _mockLogger.Object);

        var result = sut.LoadConfig();

        result.ShouldNotBeNull();
        File.Exists(_tempConfigFile).ShouldBeTrue();
    }

    [Fact]
    public void SaveConfig_ShouldEncryptApiKeyAndWriteFile()
    {
        var sut = new EVisitorConfigService(_mockPathService.Object, _mockPathProvider.Object, _mockEncryptionService.Object, _mockLogger.Object);
        var config = new AppConfig();
        config.Settings.ApiKey = "plainTextKey";

        _mockEncryptionService.Setup(e => e.Encrypt("plainTextKey")).Returns("encryptedKey");

        sut.SaveConfig(config);

        File.Exists(_tempConfigFile).ShouldBeTrue();
        var savedContent = File.ReadAllText(_tempConfigFile);
        
        savedContent.ShouldContain("encryptedKey");
        savedContent.ShouldNotContain("plainTextKey");
    }

    [Fact]
    public void LoadConfig_WhenFileExists_ShouldReadAndDecryptApiKey()
    {
        var sut = new EVisitorConfigService(_mockPathService.Object, _mockPathProvider.Object, _mockEncryptionService.Object, _mockLogger.Object);
        
        var configToSave = new AppConfig();
        configToSave.Settings.ApiKey = "encryptedKey";
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        
        var jsonContent = JsonSerializer.Serialize(configToSave, jsonOptions);
        File.WriteAllText(_tempConfigFile, jsonContent);

        _mockEncryptionService.Setup(e => e.Decrypt("encryptedKey")).Returns("plainTextKey");

        var result = sut.LoadConfig();

        result.ShouldNotBeNull();
        result.Settings.ApiKey.ShouldBe("plainTextKey");
    }

    [Fact]
    public void LoadConfig_WhenDecryptionFails_ShouldReturnEmptyApiKey()
    {
        var sut = new EVisitorConfigService(_mockPathService.Object, _mockPathProvider.Object, _mockEncryptionService.Object, _mockLogger.Object);
        
        var configToSave = new AppConfig();
        configToSave.Settings.ApiKey = "invalidEncryptedKey";
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        
        var jsonContent = JsonSerializer.Serialize(configToSave, jsonOptions);
        File.WriteAllText(_tempConfigFile, jsonContent);

        _mockEncryptionService.Setup(e => e.Decrypt("invalidEncryptedKey")).Returns(string.Empty);

        var result = sut.LoadConfig();

        result.ShouldNotBeNull();
        result.Settings.ApiKey.ShouldBeEmpty();
    }

    [Fact]
    public void LoadConfig_WhenInvalidJson_ShouldReturnDefaultConfig()
    {
        var sut = new EVisitorConfigService(_mockPathService.Object, _mockPathProvider.Object, _mockEncryptionService.Object, _mockLogger.Object);
        File.WriteAllText(_tempConfigFile, "invalid { json");

        var result = sut.LoadConfig();

        result.ShouldNotBeNull();
        result.Settings.ApiKey.ShouldBeEmpty();
    }

    [Fact]
    public void ResetConfig_ShouldOverwriteWithDefault()
    {
        var sut = new EVisitorConfigService(_mockPathService.Object, _mockPathProvider.Object, _mockEncryptionService.Object, _mockLogger.Object);
        File.WriteAllText(_tempConfigFile, "{ \"Settings\": { \"ApiUsername\": \"test\" } }");

        sut.ResetConfig();

        var result = sut.LoadConfig();
        result.Settings.ApiUsername.ShouldBeEmpty();
    }
}