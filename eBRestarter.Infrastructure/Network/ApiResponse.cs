using eBRestarter.Infrastructure.Network;
using eBRestarter.Infrastructure.Api;
using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Infrastructure.Network;

public sealed class ApiResponse
{
    public bool IsSuccess { get; set; }
    public string? Content { get; set; }
    public ResponseCode StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}


