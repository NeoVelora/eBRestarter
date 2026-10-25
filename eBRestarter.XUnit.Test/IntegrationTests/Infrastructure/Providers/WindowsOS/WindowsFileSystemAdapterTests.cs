using Shouldly;
using System;
using System.IO;
using Xunit;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.WindowsOS;

namespace eBRestarter.XUnit.Test.IntegrationTests.Infrastructure.Providers.WindowsOS
{
    [Trait("Category", "Integration")]
    public class WindowsFileSystemServiceAdapterTests : IDisposable
    {
        private readonly AdapterWindowsFileSystem _sut;
        private readonly string _tempTestDirectory;

        public WindowsFileSystemServiceAdapterTests()
        {
            _sut = new AdapterWindowsFileSystem();

            _tempTestDirectory = Path.Combine(Path.GetTempPath(), $"eB_FS_Test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempTestDirectory);
        }

        [Fact]
        public void FileExists_ShouldReturnCorrectBoolean()
        {
            string existingFilePath = Path.Combine(_tempTestDirectory, "exists.txt");
            string missingFilePath = Path.Combine(_tempTestDirectory, "missing.txt");
            File.WriteAllText(existingFilePath, "Test");

            bool exists = _sut.FileExists(existingFilePath);
            bool missing = _sut.FileExists(missingFilePath);

            exists.ShouldBeTrue();
            missing.ShouldBeFalse();
        }

        [Fact]
        public void DirectoryExists_ShouldReturnCorrectBoolean()
        {
            string existingDirPath = Path.Combine(_tempTestDirectory, "SubFolder");
            string missingDirPath = Path.Combine(_tempTestDirectory, "GhostFolder");
            Directory.CreateDirectory(existingDirPath);

            bool exists = _sut.DirectoryExists(existingDirPath);
            bool missing = _sut.DirectoryExists(missingDirPath);

            exists.ShouldBeTrue();
            missing.ShouldBeFalse();
        }

        [Fact]
        public void CombinePaths_ShouldCombineCorrectly()
        {
            string result = _sut.CombinePaths(@"C:\", "Ordner", "Datei.txt");

            result.ShouldBe(@"C:\Ordner\Datei.txt");
        }

        [Theory]
        [InlineData("appdata")]
        [InlineData("localappdata")]
        [InlineData("programfiles")]
        public void GetEnvironmentPath_ShouldReturnValidPath_ForKnownVariables(string variable)
        {
            string result = _sut.ResolveEnvironmentPath(variable);

            result.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(result).ShouldBeTrue("The resolved path must be an absolute system path.");
        }

        [Fact]
        public void GetEnvironmentPath_ShouldReturnEmptyString_ForUnknownVariables()
        {
            string result = _sut.ResolveEnvironmentPath("GibtsNicht_12345");

            result.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void DeleteFile_ShouldThrowArgumentException_WhenPathIsNullOrWhiteSpace(string? invalidPath)
        {
            Action act = () => _sut.DeleteFile(invalidPath!);

            act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("path");
        }

        [Fact]
        public void DeleteFile_ShouldRemoveFileFromDisk()
        {
            string targetFile = Path.Combine(_tempTestDirectory, "delete_me.txt");
            File.WriteAllText(targetFile, "Trash");

            _sut.DeleteFile(targetFile);

            File.Exists(targetFile).ShouldBeFalse();
        }

        [Fact]
        public void WriteAllText_And_ReadAllText_ShouldWorkCorrectly()
        {
            string filePath = Path.Combine(_tempTestDirectory, "io_test.txt");
            string expectedContent = "Hallo Welt! \n Das ist ein Test.";

            _sut.WriteAllText(filePath, expectedContent);
            string actualContent = _sut.ReadAllText(filePath);

            actualContent.ShouldBe(expectedContent);
        }

        [Fact]
        public void ReadAllLines_ShouldReturnStringArray()
        {
            string filePath = Path.Combine(_tempTestDirectory, "lines_test.txt");
            string[] expectedLines = { "Zeile 1", "Zeile 2", "Zeile 3" };
            File.WriteAllLines(filePath, expectedLines);

            string[] actualLines = _sut.ReadAllLines(filePath);

            actualLines.Length.ShouldBe(3);
            actualLines[0].ShouldBe("Zeile 1");
            actualLines[2].ShouldBe("Zeile 3");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempTestDirectory))
            {
                try
                {
                    Directory.Delete(_tempTestDirectory, true);
                }
                catch
                {
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
