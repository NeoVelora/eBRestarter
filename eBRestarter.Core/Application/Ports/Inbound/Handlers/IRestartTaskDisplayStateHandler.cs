using eBRestarter.Core.Application.Models;
using eBRestarter.Core.Domain.Entities;

namespace eBRestarter.Core.Application.Ports.Inbound.Handlers;

public interface IRestartTaskDisplayStateHandler
{
    RestartTaskDisplayState RetrieveInitialState(AppConfig config);
}