using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Infrastructure.Browsers;
using eBRestarter.Infrastructure.Factories;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Factories
{
    public class BrowserFactoryTests
    {
        [Theory]
        [InlineData(BrowserType.Chrome, typeof(ChromeBrowserAdapter))]
        [InlineData(BrowserType.Firefox, typeof(FirefoxBrowserAdapter))]
        [InlineData(BrowserType.Edge, typeof(EdgeBrowserAdapter))]
        [InlineData(BrowserType.Brave, typeof(BraveBrowserAdapter))]
        [InlineData(BrowserType.Vivaldi, typeof(VivaldiBrowserAdapter))]
        public void Create_ShouldReturnCorrectBrowserInstance_ForValidBrowserType(BrowserType inputType, Type expectedClassType)
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockOs = new Mock<IOperatingSystemFacade>();

            var fakeChrome = new ChromeBrowserAdapter(mockOs.Object, new Mock<ILogger<ChromeBrowserAdapter>>().Object);
            var fakeFirefox = new FirefoxBrowserAdapter(mockOs.Object, new Mock<ILogger<FirefoxBrowserAdapter>>().Object);
            var fakeEdge = new EdgeBrowserAdapter(mockOs.Object, new Mock<ILogger<EdgeBrowserAdapter>>().Object);
            var fakeBrave = new BraveBrowserAdapter(mockOs.Object, new Mock<ILogger<BraveBrowserAdapter>>().Object);
            var fakeVivaldi = new VivaldiBrowserAdapter(mockOs.Object, new Mock<ILogger<VivaldiBrowserAdapter>>().Object);

            mockServiceProvider.Setup(sp => sp.GetService(typeof(ChromeBrowserAdapter))).Returns(fakeChrome);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(FirefoxBrowserAdapter))).Returns(fakeFirefox);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(EdgeBrowserAdapter))).Returns(fakeEdge);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(BraveBrowserAdapter))).Returns(fakeBrave);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(VivaldiBrowserAdapter))).Returns(fakeVivaldi);

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


