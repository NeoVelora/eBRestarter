using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.UseCases.GetSystemInformation;
using Moq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Tests.Core.Application.UseCases.GetSystemInformation
{
    public class GetSystemInformationServiceTests
    {
        private readonly Mock<IHardwareInfoService> _mockHardwareService;
        private readonly Mock<IOsEditionService> _mockOsEditionService;
        private readonly Mock<IWindowsSystemInfoService> _mockSystemInfoService;
        private readonly GetSystemInformationService _sut;

        public GetSystemInformationServiceTests()
        {
            _mockHardwareService = new Mock<IHardwareInfoService>();
            _mockOsEditionService = new Mock<IOsEditionService>();
            _mockSystemInfoService = new Mock<IWindowsSystemInfoService>();

            _sut = new GetSystemInformationService(
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

            _mockHardwareService.Setup(s => s.GetHardwareInfoAsync()).ReturnsAsync(fakeHardware);


            _mockHardwareService.Setup(s => s.GetHardwareInfoAsync()).ReturnsAsync(fakeHardware);

            _mockOsEditionService.Setup(s => s.GetOsEditionAsync()).ReturnsAsync("Windows 11 Pro");

            _mockSystemInfoService.Setup(s => s.GetCurrentOsDisplayVersion()).Returns("23H2");
            _mockSystemInfoService.Setup(s => s.GetCurrentOsBuildVersion()).Returns("22631.3296");
            _mockSystemInfoService.Setup(s => s.GetCurrentStandardBrowserName()).Returns("Firefox");

            var result = await _sut.ExecuteAsync();

            result.ShouldNotBeNull();

            result.ProcessorName.ShouldBe("AMD Ryzen 9 5900X");
            result.GraphicsCardName.ShouldBe("NVIDIA RTX 3080");
            result.InstalledRam.ShouldBe("32 GB");

            result.OsEdition.ShouldBe("Windows 11 Pro");
            result.OsDisplayVersion.ShouldBe("23H2");
            result.OsBuildVersion.ShouldBe("22631.3296");
            result.StandardBrowserName.ShouldBe("Firefox");

            _mockHardwareService.Verify(s => s.GetHardwareInfoAsync(), Times.Once);
            _mockOsEditionService.Verify(s => s.GetOsEditionAsync(), Times.Once);
            _mockSystemInfoService.Verify(s => s.GetCurrentOsDisplayVersion(), Times.Once);
            _mockSystemInfoService.Verify(s => s.GetCurrentOsBuildVersion(), Times.Once);
            _mockSystemInfoService.Verify(s => s.GetCurrentStandardBrowserName(), Times.Once);
        }


        [Fact]
        public async Task ExecuteAsync_ShouldBubbleUpExceptions_WhenDependencyThrows()
        {
            _mockHardwareService
                .Setup(s => s.GetHardwareInfoAsync())
                .ThrowsAsync(new UnauthorizedAccessException("WMI Access Denied"));

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.ExecuteAsync());
            exception.Message.ShouldBe("WMI Access Denied");
        }
    }


}