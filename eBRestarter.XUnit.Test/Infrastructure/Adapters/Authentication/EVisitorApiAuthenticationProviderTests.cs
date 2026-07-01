using eBRestarter.Infrastructure.Network;
using eBRestarter.Infrastructure.Adapters.Authentication;
using eBRestarter.Infrastructure.Adapters.RestSharp;
using eBRestarter.Infrastructure.Network;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Adapters.RestSharp;
using eBRestarter.Infrastructure.Network;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Infrastructure.Network;
using eBRestarter.Infrastructure.Repositories.Authentication;
using Moq;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Adapters.Authentication
{
    public class EVisitorApiAuthenticationProviderTests
    {
        [Fact]
        public async Task VerifyCredentialsAsync_ShouldReturnTrue_WhenApiCallIsSuccessful()
        {
            var mockRestClient = new Mock<IRestClientPort>();

            string testUsername = "TestUser";
            string testApiKey = "SecretKey123";

            var fakeSuccessResponse = new ApiResponse
            {
                IsSuccess = true,
                StatusCode = eBRestarter.Infrastructure.Api.ResponseCode.HttpRE200
            };

            mockRestClient
                .Setup(client => client.ExecuteGetAsync(It.IsAny<ApiRequest>()))
                .ReturnsAsync(fakeSuccessResponse);

            var apiUseCase = new EVisitorApiAuthenticationProvider(mockRestClient.Object);

            var (isValid, message) = await apiUseCase.VerifyCredentialsAsync(testUsername, testApiKey);

            isValid.ShouldBeTrue();
            message.ShouldBe("Connection successful!");

            mockRestClient.Verify(client => client.ExecuteGetAsync(It.Is<ApiRequest>(req =>
                req.Username == testUsername && req.Password == testApiKey)), Times.Once);
        }

        [Fact]
        public async Task VerifyCredentialsAsync_ShouldReturnFalseAndSpecificMessage_WhenCredentialsAreInvalid()
        {
            var mockRestClient = new Mock<IRestClientPort>();

            var fakeUnauthorizedResponse = new ApiResponse
            {
                IsSuccess = false,
                StatusCode = eBRestarter.Infrastructure.Api.ResponseCode.HttpRE401
            };

            mockRestClient
                .Setup(client => client.ExecuteGetAsync(It.IsAny<ApiRequest>()))
                .ReturnsAsync(fakeUnauthorizedResponse);

            var apiUseCase = new EVisitorApiAuthenticationProvider(mockRestClient.Object);

            var (isValid, message) = await apiUseCase.VerifyCredentialsAsync("WrongUser", "WrongKey");

            isValid.ShouldBeFalse();
            message.ShouldBe("Invalid username or API key.");
        }

        [Fact]
        public async Task VerifyCredentialsAsync_ShouldReturnFalseAndErrorMessage_OnGeneralApiError()
        {
            var mockRestClient = new Mock<IRestClientPort>();

            string systemErrorMessage = "Es konnte keine Verbindung zum Zielserver hergestellt werden (Timeout).";

            var fakeGeneralErrorResponse = new ApiResponse
            {
                IsSuccess = false,
                StatusCode = eBRestarter.Infrastructure.Api.ResponseCode.HttpRE500,
                ErrorMessage = systemErrorMessage
            };

            mockRestClient
                .Setup(client => client.ExecuteGetAsync(It.IsAny<ApiRequest>()))
                .ReturnsAsync(fakeGeneralErrorResponse);

            var apiUseCase = new EVisitorApiAuthenticationProvider(mockRestClient.Object);

            var (isValid, message) = await apiUseCase.VerifyCredentialsAsync("User", "Key");

            isValid.ShouldBeFalse();
            message.ShouldBe($"Error: {systemErrorMessage}");
        }
    }
}


