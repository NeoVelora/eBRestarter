using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces.Authentication
{
    public interface IApiAuthenticationService
    {
        Task<(bool IsValid, string Message)> VerifyCredentialsAsync(string username, string apiKey);
    }
}
