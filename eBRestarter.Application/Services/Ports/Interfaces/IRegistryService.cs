using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Application.Services.Ports.Interfaces
{
    public interface IRegistryService
    {
        void SetCurrentUserValue(string subKey, string name, object value);
        void DeleteCurrentUserValue(string subKey, string name);
        void SetLocalMachineValue(string subKey, string name, object value, RegistryValueKind kind);


        object? GetCurrentUserValue(string subKey, string valueName);

        Dictionary<string, object> GetCurrentUserValues(string subKey);

        object? GetLocalMachineValue(string subKey, string valueName);
    }
}
