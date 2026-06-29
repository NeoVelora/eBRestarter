namespace eBRestarter.Core.Domain.Validators;

public interface ICacheDeletionIntervalValidator
{
    bool IsValidIntervalDays(int days);
}