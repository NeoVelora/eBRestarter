namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOutboundPortAutoStartRepository
{
    Task EnableAutoStartAsync();
    Task DisableAutoStartAsync();
    Task<bool> IsAutoStartEnabledAsync();
    void EnableAutoStart();
    void DisableAutoStart();
    Dictionary<string, object> RetrieveStartupEntries();
}
