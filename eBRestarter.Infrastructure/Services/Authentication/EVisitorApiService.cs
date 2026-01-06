using eBRestarter.Core.Application.Interfaces.Authentication;
using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Core.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Infrastructure.Services.Authentication
{
    public class EVisitorApiService : IApiAuthenticationService
    {
        private readonly IRestClientService _restClient;

        public EVisitorApiService(IRestClientService restClient)
        {
            _restClient = restClient;
        }

        public async Task<(bool IsValid, string Message)> VerifyCredentialsAsync(string username, string apiKey)
        {
            var request = new ApiRequest
            {
                Url = "https://www.ebesucher.de/api/visitor_exchange.json/account/status",
                Username = username,
                Password = apiKey,
                TimeoutSeconds = 10
            };

            var response = await _restClient.ExecuteGetAsync(request);

            if (response.IsSuccess)
            {
                return (true, "Verbindung erfolgreich!");
            }
            else if (response.StatusCode == ResponseCode.HttpRE401)
            {
                return (false, "Benutzername oder API-Schlüssel falsch.");
            }
            else
            {
                return (false, $"Fehler: {response.ErrorMessage}");
            }
        }
    }
}
