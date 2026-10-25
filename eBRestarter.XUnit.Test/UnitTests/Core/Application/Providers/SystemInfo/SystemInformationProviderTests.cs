using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.BehavioralComponents.Providers;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using NSubstitute.ExceptionExtensions;

namespace eBRestarter.XUnit.Test.UnitTests.Core.Application.Providers.SystemInfo
{
    [Trait("Category", "Unit")]
    public class SystemInformationProviderTests
    {
        private readonly IOutboundPortHardwareInfoProvider _mockHardwareService;
        private readonly IOutboundPortOsEditionProvider _mockOsEditionService;
        private readonly IOutboundPortSystemInfoProvider _mockSystemInfoService;
        private readonly SystemInformationProvider _sut;

        public SystemInformationProviderTests()
        {
            _mockHardwareService = Substitute.For<IOutboundPortHardwareInfoProvider>();
            _mockOsEditionService = Substitute.For<IOutboundPortOsEditionProvider>();
            _mockSystemInfoService = Substitute.For<IOutboundPortSystemInfoProvider>();

            _sut = new SystemInformationProvider(
                _mockHardwareService,
                _mockOsEditionService,
                _mockSystemInfoService);
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

            _mockHardwareService.RetrieveHardwareInfoAsync().Returns(fakeHardware);

            _mockOsEditionService.RetrieveOsEditionAsync().Returns("Windows 11 Pro");

            _mockSystemInfoService.RetrieveCurrentOsDisplayVersion().Returns("23H2");
            _mockSystemInfoService.RetrieveCurrentOsBuildVersion().Returns("22631.3296");
            _mockSystemInfoService.RetrieveCurrentStandardBrowserName().Returns("Firefox");

            var result = await _sut.RetrieveAsync();

            result.ShouldNotBeNull();

            result.ProcessorName.ShouldBe("AMD Ryzen 9 5900X");
            result.GraphicsCardName.ShouldBe("NVIDIA RTX 3080");
            result.InstalledRam.ShouldBe("32 GB");

            result.OsEdition.ShouldBe("Windows 11 Pro");
            result.OsDisplayVersion.ShouldBe("23H2");
            result.OsBuildVersion.ShouldBe("22631.3296");
            result.StandardBrowserName.ShouldBe("Firefox");

            await _mockHardwareService.Received(1).RetrieveHardwareInfoAsync();
            await _mockOsEditionService.Received(1).RetrieveOsEditionAsync();
            _mockSystemInfoService.Received(1).RetrieveCurrentOsDisplayVersion();
            _mockSystemInfoService.Received(1).RetrieveCurrentOsBuildVersion();
            _mockSystemInfoService.Received(1).RetrieveCurrentStandardBrowserName();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldBubbleUpExceptions_WhenDependencyThrows()
        {
            _mockHardwareService
                .RetrieveHardwareInfoAsync()
                .Throws(new UnauthorizedAccessException("WMI Access Denied"));

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.RetrieveAsync());
            exception.Message.ShouldBe("WMI Access Denied");
        }
    }
}


