using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Entities;
using eBRestarter.Infrastructure.Services.Config;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Config
{
    public class EVisitorConfigServiceTests
    {
        private readonly Mock<IPathUseCase> _mockPathUseCase;
        private readonly Mock<IWindowsFileSystemService> _mockFileSystem;
        private readonly Mock<ILogger<EVisitorConfigService>> _mockLogger;
        private readonly EVisitorConfigService _service;
        private const string FakeFilePath = @"C:\AppData\eBRestarter\config.json";

        public EVisitorConfigServiceTests()
        {
            _mockPathUseCase = new Mock<IPathUseCase>();
            _mockFileSystem = new Mock<IWindowsFileSystemService>();
            _mockLogger = new Mock<ILogger<EVisitorConfigService>>();

            _mockPathUseCase.Setup(p => p.RetrieveConfigFilePath()).Returns(FakeFilePath);

            _service = new EVisitorConfigService(
                _mockPathUseCase.Object, 
                _mockFileSystem.Object, 
                _mockLogger.Object
            );
        }

        [Fact]
        public void SaveConfig_ShouldWriteJsonToFileSystem()
        {
            var configToSave = new AppConfig();
            configToSave.Settings.ApiKey = "PlaintextKey123";

            string capturedJson = null!;
            _mockFileSystem
                .Setup(f => f.WriteAllText(FakeFilePath, It.IsAny<string>()))
                .Callback<string, string>((path, content) => capturedJson = content);

            _mockFileSystem.Setup(f => f.GetDirectoryName(FakeFilePath)).Returns(@"C:\AppData\eBRestarter");
            _mockFileSystem.Setup(f => f.DirectoryExists(@"C:\AppData\eBRestarter")).Returns(true);

            _service.SaveConfig(configToSave);

            _mockFileSystem.Verify(f => f.WriteAllText(FakeFilePath, It.IsAny<string>()), Times.Once);
            capturedJson.ShouldNotBeNull();
            capturedJson.ShouldContain("PlaintextKey123");
        }

        [Fact]
        public void LoadConfig_ShouldCreateAndReturnDefault_WhenFileDoesNotExist()
        {
            _mockFileSystem.Setup(f => f.FileExists(FakeFilePath)).Returns(false);
            _mockFileSystem.Setup(f => f.GetDirectoryName(FakeFilePath)).Returns(@"C:\AppData\eBRestarter");
            _mockFileSystem.Setup(f => f.DirectoryExists(@"C:\AppData\eBRestarter")).Returns(true);

            var result = _service.LoadConfig();

            result.ShouldNotBeNull();
            _mockFileSystem.Verify(f => f.WriteAllText(FakeFilePath, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void LoadConfig_ShouldReturnDefault_WhenJsonIsBroken()
        {
            _mockFileSystem.Setup(f => f.FileExists(FakeFilePath)).Returns(true);
            _mockFileSystem.Setup(f => f.ReadAllText(FakeFilePath)).Returns("Das hier ist kein JSON!");

            var result = _service.LoadConfig();

            result.ShouldNotBeNull();
            result.Settings.ApiKey.ShouldBeEmpty();
        }

        [Fact]
        public void ResetConfig_ShouldWriteDefaultConfigToFileSystem()
        {
            _mockFileSystem.Setup(f => f.GetDirectoryName(FakeFilePath)).Returns(@"C:\AppData\eBRestarter");
            _mockFileSystem.Setup(f => f.DirectoryExists(@"C:\AppData\eBRestarter")).Returns(true);

            _service.ResetConfig();

            _mockFileSystem.Verify(f => f.WriteAllText(FakeFilePath, It.IsAny<string>()), Times.Once);
        }
    }
}

