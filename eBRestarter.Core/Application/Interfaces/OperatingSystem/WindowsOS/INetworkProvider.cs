using System.Net.NetworkInformation;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

public interface INetworkProvider
{
    bool CheckIsNetworkAvailable();
    NetworkInterface[] RetrieveAllNetworkInterfaces();
}
