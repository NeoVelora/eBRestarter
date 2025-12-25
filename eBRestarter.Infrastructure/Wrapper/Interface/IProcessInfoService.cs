using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Infrastructure.Wrapper.Interface
{
    public interface IProcessInfoService
    {
        string GetCurrentExecutablePath();
    }
}
