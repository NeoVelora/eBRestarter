using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Infrastructure.Wrapper;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;

namespace eBRestarter.XUnit.Test.IntegrationTests.Infrastructure.Services.WindowsOS
{
    [SupportedOSPlatform("windows")]
    public class WindowsSystemInfoServiceIntegrationTests
    {
        private readonly WindowsSystemInfoService _service;

        public WindowsSystemInfoServiceIntegrationTests()
        {
            var logger = NullLogger<WindowsSystemInfoService>.Instance;
            var registryService = new RegistryService();

            _service = new WindowsSystemInfoService(logger, registryService);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GetCurrentOsDisplayVersion_Should_Return_Valid_String()
        {
            var result = _service.GetCurrentOsDisplayVersion();

            result.ShouldNotBeNullOrWhiteSpace();
            result.ShouldNotBe("Error", "weil der Registry-Lesezugriff auf einem echten Windows klappen sollte.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GetCurrentOsBuildVersion_Should_Return_Version_Format()
        {
            var result = _service.GetCurrentOsBuildVersion();

            result.ShouldNotBeNullOrWhiteSpace();
            result.ShouldNotBe("Error");

            result.ShouldContain(".", customMessage: "weil eine Build-Version meistens Punkte enthält (z.B. 22621.1234)");

            var parts = result.Split('.');
            int.TryParse(parts[0], out _).ShouldBeTrue(customMessage: "weil der erste Teil der Version eine Zahl sein muss.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GetCurrentStandardBrowserName_Should_Return_Known_Browser_Or_Dash()
        {
            var browser = _service.GetCurrentStandardBrowserName();

            var validResponses = new[] { "Chrome", "Firefox", "Edge", "Opera", "Brave", "-" };

            browser.ShouldNotBeNull();

            validResponses.ShouldContain(browser,
                customMessage: $"weil der erkannte Browser '{browser}' entweder unterstützt oder '-' sein muss.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GetCurrentStandardBrowserName_Should_Not_Throw_Exception()
        {
            Action act = () => _service.GetCurrentStandardBrowserName();

            act.ShouldNotThrow();
        }
    }
}
