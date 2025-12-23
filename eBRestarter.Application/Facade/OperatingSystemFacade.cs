using eBRestarter.Application.Facade.Interfaces;
using eBRestarter.Application.Services.Ports.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Application.Facade
{
    public class OperatingSystemFacade : IOperatingSystemFacade
    {
        public IProcessControlService Process { get; }
        public ISystemInfoService SystemInfo { get; }
        public IStartupManagerService Startup { get; }

        public OperatingSystemFacade(
            IProcessControlService process,
            ISystemInfoService systemInfo,
            IStartupManagerService startup)
        {
            Process = process;
            SystemInfo = systemInfo;
            Startup = startup;
        }
    }
}
