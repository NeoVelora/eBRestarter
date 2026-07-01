using eBRestarter.Core.Application.Ports.Outbound;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Infrastructure.Adapters.Browsers;
using eBRestarter.Infrastructure.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Factories
{
    public class BrowserFactoryProviderAdapterTests
    {
        [Theory]
        [InlineData(BrowserType.Chrome, typeof(ChromeBrowser))]
        [InlineData(BrowserType.Firefox, typeof(FirefoxBrowser))]
        [InlineData(BrowserType.Edge, typeof(EdgeBrowser))]
        [InlineData(BrowserType.Brave, typeof(BraveBrowser))]
        [InlineData(BrowserType.Vivaldi, typeof(VivaldiBrowser))]
        public void Create_ShouldReturnCorrectBrowserInstance_ForValidBrowserType(BrowserType inputType, Type expectedClassType)
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockProcess = new Mock<IOsProcessControlOutboundPort>();
            var mockSettings = new Mock<ISettingsRepositoryOutboundPort>();
            var mockFs = new Mock<IFileSystemOutboundPort>();

            var fakeChrome = new ChromeBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<ChromeBrowser>>().Object);
            var fakeFirefox = new FirefoxBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<FirefoxBrowser>>().Object);
            var fakeEdge = new EdgeBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<EdgeBrowser>>().Object);
            var fakeBrave = new BraveBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<BraveBrowser>>().Object);
            var fakeVivaldi = new VivaldiBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<VivaldiBrowser>>().Object);

            mockServiceProvider.Setup(sp => sp.GetService(typeof(ChromeBrowser))).Returns(fakeChrome);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(FirefoxBrowser))).Returns(fakeFirefox);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(EdgeBrowser))).Returns(fakeEdge);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(BraveBrowser))).Returns(fakeBrave);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(VivaldiBrowser))).Returns(fakeVivaldi);

            var factory = new BrowserFactory(mockServiceProvider.Object);
            var result = factory.Create(inputType);
            result.ShouldNotBeNull();

            result.ShouldBeOfType(expectedClassType);
        }

        [Fact]
        public void Create_ShouldThrowNotSupportedException_ForInvalidBrowserType()
        {
            var mockServiceProvider = new Mock<IServiceProvider>();
            var factory = new BrowserFactory(mockServiceProvider.Object);

            var invalidBrowserType = (BrowserType)999;
            var exception = Should.Throw<NotSupportedException>(() =>
            {
                factory.Create(invalidBrowserType);
            });

            exception.Message.ShouldContain("ist noch nicht implementiert");
        }
    }
}


