using eBRestarter.Core.Application.ObjectArchetypes.DTOs.ImmutableSnapshot;
using eBRestarter.Core.Domain.Entities;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Handlers;

public interface IInboundPortRestartTaskDisplayStateHandler
{
    RestartTaskDisplayState RetrieveInitialState(AppConfig config);
}