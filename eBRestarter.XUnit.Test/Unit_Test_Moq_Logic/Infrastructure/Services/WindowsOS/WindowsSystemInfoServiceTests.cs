using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.XUnit.Test.Unit_Test_Moq_Logic.Infrastructure.Services.WindowsOS
{
    public class WindowsSystemInfoServiceTests
    {
        private readonly Mock<ILogger<WindowsSystemInfoService>> _loggerMock;
        private readonly Mock<IRegistryService> _registryMock;
        private readonly WindowsSystemInfoService _windowsSystemInfoService;

        public WindowsSystemInfoServiceTests()
        {
            _loggerMock = new Mock<ILogger<WindowsSystemInfoService>>();
            _registryMock = new Mock<IRegistryService>();

            _windowsSystemInfoService = new WindowsSystemInfoService(_loggerMock.Object, _registryMock.Object);
        }

        [Theory]
        [InlineData("ChromeHTML", "Chrome")]
        [InlineData("FirefoxURL-ABC", "Firefox")]
        [InlineData("MSEdgeHTM", "Edge")]
        [InlineData("OperaStable", "Opera")]
        [InlineData("SomethingElse", "-")]
        public void GetCurrentStandardBrowserName_Should_Identify_Correct_Browser(string progId, string expectedName)
        {
            _registryMock.Setup(x => x.GetCurrentUserValue(It.Is<string>(s => s.Contains("http")), "ProgId"))
                         .Returns(progId);

            var result = _windowsSystemInfoService.GetCurrentStandardBrowserName();

            result.ShouldBe(expectedName);
        }

        [Fact]
        public void GetCurrentStandardBrowserName_Should_Return_Dash_If_Registry_Returns_Null()
        {
            _registryMock.Setup(x => x.GetCurrentUserValue(It.IsAny<string>(), "ProgId"))
                         .Returns((object?)null);

            var result = _windowsSystemInfoService.GetCurrentStandardBrowserName();

            result.ShouldBe("-");
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_Should_Return_Version_From_Registry()
        {
            _registryMock.Setup(x => x.GetLocalMachineValue(It.IsAny<string>(), "DisplayVersion"))
                         .Returns("22H2");

            var result = _windowsSystemInfoService.GetCurrentOsDisplayVersion();

            result.ShouldBe("22H2");
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_Should_Log_Error_And_Return_Error_On_Exception()
        {
            _registryMock.Setup(x => x.GetLocalMachineValue(It.IsAny<string>(), "DisplayVersion"))
                         .Throws(new Exception("Access Denied"));

            var result = _windowsSystemInfoService.GetCurrentOsDisplayVersion();

            result.ShouldBe("Error");

            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
    }
}
