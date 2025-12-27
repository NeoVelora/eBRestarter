using eBRestarter.Core.Domain.Models.Records.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces.Config
{
    public interface IConfigService
    {
        AppConfig LoadConfig();

        void SaveConfig(AppConfig config);

        void ResetConfig();
    }
}
