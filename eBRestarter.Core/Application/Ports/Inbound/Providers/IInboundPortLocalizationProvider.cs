namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IInboundPortLocalizationProvider
{
    string RetrieveString(string key);
}
