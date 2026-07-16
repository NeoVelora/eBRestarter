using eBRestarter.Core.Application.Ports.Outbound;
using eBRestarter.Infrastructure.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.Browsers;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.XUnit.Test.Infrastructure.Factories
{
    public class BrowserFactoryProviderAdapterTests
    {
        [Theory]
        [InlineData(BrowserType.Chrome, typeof(AdapterChromeBrowser))]
        [InlineData(BrowserType.Firefox, typeof(AdapterFirefoxBrowser))]
        [InlineData(BrowserType.Edge, typeof(AdapterEdgeBrowser))]
        [InlineData(BrowserType.Brave, typeof(AdapterBraveBrowser))]
        [InlineData(BrowserType.Vivaldi, typeof(AdapterVivaldiBrowser))]
        public void Create_ShouldReturnCorrectBrowserInstance_ForValidBrowserType(BrowserType inputType, Type expectedClassType)
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockProcess = new Mock<IOutboundPortOsProcessControl>();
            var mockSettings = new Mock<IOutboundPortSystemConfigurationRepository>();
            var mockFs = new Mock<IOutboundPortFileSystem>();

            var fakeChrome = new AdapterChromeBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterChromeBrowser>>().Object);
            var fakeFirefox = new AdapterFirefoxBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterFirefoxBrowser>>().Object);
            var fakeEdge = new AdapterEdgeBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterEdgeBrowser>>().Object);
            var fakeBrave = new AdapterBraveBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterBraveBrowser>>().Object);
            var fakeVivaldi = new AdapterVivaldiBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterVivaldiBrowser>>().Object);

            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterChromeBrowser))).Returns(fakeChrome);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterFirefoxBrowser))).Returns(fakeFirefox);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterEdgeBrowser))).Returns(fakeEdge);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterBraveBrowser))).Returns(fakeBrave);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterVivaldiBrowser))).Returns(fakeVivaldi);

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

        [Fact]
        public void Create_ShouldResolveBrowser_ViaKeyedServices()
        {
            var services = new ServiceCollection();
            var mockProcess = new Mock<IOutboundPortOsProcessControl>();
            var mockSettings = new Mock<IOutboundPortSystemConfigurationRepository>();
            var mockFs = new Mock<IOutboundPortFileSystem>();
            var fakeChrome = new AdapterChromeBrowser(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterChromeBrowser>>().Object);

            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Chrome, fakeChrome);
            var sp = services.BuildServiceProvider();
            var factory = new BrowserFactory(sp);

            var result = factory.Create(BrowserType.Chrome);

            result.ShouldBeSameAs(fakeChrome);
        }
    }
}


