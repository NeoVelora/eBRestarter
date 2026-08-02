using eBRestarter.Core.Application.Ports.Outbound;
using eBRestarter.Infrastructure.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;

namespace eBRestarter.XUnit.Test.Infrastructure.Factories
{
    public class BrowserFactoryProviderAdapterTests
    {
        [Theory]
        [InlineData(BrowserType.Chrome, typeof(AdapterChromeBrowserWrapper))]
        [InlineData(BrowserType.Firefox, typeof(AdapterFirefoxBrowserWrapper))]
        [InlineData(BrowserType.Edge, typeof(AdapterEdgeBrowserWrapper))]
        [InlineData(BrowserType.Brave, typeof(AdapterBraveBrowserWrapper))]
        [InlineData(BrowserType.Vivaldi, typeof(AdapterVivaldiBrowserWrapper))]
        public void Create_ShouldReturnCorrectBrowserInstance_ForValidBrowserType(BrowserType inputType, Type expectedClassType)
        {
            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockProcess = new Mock<IOutboundPortOsProcessControl>();
            var mockSettings = new Mock<IOutboundPortSystemConfigurationRepository>();
            var mockFs = new Mock<IOutboundPortFileSystem>();

            var fakeChrome = new AdapterChromeBrowserWrapper(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterChromeBrowserWrapper>>().Object);
            var fakeFirefox = new AdapterFirefoxBrowserWrapper(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterFirefoxBrowserWrapper>>().Object);
            var fakeEdge = new AdapterEdgeBrowserWrapper(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterEdgeBrowserWrapper>>().Object);
            var fakeBrave = new AdapterBraveBrowserWrapper(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterBraveBrowserWrapper>>().Object);
            var fakeVivaldi = new AdapterVivaldiBrowserWrapper(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterVivaldiBrowserWrapper>>().Object);

            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterChromeBrowserWrapper))).Returns(fakeChrome);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterFirefoxBrowserWrapper))).Returns(fakeFirefox);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterEdgeBrowserWrapper))).Returns(fakeEdge);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterBraveBrowserWrapper))).Returns(fakeBrave);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(AdapterVivaldiBrowserWrapper))).Returns(fakeVivaldi);

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
            var fakeChrome = new AdapterChromeBrowserWrapper(mockProcess.Object, mockSettings.Object, mockFs.Object, new Mock<ILogger<AdapterChromeBrowserWrapper>>().Object);

            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Chrome, fakeChrome);
            var sp = services.BuildServiceProvider();
            var factory = new BrowserFactory(sp);

            var result = factory.Create(BrowserType.Chrome);

            result.ShouldBeSameAs(fakeChrome);
        }
    }
}


