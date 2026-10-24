using NSubstitute;
using Shouldly;
using System.Linq;
using System.Net.NetworkInformation;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Infrastructure.BehavioralComponents.Providers;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.WindowsOS
{
    public class WindowsNetworkInfoUseCaseTests
    {
        private readonly IOutboundPortNetworkProvider _mockProvider;
        private readonly NetworkInfoProvider _sut;

        public WindowsNetworkInfoUseCaseTests()
        {
            _mockProvider = Substitute.For<IOutboundPortNetworkProvider>();
            _sut = new NetworkInfoProvider(_mockProvider);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsNetworkAvailable_ShouldReturnResultFromProvider(bool isAvailable)
        {
            _mockProvider.CheckIsNetworkAvailable().Returns(isAvailable);

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

            _mockProvider.RetrieveAllNetworkInterfaces().Returns(new[]
            {
                loopbackMock,
                downMock,
                noTrafficMock,
                validEthMock,
                validWifiMock
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

        private static NetworkInterface CreateNicMock(
            string name,
            OperationalStatus status,
            NetworkInterfaceType type,
            long sent,
            long received)
        {
            var mockNic = Substitute.For<NetworkInterface>();
            mockNic.Name.Returns(name);
            mockNic.OperationalStatus.Returns(status);
            mockNic.NetworkInterfaceType.Returns(type);

            var mockStats = Substitute.For<IPv4InterfaceStatistics>();
            mockStats.BytesSent.Returns(sent);
            mockStats.BytesReceived.Returns(received);

            mockNic.GetIPv4Statistics().Returns(mockStats);

            return mockNic;
        }
    }
}
