using NSubstitute;
using Shouldly;
using System.IO;
using System.Text.Json;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.BehavioralComponents.Repositories.Authentication;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Authentication
{
    public class JsonCredentialStoreRepositoryTests
    {
        private readonly string _expectedStoragePath = Path.Combine(@"C:\FakeAppData", "Skylar", "eBRestarter", "eBRestarterConfig.json");

        [Fact]
        public void SaveCredentials_ShouldSerializeToJson_AndWriteToFile()
        {
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockPathProvider = Substitute.For<IOutboundPortAppPathProvider>();

            mockPathProvider.RetrieveLocalAppDataDirectory().Returns(@"C:\FakeAppData");
            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => Path.Combine(ci.Arg<string[]>()));

            var store = new JsonCredentialStoreRepository(
                CreateRoundTripEncryption(),
                mockFileSystem,
                new FakeLogger<JsonCredentialStoreRepository>(),
                mockPathProvider);
            var credentials = new ApiCredentials("TestUser", "TestKey123");

            string expectedJson = JsonSerializer.Serialize(credentials);

            store.SaveCredentials(credentials);

            mockFileSystem.Received(1).WriteAllText(_expectedStoragePath, expectedJson);
        }

        [Fact]
        public void LoadCredentials_ShouldReturnCredentials_WhenFileExistsAndIsValidJson()
        {
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockPathProvider = Substitute.For<IOutboundPortAppPathProvider>();

            mockPathProvider.RetrieveLocalAppDataDirectory().Returns(@"C:\FakeAppData");
            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => Path.Combine(ci.Arg<string[]>()));

            mockFileSystem.FileExists(_expectedStoragePath).Returns(true);

            string fakeJson = "{\"Username\":\"TestUser\",\"ApiKey\":\"TestKey123\"}";
            mockFileSystem.ReadAllText(_expectedStoragePath).Returns(fakeJson);

            var store = new JsonCredentialStoreRepository(
                CreateRoundTripEncryption(),
                mockFileSystem,
                new FakeLogger<JsonCredentialStoreRepository>(),
                mockPathProvider);

            var result = store.LoadCredentials();

            result.ShouldNotBeNull();
            result.Username.ShouldBe("TestUser");
            result.ApiKey.ShouldBe("TestKey123");
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(true, "Kein Gültiges JSON {")]
        public void LoadCredentials_ShouldReturnNull_WhenFileIsMissingOrBroken(bool fileExists, string? fileContent)
        {
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockPathProvider = Substitute.For<IOutboundPortAppPathProvider>();

            mockPathProvider.RetrieveLocalAppDataDirectory().Returns(@"C:\FakeAppData");
            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => Path.Combine(ci.Arg<string[]>()));

            mockFileSystem.FileExists(_expectedStoragePath).Returns(fileExists);

            if (fileContent != null)
            {
                mockFileSystem.ReadAllText(_expectedStoragePath).Returns(fileContent);
            }

            var store = new JsonCredentialStoreRepository(
                CreateRoundTripEncryption(),
                mockFileSystem,
                new FakeLogger<JsonCredentialStoreRepository>(),
                mockPathProvider);

            var result = store.LoadCredentials();

            result.ShouldBeNull();
        }

        [Fact]
        public void ClearCredentials_ShouldDeleteFile_WhenFileExists()
        {
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockPathProvider = Substitute.For<IOutboundPortAppPathProvider>();

            mockPathProvider.RetrieveLocalAppDataDirectory().Returns(@"C:\FakeAppData");
            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => Path.Combine(ci.Arg<string[]>()));

            mockFileSystem.FileExists(_expectedStoragePath).Returns(true);

            var store = new JsonCredentialStoreRepository(
                CreateRoundTripEncryption(),
                mockFileSystem,
                new FakeLogger<JsonCredentialStoreRepository>(),
                mockPathProvider);

            store.ClearCredentials();

            mockFileSystem.Received(1).DeleteFile(_expectedStoragePath);
        }

        [Fact]
        public void ImportFromLegacyFile_ShouldReadBinaryFileCorrectly()
        {
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockPathProvider = Substitute.For<IOutboundPortAppPathProvider>();
            
            mockPathProvider.RetrieveLocalAppDataDirectory().Returns(@"C:\FakeAppData");
            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => Path.Combine(ci.Arg<string[]>()));

            var store = new JsonCredentialStoreRepository(
                CreateRoundTripEncryption(),
                mockFileSystem,
                new FakeLogger<JsonCredentialStoreRepository>(),
                mockPathProvider);

            var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("LegacyUser");
                writer.Write("LegacyKey999");
            }
            ms.Position = 0;

            mockFileSystem.FileExists("legacyPath").Returns(true);
            mockFileSystem.OpenRead("legacyPath").Returns(ms);

            var result = store.ImportFromLegacyFile("legacyPath");

            result.ShouldNotBeNull();
            result.Username.ShouldBe("LegacyUser");
            result.ApiKey.ShouldBe("LegacyKey999");
        }

        [Fact]
        public void ImportFromLegacyFile_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockPathProvider = Substitute.For<IOutboundPortAppPathProvider>();

            mockPathProvider.RetrieveLocalAppDataDirectory().Returns(@"C:\FakeAppData");
            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => Path.Combine(ci.Arg<string[]>()));

            mockFileSystem.FileExists("nonExistentPath").Returns(false);

            var store = new JsonCredentialStoreRepository(
                CreateRoundTripEncryption(),
                mockFileSystem,
                new FakeLogger<JsonCredentialStoreRepository>(),
                mockPathProvider);

            var result = store.ImportFromLegacyFile("nonExistentPath");

            result.ShouldBeNull();
        }
    
        private static IOutboundPortEncryption CreateRoundTripEncryption()
        {
            var encryption = Substitute.For<IOutboundPortEncryption>();
            encryption.Encrypt(Arg.Any<string>()).Returns(ci => ci.Arg<string>());
            encryption.Decrypt(Arg.Any<string>()).Returns(ci => ci.Arg<string>());

            return encryption;
        }
    }
}
