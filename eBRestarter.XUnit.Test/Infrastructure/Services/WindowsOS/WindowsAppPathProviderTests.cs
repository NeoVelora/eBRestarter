using eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Providers;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsAppPathProviderTests
    {
        private readonly AdapterWindowsAppPathProvider _sut;

        public WindowsAppPathProviderTests()
        {
            _sut = new AdapterWindowsAppPathProvider();
        }

        [Fact]
        public void GetAppDataDirectory_ShouldReturnCorrectRoamingAppDataPath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string actualPath = _sut.RetrieveAppDataDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue("Der Pfad muss ein absoluter Systempfad (z.B. C:\\...) sein.");
        }

        [Fact]
        public void GetLocalAppDataDirectory_ShouldReturnCorrectLocalAppDataPath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string actualPath = _sut.RetrieveLocalAppDataDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }

        [Fact]
        public void GetUserProfileDirectory_ShouldReturnCorrectUserProfilePath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string actualPath = _sut.RetrieveUserProfileDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }

        [Fact]
        public void GetProgramFilesDirectory_ShouldReturnCorrectProgramFilesPath()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            string actualPath = _sut.RetrieveProgramFilesDirectory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }

        [Fact]
        public void GetProgramFilesX86Directory_ShouldReturnCorrectProgramFilesX86Path()
        {
            string expectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string actualPath = _sut.RetrieveProgramFilesX86Directory();

            actualPath.ShouldBe(expectedPath);
            actualPath.ShouldNotBeNullOrWhiteSpace();
            Path.IsPathRooted(actualPath).ShouldBeTrue();
        }
    }
}

