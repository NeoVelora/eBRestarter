using System;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handlers.Interfaces;
using Microsoft.Windows.Globalization;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Handlers;

public sealed class LanguageHandler : ILanguageHandler
{
    public string CurrentLanguageCode => ApplicationLanguages.PrimaryLanguageOverride;

    public void SetLanguageOption(string languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        ApplicationLanguages.PrimaryLanguageOverride = languageCode;
    }
}
