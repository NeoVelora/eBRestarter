using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Infrastructure.Services.WindowsOS;
using Moq;
using Shouldly;
using System.IO;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsPathServiceTests
    {
        private readonly Mock<IPathProvider> _mockPathProvider;
        private readonly WindowsPathService _sut;

        public WindowsPathServiceTests()
        {
            _mockPathProvider = new Mock<IPathProvider>();

            _sut = new WindowsPathService(_mockPathProvider.Object);
        }


        [Fact]
        public void GetAppDataPath_ShouldCombineLocalAppData_WithSkylarAndAppFolder()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.Setup(p => p.GetLocalAppDataDirectory()).Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter");

            string actualPath = _sut.GetAppDataPath();

            actualPath.ShouldBe(expectedPath);
        }


        [Fact]
        public void GetDownloadsPath_ShouldCombineUserProfile_WithDownloadsFolder()
        {
            string fakeUserProfile = @"C:\FakeUsers\Max";
            _mockPathProvider.Setup(p => p.GetUserProfileDirectory()).Returns(fakeUserProfile);

            string expectedPath = Path.Combine(fakeUserProfile, "Downloads");

            string actualPath = _sut.GetDownloadsPath();

            actualPath.ShouldBe(expectedPath);
        }


        [Fact]
        public void GetConfigFilePath_ShouldCombineAppDataPath_WithConfigFileName()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.Setup(p => p.GetLocalAppDataDirectory()).Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter", "eBRestarterConfig.json");

            string actualPath = _sut.GetConfigFilePath();

            actualPath.ShouldBe(expectedPath);
        }

        [Fact]
        public void GetLogFilePath_ShouldCombineAppDataPath_WithLogFileName()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.Setup(p => p.GetLocalAppDataDirectory()).Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter", "log.txt");

            string actualPath = _sut.GetLogFilePath();

            actualPath.ShouldBe(expectedPath);
        }
    }
}