using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Services.WindowsOS;
using Moq;
using Shouldly;
using System.Linq;
using System.Net.NetworkInformation;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.WindowsOS
{
    public class WindowsNetworkInfoServiceTests
    {
        private readonly Mock<INetworkProvider> _mockProvider;
        private readonly WindowsNetworkInfoService _sut;

        public WindowsNetworkInfoServiceTests()
        {
            _mockProvider = new Mock<INetworkProvider>();
            _sut = new WindowsNetworkInfoService(_mockProvider.Object);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsNetworkAvailable_ShouldReturnResultFromProvider(bool isAvailable)
        {
            _mockProvider.Setup(p => p.CheckIsNetworkAvailable()).Returns(isAvailable);

            var result = _sut.IsNetworkAvailable();

            result.ShouldBe(isAvailable);
        }

        [Fact]
        public void GetActiveInterfaces_ShouldFilterOutInvalidInterfaces_AndMapCorrectly()
        {
            var loopbackMock = CreateNicMock("Loopback", OperationalStatus.Up, NetworkInterfaceType.Loopback, sent: 100, received: 100);
            var downMock = CreateNicMock("OfflineCard", OperationalStatus.Down, NetworkInterfaceType.Ethernet, sent: 100, received: 100);
            var noTrafficMock = CreateNicMock("GhostCard", OperationalStatus.Up, NetworkInterfaceType.Ethernet, sent: 0, received: 0);

            var validEthMock = CreateNicMock("Valid Ethernet", OperationalStatus.Up, NetworkInterfaceType.Ethernet, sent: 500, received: 1000);
            var validWifiMock = CreateNicMock("Valid WiFi", OperationalStatus.Up, NetworkInterfaceType.Wireless80211, sent: 0, received: 50);

            _mockProvider.Setup(p => p.RetrieveAllNetworkInterfaces()).Returns(new[]
            {
                loopbackMock.Object,
                downMock.Object,
                noTrafficMock.Object,
                validEthMock.Object,
                validWifiMock.Object
            });

            var result = _sut.RetrieveActiveInterfaces().ToList();

            result.Count.ShouldBe(2);

            var ethStats = result.First(r => r.Name == "Valid Ethernet");
            ethStats.BytesSent.ShouldBe(500);
            ethStats.BytesReceived.ShouldBe(1000);
            ethStats.IsActive.ShouldBeTrue();

            var wifiStats = result.First(r => r.Name == "Valid WiFi");
            wifiStats.BytesSent.ShouldBe(0);
            wifiStats.BytesReceived.ShouldBe(50);
            wifiStats.IsActive.ShouldBeTrue();
        }

        private Mock<NetworkInterface> CreateNicMock(
            string name,
            OperationalStatus status,
            NetworkInterfaceType type,
            long sent,
            long received)
        {
            var mockNic = new Mock<NetworkInterface>();
            mockNic.Setup(n => n.Name).Returns(name);
            mockNic.Setup(n => n.OperationalStatus).Returns(status);
            mockNic.Setup(n => n.NetworkInterfaceType).Returns(type);

            var mockStats = new Mock<IPv4InterfaceStatistics>();
            mockStats.Setup(s => s.BytesSent).Returns(sent);
            mockStats.Setup(s => s.BytesReceived).Returns(received);

            mockNic.Setup(n => n.GetIPv4Statistics()).Returns(mockStats.Object);

            return mockNic;
        }
    }
}
