using eBRestarter.Core.Application.Models;
using eBRestarter.Core.Application.Models.Config;

namespace eBRestarter.Core.Application.Interfaces;

public interface IRestartTaskDisplayStateService
{

    RestartTaskDisplayState GetInitialState(AppConfig config);
}
