using eBRestarter.Application.Services.Ports.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Application.Facade.Interfaces
{
    public interface IOperatingSystemFacade
    {
        IProcessControlService Process { get; }
        ISystemInfoService SystemInfo { get; }
        IRegistryService Registry { get; }
        IStartupManagerService Startup { get; }
        IFileSystemService FileSystem { get; }
    }
}
