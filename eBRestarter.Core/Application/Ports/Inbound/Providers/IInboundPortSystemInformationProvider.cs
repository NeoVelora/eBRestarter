using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IInboundPortSystemInformationProvider
{
    Task<SystemInformationResponse> RetrieveAsync();
}
