using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Providers.SystemInfo;
using Moq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Tests.Core.Application.UseCases.GetSystemInformation
{
    public class SystemInformationProviderTests
    {
        private readonly Mock<IHardwareInfoProviderOutboundPort> _mockHardwareService;
        private readonly Mock<IOsEditionProviderOutboundPort> _mockOsEditionService;
        private readonly Mock<ISystemInfoProviderOutboundPort> _mockSystemInfoService;
        private readonly SystemInformationProvider _sut;

        public SystemInformationProviderTests()
        {
            _mockHardwareService = new Mock<IHardwareInfoProviderOutboundPort>();
            _mockOsEditionService = new Mock<IOsEditionProviderOutboundPort>();
            _mockSystemInfoService = new Mock<ISystemInfoProviderOutboundPort>();

            _sut = new SystemInformationProvider(
                _mockHardwareService.Object,
                _mockOsEditionService.Object,
                _mockSystemInfoService.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldAggregateAndMapAllDataCorrectly()
        {

            var fakeHardware = new HardwareInfo
            {
                ProcessorName = "AMD Ryzen 9 5900X",
                GraphicsCardName = "NVIDIA RTX 3080",
                InstalledRam = "32 GB"
            };

            _mockHardwareService.Setup(s => s.RetrieveHardwareInfoAsync()).ReturnsAsync(fakeHardware);

            _mockOsEditionService.Setup(s => s.RetrieveOsEditionAsync()).ReturnsAsync("Windows 11 Pro");

            _mockSystemInfoService.Setup(s => s.RetrieveCurrentOsDisplayVersion()).Returns("23H2");
            _mockSystemInfoService.Setup(s => s.RetrieveCurrentOsBuildVersion()).Returns("22631.3296");
            _mockSystemInfoService.Setup(s => s.RetrieveCurrentStandardBrowserName()).Returns("Firefox");

            var result = await _sut.RetrieveAsync();

            result.ShouldNotBeNull();

            result.ProcessorName.ShouldBe("AMD Ryzen 9 5900X");
            result.GraphicsCardName.ShouldBe("NVIDIA RTX 3080");
            result.InstalledRam.ShouldBe("32 GB");

            result.OsEdition.ShouldBe("Windows 11 Pro");
            result.OsDisplayVersion.ShouldBe("23H2");
            result.OsBuildVersion.ShouldBe("22631.3296");
            result.StandardBrowserName.ShouldBe("Firefox");

            _mockHardwareService.Verify(s => s.RetrieveHardwareInfoAsync(), Times.Once);
            _mockOsEditionService.Verify(s => s.RetrieveOsEditionAsync(), Times.Once);
            _mockSystemInfoService.Verify(s => s.RetrieveCurrentOsDisplayVersion(), Times.Once);
            _mockSystemInfoService.Verify(s => s.RetrieveCurrentOsBuildVersion(), Times.Once);
            _mockSystemInfoService.Verify(s => s.RetrieveCurrentStandardBrowserName(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldBubbleUpExceptions_WhenDependencyThrows()
        {
            _mockHardwareService
                .Setup(s => s.RetrieveHardwareInfoAsync())
                .ThrowsAsync(new UnauthorizedAccessException("WMI Access Denied"));

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.RetrieveAsync());
            exception.Message.ShouldBe("WMI Access Denied");
        }
    }
}


