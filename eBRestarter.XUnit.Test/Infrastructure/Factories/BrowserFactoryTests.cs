using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Factories;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.XUnit.Test.Infrastructure.Factories
{
    public class AdapterBrowserFactoryProviderAdapterTests
    {
        [Theory]
        [InlineData(BrowserType.Chrome, typeof(AdapterChromeBrowserWrapper))]
        [InlineData(BrowserType.Firefox, typeof(AdapterFirefoxBrowserWrapper))]
        [InlineData(BrowserType.Edge, typeof(AdapterEdgeBrowserWrapper))]
        [InlineData(BrowserType.Brave, typeof(AdapterBraveBrowserWrapper))]
        [InlineData(BrowserType.Vivaldi, typeof(AdapterVivaldiBrowserWrapper))]
        public void Create_ShouldReturnCorrectBrowserInstance_ForValidBrowserType(BrowserType inputType, Type expectedClassType)
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFs = Substitute.For<IOutboundPortFileSystem>();

            var fakeChrome = new AdapterChromeBrowserWrapper(mockProcess, mockSettings, mockFs, new FakeLogger<AdapterChromeBrowserWrapper>());
            var fakeFirefox = new AdapterFirefoxBrowserWrapper(mockProcess, mockSettings, mockFs, new FakeLogger<AdapterFirefoxBrowserWrapper>());
            var fakeEdge = new AdapterEdgeBrowserWrapper(mockProcess, mockSettings, mockFs, new FakeLogger<AdapterEdgeBrowserWrapper>());
            var fakeBrave = new AdapterBraveBrowserWrapper(mockProcess, mockSettings, mockFs, new FakeLogger<AdapterBraveBrowserWrapper>());
            var fakeVivaldi = new AdapterVivaldiBrowserWrapper(mockProcess, mockSettings, mockFs, new FakeLogger<AdapterVivaldiBrowserWrapper>());

            var services = new ServiceCollection();
            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Chrome, fakeChrome);
            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Firefox, fakeFirefox);
            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Edge, fakeEdge);
            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Brave, fakeBrave);
            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Vivaldi, fakeVivaldi);

            var factory = new AdapterBrowserFactory(services.BuildServiceProvider());

            var result = factory.Create(inputType);

            result.ShouldNotBeNull();
            result.ShouldBeOfType(expectedClassType);
        }

        [Fact]
        public void Create_ShouldThrowNotSupportedException_ForInvalidBrowserType()
        {
            var factory = new AdapterBrowserFactory(new ServiceCollection().BuildServiceProvider());

            var invalidBrowserType = (BrowserType)999;

            Action act = () => factory.Create(invalidBrowserType);

            var exception = act.ShouldThrow<NotSupportedException>();
            exception.Message.ShouldContain("is not supported");
            exception.Message.ShouldContain(nameof(AdapterBrowserFactory));
        }

        [Fact]
        public void Create_ShouldResolveBrowser_ViaKeyedServices()
        {
            var services = new ServiceCollection();
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFs = Substitute.For<IOutboundPortFileSystem>();
            var fakeChrome = new AdapterChromeBrowserWrapper(mockProcess, mockSettings, mockFs, new FakeLogger<AdapterChromeBrowserWrapper>());

            services.AddKeyedSingleton<IOutboundPortBrowser>(BrowserType.Chrome, fakeChrome);
            var sp = services.BuildServiceProvider();
            var factory = new AdapterBrowserFactory(sp);

            var result = factory.Create(BrowserType.Chrome);

            result.ShouldBeSameAs(fakeChrome);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenServiceProviderIsNull()
        {
            Action act = () => new AdapterBrowserFactory(null!);

            act.ShouldThrow<ArgumentNullException>()
                .ParamName.ShouldBe("serviceProvider");
        }
    }
}
