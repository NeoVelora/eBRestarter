using NSubstitute;
using Shouldly;
using Xunit;
using eBRestarter.Core.Application.BehavioralComponents.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Domain.ValueObjects;

namespace eBRestarter.XUnit.Test.UnitTests.Core.Application.Providers;

[Trait("Category", "Unit")]
public class StartupConfigProviderTests
{
    [Fact]
    public void RetrieveStartupPreferences_ReturnsCorrectPreferences_WithoutSaving()
    {
        var config = new AppConfig
        {
            Settings = new SettingsConfig { Language = 0, Theme = "Dark" }
        };

        var mockConfigService = Substitute.For<IOutboundPortEVisitorConfigRepository>();
        mockConfigService.LoadConfig().Returns(config);

        var sut = new StartupConfigProvider(mockConfigService);

        var prefs = sut.RetrieveStartupPreferences();

        prefs.LanguageCode.ShouldBe("de-DE");
        prefs.ThemeName.ShouldBe("Dark");
        mockConfigService.DidNotReceive().SaveConfig(Arg.Any<AppConfig>());
    }
}


