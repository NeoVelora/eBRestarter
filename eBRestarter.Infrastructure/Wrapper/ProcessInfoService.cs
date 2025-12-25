using eBRestarter.Infrastructure.Wrapper.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.Infrastructure.Wrapper
{
    public class ProcessInfoService : IProcessInfoService
    {
        public string GetCurrentExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule!.FileName;
        }
    }
}
