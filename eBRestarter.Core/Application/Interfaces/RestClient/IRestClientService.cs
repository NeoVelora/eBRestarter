using eBRestarter.Core.Application.Models.Api;

namespace eBRestarter.Core.Application.Interfaces.RestClient;

public interface IRestClientService
{
    Task<ApiResponse> ExecuteGetAsync(ApiRequest request);

    ApiResponse ExecuteGet(ApiRequest request);
}
