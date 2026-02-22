using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.Windows.Globalization;

namespace eBRestarter.Desktop.WinUI3.Services;

public class LanguageService : ILanguageService
{
    #region PublicProperties

    public string CurrentLanguageCode => ApplicationLanguages.PrimaryLanguageOverride;

    #endregion

    #region PublicMethods

    public void SetLanguage(string languageCode)
    {
        ApplicationLanguages.PrimaryLanguageOverride = languageCode;
    }

    #endregion
}
