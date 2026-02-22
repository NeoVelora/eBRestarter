using eBRestarter.Core.Domain.Models.Records;

namespace eBRestarter.Core.Application.Interfaces;

public interface IEVisitorApiService
{
    Task<IpInfoData?> GetIpInfoAsync();

    Task<EarningsData?> GetEarningsAsync();
}
