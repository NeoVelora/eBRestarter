using eBRestarter.Core.Domain.Enums;

namespace eBRestarter.Core.Domain.Models;

public class ApiResponse
{
    public bool IsSuccess { get; set; }
    public string? Content { get; set; }
    public ResponseCode StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}
