using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Services.Authentication;
using Moq;
using Shouldly;
using System.IO;
using System.Text.Json;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Authentication
{
    public class JsonCredentialStoreTests
    {
        private readonly string _expectedStoragePath = Path.Combine(@"C:\FakeAppData", "Skylar", "eBRestarter", "eBRestarterConfig.json");

        [Fact]
        public void SaveCredentials_ShouldSerializeToJson_AndWriteToFile()
        {
            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            var mockPathProvider = new Mock<IPathProvider>();

            mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(@"C:\FakeAppData");
            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns((string[] paths) => Path.Combine(paths));

            var store = new JsonCredentialStore(mockFileSystem.Object, mockPathProvider.Object);
            var credentials = new ApiCredentials("TestUser", "TestKey123");

            string expectedJson = JsonSerializer.Serialize(credentials);

            store.SaveCredentials(credentials);

            mockFileSystem.Verify(fs => fs.WriteAllText(_expectedStoragePath, expectedJson), Times.Once);
        }

        [Fact]
        public void LoadCredentials_ShouldReturnCredentials_WhenFileExistsAndIsValidJson()
        {
            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            var mockPathProvider = new Mock<IPathProvider>();

            mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(@"C:\FakeAppData");
            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns((string[] paths) => Path.Combine(paths));

            mockFileSystem.Setup(fs => fs.FileExists(_expectedStoragePath)).Returns(true);

            string fakeJson = "{\"Username\":\"TestUser\",\"ApiKey\":\"TestKey123\"}";
            mockFileSystem.Setup(fs => fs.ReadAllText(_expectedStoragePath)).Returns(fakeJson);

            var store = new JsonCredentialStore(mockFileSystem.Object, mockPathProvider.Object);

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
            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            var mockPathProvider = new Mock<IPathProvider>();

            mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(@"C:\FakeAppData");
            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns((string[] paths) => Path.Combine(paths));

            mockFileSystem.Setup(fs => fs.FileExists(_expectedStoragePath)).Returns(fileExists);

            if (fileContent != null)
            {
                mockFileSystem.Setup(fs => fs.ReadAllText(_expectedStoragePath)).Returns(fileContent);
            }

            var store = new JsonCredentialStore(mockFileSystem.Object, mockPathProvider.Object);

            var result = store.LoadCredentials();

            result.ShouldBeNull();
        }

        [Fact]
        public void ClearCredentials_ShouldDeleteFile_WhenFileExists()
        {
            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            var mockPathProvider = new Mock<IPathProvider>();

            mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(@"C:\FakeAppData");
            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns((string[] paths) => Path.Combine(paths));

            mockFileSystem.Setup(fs => fs.FileExists(_expectedStoragePath)).Returns(true);

            var store = new JsonCredentialStore(mockFileSystem.Object, mockPathProvider.Object);

            store.ClearCredentials();

            mockFileSystem.Verify(fs => fs.DeleteFile(_expectedStoragePath), Times.Once);
        }

        [Fact]
        public void ImportFromLegacyFile_ShouldReadBinaryFileCorrectly()
        {
            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            var mockPathProvider = new Mock<IPathProvider>();
            
            mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(@"C:\FakeAppData");
            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns((string[] paths) => Path.Combine(paths));

            var store = new JsonCredentialStore(mockFileSystem.Object, mockPathProvider.Object);

            var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("LegacyUser");
                writer.Write("LegacyKey999");
            }
            ms.Position = 0;

            mockFileSystem.Setup(fs => fs.FileExists("legacyPath")).Returns(true);
            mockFileSystem.Setup(fs => fs.OpenRead("legacyPath")).Returns(ms);

            var result = store.ImportFromLegacyFile("legacyPath");

            result.ShouldNotBeNull();
            result.Username.ShouldBe("LegacyUser");
            result.ApiKey.ShouldBe("LegacyKey999");
        }

        [Fact]
        public void ImportFromLegacyFile_ShouldReturnNull_WhenFileDoesNotExist()
        {
            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            var mockPathProvider = new Mock<IPathProvider>();

            mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(@"C:\FakeAppData");
            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns((string[] paths) => Path.Combine(paths));

            mockFileSystem.Setup(fs => fs.FileExists("nonExistentPath")).Returns(false);

            var store = new JsonCredentialStore(mockFileSystem.Object, mockPathProvider.Object);

            var result = store.ImportFromLegacyFile("nonExistentPath");

            result.ShouldBeNull();
        }
    }
}
