using eBRestarter.Infrastructure.Services.WindowsOS;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsPathProviderTests
    {
        private readonly WindowsPathProvider _sut;

        public WindowsPathProviderTests()
        {
            _sut = new WindowsPathProvider();
        }

        [Fact]
        public void GetAppDataDirectory_ShouldReturnCorrectRoamingAppDataPath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string actualPath = _sut.GetAppDataDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue("Der Pfad muss ein absoluter Systempfad (z.B. C:\\...) sein.");
        }

        [Fact]
        public void GetLocalAppDataDirectory_ShouldReturnCorrectLocalAppDataPath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string actualPath = _sut.GetLocalAppDataDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }

        [Fact]
        public void GetUserProfileDirectory_ShouldReturnCorrectUserProfilePath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string actualPath = _sut.GetUserProfileDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }

        [Fact]
        public void GetProgramFilesDirectory_ShouldReturnCorrectProgramFilesPath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            string actualPath = _sut.GetProgramFilesDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }

        [Fact]
        public void GetProgramFilesX86Directory_ShouldReturnCorrectProgramFilesX86Path()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string actualPath = _sut.GetProgramFilesX86Directory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }
    }
}