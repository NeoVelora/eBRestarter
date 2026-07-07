using eBRestarter.Infrastructure.Models.Network;

namespace eBRestarter.Infrastructure.Api.Interfaces;

public interface IRestClient
{
    Task<ApiResponse> ExecuteGetAsync(ApiRequest request);

    ApiResponse ExecuteGet(ApiRequest request);
}


