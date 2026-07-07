using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;

public interface IInboundPortSystemInformationProvider
{
    Task<SystemInformationResponse> RetrieveAsync();
}
