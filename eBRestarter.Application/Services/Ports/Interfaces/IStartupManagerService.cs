using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Application.Services.Ports.Interfaces
{
    public interface IStartupManagerService
    {
        void EnableAutoStart();
        void DisableAutoStart();
        Dictionary<string, object> GetStartupEntries();
        void SetEdgeStartupBoost(bool enable);
        void SetAutoLogon(bool enable);
    }
}
