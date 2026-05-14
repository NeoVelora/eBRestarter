using Microsoft.Win32;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

public interface IWindowsRegistryService
{
    void SetCurrentUserValue(string subKey, string name, object value);
    void DeleteCurrentUserValue(string subKey, string name);
    void SetLocalMachineValue(string subKey, string name, object value, RegistryValueKind kind);

    object? RetrieveCurrentUserValue(string subKey, string valueName);

    Dictionary<string, object> RetrieveCurrentUserValues(string subKey);

    object? RetrieveLocalMachineValue(string subKey, string valueName);
}
