using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Domain.Enums
{
    public enum RestartTaskState
    {
        Idle,
        InitialDelay,
        Running,
        Cooldown
    }
}
