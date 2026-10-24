using System;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Config;

namespace eBRestarter.Core.Application.BehavioralComponents.Providers;

public sealed class StartupConfigProvider(IOutboundPortEVisitorConfigRepository configService) : IInboundPortStartupConfigProvider
{
    private const int GermanLanguageCode = 0;

    private const string DefaultEnglishCultureCode = "en-US";
    private const string DefaultGermanCultureCode = "de-DE";
    private const string DefaultThemeName = "Light";

    private readonly IOutboundPortEVisitorConfigRepository _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    public StartupDisplayPreferences RetrieveStartupPreferences()
    {
        var appConfig = _configService.LoadConfig();

        string languageCode = appConfig?.Settings?.Language == GermanLanguageCode
            ? DefaultGermanCultureCode
            : DefaultEnglishCultureCode;

        string? rawTheme = appConfig?.Settings?.Theme;
        string themeName = string.IsNullOrWhiteSpace(rawTheme)
            ? DefaultThemeName
            : rawTheme;

        return new StartupDisplayPreferences(languageCode, themeName);
    }
}
