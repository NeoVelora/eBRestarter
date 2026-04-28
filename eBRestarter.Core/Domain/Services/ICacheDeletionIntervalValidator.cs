namespace eBRestarter.Core.Domain.Services;

public interface ICacheDeletionIntervalValidator
{
    bool IsValidIntervalDays(int days);
}
