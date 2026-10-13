using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Infrastructure.BehavioralComponents.Repositories.Config;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.XUnit.Test.Infrastructure.Repositories.Config
{
    public class EVisitorConfigRepositoryTests
    {
        private readonly IInboundPortOsAppPathProvider _mockPathProvider;
        private readonly IOutboundPortFileSystem _mockFileSystem;
        private readonly FakeLogger<EVRestarterConfigRepository> _mockLogger;
        private readonly EVRestarterConfigRepository _service;
        private const string FakeFilePath = @"C:\AppData\eBRestarter\config.json";

        public EVisitorConfigRepositoryTests()
        {
            _mockPathProvider = Substitute.For<IInboundPortOsAppPathProvider>();
            _mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            _mockLogger = new FakeLogger<EVRestarterConfigRepository>();

            _mockPathProvider.RetrieveConfigFilePath().Returns(FakeFilePath);

            _service = new EVRestarterConfigRepository(
                _mockPathProvider, 
                _mockFileSystem, 
                _mockLogger
            );
        }

        [Fact]
        public void SaveConfig_ShouldWriteJsonToFileSystem()
        {
            var configToSave = new AppConfig();
            configToSave.Settings.ApiKey = "PlaintextKey123";

            string capturedJson = null!;
            _mockFileSystem
                .When(fs => fs.WriteAllText(FakeFilePath, Arg.Any<string>()))
                .Do(ci => capturedJson = ci.ArgAt<string>(1));

            _mockFileSystem.GetDirectoryName(FakeFilePath).Returns(@"C:\AppData\eBRestarter");
            _mockFileSystem.DirectoryExists(@"C:\AppData\eBRestarter").Returns(true);

            _service.SaveConfig(configToSave);

            _mockFileSystem.Received(1).WriteAllText(FakeFilePath, Arg.Any<string>());
            capturedJson.ShouldNotBeNull();
            capturedJson.ShouldContain("PlaintextKey123");
        }

        [Fact]
        public void SaveConfig_ShouldThrowInvalidOperationException_WhenFileSystemWriteFails()
        {
            var config = new AppConfig();
            _mockFileSystem.GetDirectoryName(FakeFilePath).Returns(@"C:\AppData\eBRestarter");
            _mockFileSystem.DirectoryExists(@"C:\AppData\eBRestarter").Returns(true);
            _mockFileSystem.When(fs => fs.WriteAllText(FakeFilePath, Arg.Any<string>()))
                .Do(_ => throw new System.IO.IOException("Disk full"));

            var ex = Should.Throw<InvalidOperationException>(() => _service.SaveConfig(config));
            ex.InnerException.ShouldBeOfType<System.IO.IOException>();
        }

        [Fact]
        public void LoadConfig_ShouldCreateAndReturnDefault_WhenFileDoesNotExist()
        {
            _mockFileSystem.FileExists(FakeFilePath).Returns(false);
            _mockFileSystem.GetDirectoryName(FakeFilePath).Returns(@"C:\AppData\eBRestarter");
            _mockFileSystem.DirectoryExists(@"C:\AppData\eBRestarter").Returns(true);

            var result = _service.LoadConfig();

            result.ShouldNotBeNull();
            _mockFileSystem.Received(1).WriteAllText(FakeFilePath, Arg.Any<string>());
        }

        [Fact]
        public void LoadConfig_ShouldThrowInvalidOperationException_WhenJsonIsBroken()
        {
            _mockFileSystem.FileExists(FakeFilePath).Returns(true);
            _mockFileSystem.ReadAllText(FakeFilePath).Returns("Invalid JSON content {");

            var ex = Should.Throw<InvalidOperationException>(() => _service.LoadConfig());
            ex.InnerException.ShouldBeOfType<System.Text.Json.JsonException>();
        }

        [Fact]
        public void LoadConfig_ShouldRethrow_WhenFileSystemThrowsException()
        {
            _mockFileSystem.FileExists(FakeFilePath).Returns(true);
            _mockFileSystem.ReadAllText(FakeFilePath).Throws(new UnauthorizedAccessException("Access denied"));

            Should.Throw<UnauthorizedAccessException>(() => _service.LoadConfig());
        }

        [Fact]
        public void ResetConfig_ShouldWriteDefaultConfigToFileSystem()
        {
            _mockFileSystem.GetDirectoryName(FakeFilePath).Returns(@"C:\AppData\eBRestarter");
            _mockFileSystem.DirectoryExists(@"C:\AppData\eBRestarter").Returns(true);

            _service.ResetConfig();

            _mockFileSystem.Received(1).WriteAllText(FakeFilePath, Arg.Any<string>());
        }
    }
}
