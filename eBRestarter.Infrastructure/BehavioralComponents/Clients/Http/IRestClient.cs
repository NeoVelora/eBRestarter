using eBRestarter.Infrastructure.ObjectArchetypes.Models;

namespace eBRestarter.Infrastructure.BehavioralComponents.Clients.Http;

public interface IRestClient
{
    Task<ApiResponse> ExecuteGetAsync(ApiRequest request, CancellationToken cancellationToken = default);

    ApiResponse ExecuteGet(ApiRequest request);
}

