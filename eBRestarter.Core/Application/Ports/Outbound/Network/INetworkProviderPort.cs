using System.Net.NetworkInformation;

namespace eBRestarter.Core.Application.Ports.Outbound.Network;

public interface INetworkProviderPort
{
    bool CheckIsNetworkAvailable();
    NetworkInterface[] RetrieveAllNetworkInterfaces();
}


