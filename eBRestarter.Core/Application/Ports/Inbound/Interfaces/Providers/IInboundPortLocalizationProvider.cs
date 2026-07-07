namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;

public interface IInboundPortLocalizationProvider
{
    string RetrieveString(string key);
}
