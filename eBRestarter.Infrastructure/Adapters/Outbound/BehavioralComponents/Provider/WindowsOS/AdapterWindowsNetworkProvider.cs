using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;
using System.Net.NetworkInformation;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS;

public sealed class AdapterWindowsNetworkProvider : IOutboundPortNetworkProvider
{
    public bool CheckIsNetworkAvailable()
    {
        return NetworkInterface.GetIsNetworkAvailable();
    }

    public NetworkInterface[] RetrieveAllNetworkInterfaces()
    {
        return NetworkInterface.GetAllNetworkInterfaces();
    }
}


