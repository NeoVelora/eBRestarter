using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.Infrastructure.Wrapper.Interface
{
    public interface IProcessWrapper
    {
        Process? Start(ProcessStartInfo info);

        bool IsProcessRunning(string name);

        void KillProcess(string name);

        Process[] GetProcesses();

    }
}
