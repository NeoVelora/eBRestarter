using eBRestarter.Core.Application.Models;
using eBRestarter.Core.Domain.Entities;

namespace eBRestarter.Core.Application.Interfaces;

public interface IRestartTaskDisplayStateUseCase
{

    RestartTaskDisplayState RetrieveInitialState(AppConfig config);
}
