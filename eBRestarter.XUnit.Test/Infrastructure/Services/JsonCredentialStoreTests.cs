using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Infrastructure.Services.Authentication;
using Moq;
using Shouldly;
using System.IO;
using System.Text.Json;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services;

public class JsonCredentialStoreTests
{
    private readonly Mock<IWindowsFileSystemService> _mockFileSystem;
    private readonly Mock<IPathProvider> _mockPathProvider;
    private readonly string _mockLocalAppDataPath = @"C:\TestApp\LocalAppData";
    private readonly string _expectedStoragePath;

    public JsonCredentialStoreTests()
    {
        _mockFileSystem = new Mock<IWindowsFileSystemService>();
        _mockPathProvider = new Mock<IPathProvider>();
        
        _mockPathProvider.Setup(p => p.GetLocalAppDataDirectory()).Returns(_mockLocalAppDataPath);
        
        _expectedStoragePath = Path.Combine(_mockLocalAppDataPath, "Skylar", "eBRestarter", "eBRestarterConfig.json");
    }

    [Fact]
    public void SaveCredentials_ShouldSerializeAndWriteToFile()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);
        var credentials = new ApiCredentials("testUser", "testKey");

        sut.SaveCredentials(credentials);

        var expectedJson = JsonSerializer.Serialize(credentials);
        _mockFileSystem.Verify(f => f.WriteAllText(_expectedStoragePath, expectedJson), Times.Once);
    }

    [Fact]
    public void LoadCredentials_WhenFileExists_ShouldDeserializeAndReturnCredentials()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);
        var expectedCredentials = new ApiCredentials("testUser", "testKey");
        var jsonContent = JsonSerializer.Serialize(expectedCredentials);

        _mockFileSystem.Setup(f => f.FileExists(_expectedStoragePath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(_expectedStoragePath)).Returns(jsonContent);

        var result = sut.LoadCredentials();

        result.ShouldNotBeNull();
        result.Username.ShouldBe("testUser");
        result.ApiKey.ShouldBe("testKey");
    }

    [Fact]
    public void LoadCredentials_WhenFileDoesNotExist_ShouldReturnNull()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);
        _mockFileSystem.Setup(f => f.FileExists(_expectedStoragePath)).Returns(false);

        var result = sut.LoadCredentials();

        result.ShouldBeNull();
        _mockFileSystem.Verify(f => f.ReadAllText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadCredentials_WhenFileIsInvalidJson_ShouldReturnNull()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);
        _mockFileSystem.Setup(f => f.FileExists(_expectedStoragePath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(_expectedStoragePath)).Returns("invalid json");

        var result = sut.LoadCredentials();

        result.ShouldBeNull();
    }

    [Fact]
    public void ClearCredentials_WhenFileExists_ShouldDeleteFile()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);
        _mockFileSystem.Setup(f => f.FileExists(_expectedStoragePath)).Returns(true);

        sut.ClearCredentials();

        _mockFileSystem.Verify(f => f.DeleteFile(_expectedStoragePath), Times.Once);
    }

    [Fact]
    public void ClearCredentials_WhenFileDoesNotExist_ShouldNotDeleteFile()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);
        _mockFileSystem.Setup(f => f.FileExists(_expectedStoragePath)).Returns(false);

        sut.ClearCredentials();

        _mockFileSystem.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ImportFromLegacyFile_WhenValidBinaryFile_ShouldReturnCredentials()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);
        var tempFile = Path.GetTempFileName();
        
        try
        {
            using (var writer = new BinaryWriter(File.Open(tempFile, FileMode.Create)))
            {
                writer.Write("legacyUser");
                writer.Write("legacyKey");
            }

            var result = sut.ImportFromLegacyFile(tempFile);

            result.ShouldNotBeNull();
            result.Username.ShouldBe("legacyUser");
            result.ApiKey.ShouldBe("legacyKey");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ImportFromLegacyFile_WhenFileDoesNotExist_ShouldReturnNull()
    {
        var sut = new JsonCredentialStore(_mockFileSystem.Object, _mockPathProvider.Object);

        var result = sut.ImportFromLegacyFile("nonexistent.dat");

        result.ShouldBeNull();
    }
}