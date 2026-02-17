using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Services.Interfaces
{
    public interface ILanguageService
    {
        #region PublicProperties

        string CurrentLanguageCode { get; }

        #endregion

        #region PublicMethods

        void SetLanguage(string languageCode);

        #endregion
    }
}
