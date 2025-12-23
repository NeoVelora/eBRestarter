using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.Infrastructure.Wrapper.Interface
{
    public interface IProcessWrapper
    {
        Process[] GetProcessesByName(string name);
        void Start(ProcessStartInfo info);
        bool IsProcessRunning(string name);
        void KillProcess(string name);
        
    }
}
