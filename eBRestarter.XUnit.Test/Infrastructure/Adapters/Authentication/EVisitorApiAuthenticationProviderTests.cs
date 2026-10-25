using NSubstitute;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.API.Authentication;
using eBRestarter.Infrastructure.BehavioralComponents.Clients.Http;
using eBRestarter.Infrastructure.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.ObjectArchetypes.Models;

namespace eBRestarter.XUnit.Test.Infrastructure.Adapters.Authentication
{
    public class EVisitorApiAuthenticationProviderTests
    {
        [Fact]
        public async Task VerifyCredentialsAsync_ShouldReturnTrue_WhenApiCallIsSuccessful()
        {
            var mockRestClient = Substitute.For<IRestClient>();

            string testUsername = "TestUser";
            string testApiKey = "SecretKey123";

            var fakeSuccessResponse = new ApiResponse
            {
                IsSuccess = true,
                StatusCode = ResponseCode.HttpRE200
            };

            mockRestClient
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(fakeSuccessResponse);

            var apiUseCase = new AdapterEVisitorApiAuthenticationProvider(mockRestClient);

            var (isValid, message) = await apiUseCase.VerifyCredentialsAsync(testUsername, testApiKey);

            isValid.ShouldBeTrue();
            message.ShouldBe("Connection successful!");

            await mockRestClient.Received(1).ExecuteGetAsync(Arg.Is<ApiRequest>(req =>
                req.Username == testUsername && req.Password == testApiKey));
        }

        [Fact]
        public async Task VerifyCredentialsAsync_ShouldReturnFalseAndSpecificMessage_WhenCredentialsAreInvalid()
        {
            var mockRestClient = Substitute.For<IRestClient>();

            var fakeUnauthorizedResponse = new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResponseCode.HttpRE401
            };

            mockRestClient
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(fakeUnauthorizedResponse);

            var apiUseCase = new AdapterEVisitorApiAuthenticationProvider(mockRestClient);

            var (isValid, message) = await apiUseCase.VerifyCredentialsAsync("WrongUser", "WrongKey");

            isValid.ShouldBeFalse();
            message.ShouldBe("Invalid username or API key.");
        }

        [Fact]
        public async Task VerifyCredentialsAsync_ShouldReturnFalseAndErrorMessage_OnGeneralApiError()
        {
            var mockRestClient = Substitute.For<IRestClient>();

            string systemErrorMessage = "Connection timed out.";

            var fakeGeneralErrorResponse = new ApiResponse
            {
                IsSuccess = false,
                StatusCode = ResponseCode.HttpRE500,
                ErrorMessage = systemErrorMessage
            };

            mockRestClient
                .ExecuteGetAsync(Arg.Any<ApiRequest>())
                .Returns(fakeGeneralErrorResponse);

            var apiUseCase = new AdapterEVisitorApiAuthenticationProvider(mockRestClient);

            var (isValid, message) = await apiUseCase.VerifyCredentialsAsync("User", "Key");

            isValid.ShouldBeFalse();
            message.ShouldBe($"Error: {systemErrorMessage}");
        }
    }
}
