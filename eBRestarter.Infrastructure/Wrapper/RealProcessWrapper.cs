using eBRestarter.Infrastructure.Wrapper.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.Infrastructure.Wrapper
{
    public class RealProcessWrapper : IProcessWrapper
    {
        public void Start(ProcessStartInfo info) => Process.Start(info);

        public Process[] GetProcessesByName(string name) => Process.GetProcessesByName(name);

        public bool IsProcessRunning(string name)
        {
            return Process.GetProcessesByName(name).Length > 0;
        }

        public void KillProcess(string name)
        {
            var processes = Process.GetProcessesByName(name);

            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }
}
