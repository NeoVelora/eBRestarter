using eBRestarter.Core.Application.Models;
using eBRestarter.Core.Domain.Models.Records.Config;

namespace eBRestarter.Core.Application.Interfaces;

public interface IRestartTaskDisplayStateService
{
    #region PublicMethods

    RestartTaskDisplayState GetInitialState(AppConfig config);

    #endregion
}
