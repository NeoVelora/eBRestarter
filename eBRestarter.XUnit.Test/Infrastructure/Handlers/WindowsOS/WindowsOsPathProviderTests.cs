using NSubstitute;
using Shouldly;
using System.IO;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Infrastructure.BehavioralComponents.Providers;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsOsPathProviderAdapterTests
    {
        private readonly IOutboundPortAppPathProvider _mockPathProvider;
        private readonly WindowsAppPathProvider _sut;

        public WindowsOsPathProviderAdapterTests()
        {
            _mockPathProvider = Substitute.For<IOutboundPortAppPathProvider>();

            _sut = new WindowsAppPathProvider(_mockPathProvider);
        }

        [Fact]
        public void GetAppDataPath_ShouldCombineLocalAppData_WithSkylarAndAppFolder()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.RetrieveLocalAppDataDirectory().Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter");

            string actualPath = _sut.RetrieveAppDataPath();

            actualPath.ShouldBe(expectedPath);
        }

        [Fact]
        public void GetDownloadsPath_ShouldCombineUserProfile_WithDownloadsFolder()
        {
            string fakeUserProfile = @"C:\FakeUsers\Max";
            _mockPathProvider.RetrieveUserProfileDirectory().Returns(fakeUserProfile);

            string expectedPath = Path.Combine(fakeUserProfile, "Downloads");

            string actualPath = _sut.RetrieveDownloadsPath();

            actualPath.ShouldBe(expectedPath);
        }

        [Fact]
        public void GetConfigFilePath_ShouldCombineAppDataPath_WithConfigFileName()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.RetrieveLocalAppDataDirectory().Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter", "eBRestarterConfig.json");

            string actualPath = _sut.RetrieveConfigFilePath();

            actualPath.ShouldBe(expectedPath);
        }

        [Fact]
        public void GetLogFilePath_ShouldCombineAppDataPath_WithLogFileName()
        {
            string fakeLocalAppData = @"C:\FakeUsers\Max\AppData\Local";
            _mockPathProvider.RetrieveLocalAppDataDirectory().Returns(fakeLocalAppData);

            string expectedPath = Path.Combine(fakeLocalAppData, "Skylar", "eBRestarter", "log.txt");

            string actualPath = _sut.RetrieveLogFilePath();

            actualPath.ShouldBe(expectedPath);
        }
    }
}
