using eBRestarter.Core.Application.Enums;

namespace eBRestarter.Core.Application.Models.Api;

public class ApiResponse
{
    public bool IsSuccess { get; set; }
    public string? Content { get; set; }
    public ResponseCode StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}
