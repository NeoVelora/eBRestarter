using eBRestarter.Infrastructure.ObjectArchetypes.Model;

namespace eBRestarter.Infrastructure.Api.Interfaces;

public interface IRestClient
{
    Task<ApiResponse> ExecuteGetAsync(ApiRequest request, CancellationToken cancellationToken = default);

    ApiResponse ExecuteGet(ApiRequest request);
}


