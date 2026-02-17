using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Services.Interfaces
{
    public interface IThemeService
    {
        #region PublicProperties

        string CurrentTheme { get; }

        #endregion

        #region PublicMethods

        void SetTheme(string themeName);

        #endregion
    }
}
