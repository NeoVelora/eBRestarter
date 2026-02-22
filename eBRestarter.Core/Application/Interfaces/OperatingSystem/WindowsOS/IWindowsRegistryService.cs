using Microsoft.Win32;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

public interface IWindowsRegistryService
{
    void SetCurrentUserValue(string subKey, string name, object value);
    void DeleteCurrentUserValue(string subKey, string name);
    void SetLocalMachineValue(string subKey, string name, object value, RegistryValueKind kind);


    object? GetCurrentUserValue(string subKey, string valueName);

    Dictionary<string, object> GetCurrentUserValues(string subKey);

    object? GetLocalMachineValue(string subKey, string valueName);
}
