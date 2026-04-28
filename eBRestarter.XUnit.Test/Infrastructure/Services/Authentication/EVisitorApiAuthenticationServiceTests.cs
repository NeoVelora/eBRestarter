using eBRestarter.Core.Application.Interfaces.RestClient;

using eBRestarter.Core.Application.Enums;

using eBRestarter.Core.Application.Models.Api;

using eBRestarter.Infrastructure.Services.Authentication;

using Moq;

using Shouldly;

using System.Threading.Tasks;

using Xunit;


namespace eBRestarter.XUnit.Test.Infrastructure.Services.Authentication

{

    public class EVisitorApiAuthenticationServiceTests

    {

        [Fact]

        public async Task VerifyCredentialsAsync_ShouldReturnTrue_WhenApiCallIsSuccessful()

        {


            var mockRestClient = new Mock<IRestClientService>();


            string testUsername = "TestUser";

            string testApiKey = "SecretKey123";


            var fakeSuccessResponse = new ApiResponse

            {

                IsSuccess = true,

                StatusCode = ResponseCode.HttpRE200

            };


            mockRestClient

                .Setup(client => client.ExecuteGetAsync(It.IsAny<ApiRequest>()))

                .ReturnsAsync(fakeSuccessResponse);


            var apiService = new EVisitorApiAuthenticationService(mockRestClient.Object);


            var (isValid, message) = await apiService.VerifyCredentialsAsync(testUsername, testApiKey);


            isValid.ShouldBeTrue();

            message.ShouldBe("Verbindung erfolgreich!");


            mockRestClient.Verify(client => client.ExecuteGetAsync(It.Is<ApiRequest>(req =>

                req.Username == testUsername &&

                req.Password == testApiKey

            )), Times.Once);

        }


        [Fact]

        public async Task VerifyCredentialsAsync_ShouldReturnFalseAndSpecificMessage_WhenCredentialsAreInvalid()

        {


            var mockRestClient = new Mock<IRestClientService>();


            var fakeUnauthorizedResponse = new ApiResponse

            {

                IsSuccess = false,

                StatusCode = ResponseCode.HttpRE401

            };


            mockRestClient

                .Setup(client => client.ExecuteGetAsync(It.IsAny<ApiRequest>()))

                .ReturnsAsync(fakeUnauthorizedResponse);


            var apiService = new EVisitorApiAuthenticationService(mockRestClient.Object);


            var (isValid, message) = await apiService.VerifyCredentialsAsync("WrongUser", "WrongKey");


            isValid.ShouldBeFalse();

            message.ShouldBe("Benutzername oder API-Schlüssel falsch.");

        }


        [Fact]

        public async Task VerifyCredentialsAsync_ShouldReturnFalseAndErrorMessage_OnGeneralApiError()

        {


            var mockRestClient = new Mock<IRestClientService>();


            string systemErrorMessage = "Es konnte keine Verbindung zum Zielserver hergestellt werden (Timeout).";


            var fakeGeneralErrorResponse = new ApiResponse

            {

                IsSuccess = false,

                StatusCode = ResponseCode.HttpRE500,

                ErrorMessage = systemErrorMessage

            };


            mockRestClient

                .Setup(client => client.ExecuteGetAsync(It.IsAny<ApiRequest>()))

                .ReturnsAsync(fakeGeneralErrorResponse);


            var apiService = new EVisitorApiAuthenticationService(mockRestClient.Object);


            var (isValid, message) = await apiService.VerifyCredentialsAsync("User", "Key");


            isValid.ShouldBeFalse();


            message.ShouldBe($"Fehler: {systemErrorMessage}");

        }

    }

}

