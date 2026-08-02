using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;

namespace eBRestarter.Infrastructure.BehavioralComponents.Providers;

public sealed class NetworkInfoProvider(
    IOutboundPortNetworkProvider networkProvider)
    : IInboundPortNetworkInfoProvider
{

    private readonly IOutboundPortNetworkProvider _networkProvider = networkProvider ?? throw new ArgumentNullException(nameof(networkProvider));


    public bool IsNetworkAvailable() => _networkProvider.CheckIsNetworkAvailable();

    public IEnumerable<NetworkStats> RetrieveActiveInterfaces()
    {
        var interfaces = _networkProvider.RetrieveAllNetworkInterfaces();

        foreach (var networkInterface in interfaces)
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            var ipv4Statistics = networkInterface.GetIPv4Statistics();

            if (ipv4Statistics.BytesSent == 0 && ipv4Statistics.BytesReceived == 0)
            {
                continue;
            }

            yield return new NetworkStats(
                Name: networkInterface.Name,
                BytesReceived: ipv4Statistics.BytesReceived,
                BytesSent: ipv4Statistics.BytesSent,
                IsActive: true);
        }
    }
}
