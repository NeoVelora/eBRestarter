using eBRestarter.Core.Application.Interfaces;

namespace eBRestarter.Core.Application.Services;

public class CacheDeletionIntervalValidator : ICacheDeletionIntervalValidator
{
    #region PublicAndProtectedMethods

    public bool IsValidIntervalDays(int days) => days switch
    {
        1 or 3 or 7 or 14 => true,
        _ => false
    };

    #endregion
}
