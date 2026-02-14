namespace eBRestarter.Core.Application.Interfaces
{
    public interface ICacheDeletionIntervalValidator
    {
        #region PublicMethods

        bool IsValidIntervalDays(int days);

        #endregion
    }
}
