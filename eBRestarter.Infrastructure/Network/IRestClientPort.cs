namespace eBRestarter.Infrastructure.Network;

public interface IRestClientPort
{
    Task<ApiResponse> ExecuteGetAsync(ApiRequest request);

    ApiResponse ExecuteGet(ApiRequest request);
}


