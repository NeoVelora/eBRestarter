using eBRestarter.Core.Domain.Models.Records;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces
{
    public interface ILocalizationService
    {
        string GetString(string key);

        IEnumerable<LanguageOption> GetAvailableLanguages();

        IEnumerable<ComputerRestartOption> GetComputerRestartOptions();

        IEnumerable<BrowserCacheDeleteOption> GetBrowserCacheOptions();
    }
}
