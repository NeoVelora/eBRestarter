using eBRestarter.Core.Application.Models;
using eBRestarter.Core.Domain.Entities;

namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IRestartTaskDisplayStateProvider
{
    RestartTaskDisplayState RetrieveInitialState(AppConfig config);
}