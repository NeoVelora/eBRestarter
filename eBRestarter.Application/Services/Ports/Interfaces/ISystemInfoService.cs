using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Application.Services.Ports.Interfaces
{
    public interface ISystemInfoService
    {
        string GetCurrentStandardBrowserName();
        string GetCurrentOsBuildVersion();
        string GetCurrentOsDisplayVersion();
    }
}
