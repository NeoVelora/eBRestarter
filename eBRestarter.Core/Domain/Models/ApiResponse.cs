using eBRestarter.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Domain.Models
{
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string? Content { get; set; }
        public ResponseCode StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
