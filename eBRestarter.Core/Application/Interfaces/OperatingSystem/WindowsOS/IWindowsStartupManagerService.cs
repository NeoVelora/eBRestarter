using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS
{
    public interface IWindowsStartupManagerService
    {
        Task EnableAutoStartAsync();
        Task DisableAutoStartAsync();
        Task<bool> IsAutoStartEnabledAsync();

        void EnableAutoStart();
        void DisableAutoStart();
        Dictionary<string, object> GetStartupEntries();
        void SetEdgeStartupBoost(bool enable);

        bool IsEdgeStartupBoostEnabled();
        void SetAutoLogon(bool enable);
    }
}
