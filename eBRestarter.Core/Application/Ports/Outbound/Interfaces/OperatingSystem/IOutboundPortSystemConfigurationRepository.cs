namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortSystemConfigurationRepository
{
    void SetUserValue(string subKey, string name, object value);
    void DeleteUserValue(string subKey, string name);
    void SetSystemValue(string subKey, string name, object value);
    object? GetUserValue(string subKey, string valueName);
    Dictionary<string, object> GetUserValues(string subKey);
    object? GetSystemValue(string subKey, string valueName);
}
