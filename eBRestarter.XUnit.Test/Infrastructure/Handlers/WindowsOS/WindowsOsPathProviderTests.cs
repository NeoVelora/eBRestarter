using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using Moq;
using Shouldly;
using System.IO;
using Xunit;
using eBRestarter.Infrastructure.Providers;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsOsPathProviderAdapterTests
    {
        private readonly Mock<IAppPathProviderOutboundPort> _mockPathProvider;
        private readonly WindowsAppPathProvider _sut;

        public WindowsOsPathProviderAdapterTests()
        {
            _mockPathProvider = new Mock<IAppPathProviderOutboundPort>();

            _sut = new WindowsAppPathProvider(_mockPathProvider.Object);
        }


        [Fact]
        public void GetAppDataPath_ShouldCombineLocalAppData_WithSkylarAndAppFolder()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter");

            string actualPath = _sut.RetrieveAppDataPath();

            actualPath.ShouldBe(expectedPath);
        }


        [Fact]
        public void GetDownloadsPath_ShouldCombineUserProfile_WithDownloadsFolder()
        {
            string fakeUserProfile = @"C:\FakeUsers\Max";
            _mockPathProvider.Setup(p => p.RetrieveUserProfileDirectory()).Returns(fakeUserProfile);

            string expectedPath = Path.Combine(fakeUserProfile, "Downloads");

            string actualPath = _sut.RetrieveDownloadsPath();

            actualPath.ShouldBe(expectedPath);
        }


        [Fact]
        public void GetConfigFilePath_ShouldCombineAppDataPath_WithConfigFileName()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter", "eBRestarterConfig.json");

            string actualPath = _sut.RetrieveConfigFilePath();

            actualPath.ShouldBe(expectedPath);
        }

        [Fact]
        public void GetLogFilePath_ShouldCombineAppDataPath_WithLogFileName()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.Setup(p => p.RetrieveLocalAppDataDirectory()).Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter", "log.txt");

            string actualPath = _sut.RetrieveLogFilePath();

            actualPath.ShouldBe(expectedPath);
        }
    }
}


