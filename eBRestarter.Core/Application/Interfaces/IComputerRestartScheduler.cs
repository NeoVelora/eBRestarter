using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces
{
    public interface IComputerRestartScheduler
    {
        void StartScheduler();

        Task StopSchedulerAsync();
    }
}
