using eBRestarter.Core.Application.Interfaces.Authentication;
using eBRestarter.Infrastructure.Services.Authentication;
using Moq;
using Shouldly;
using System;
using System.DirectoryServices.AccountManagement;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Authentication
{
    public class PrincipalContextCredentialValidationServiceTests
    {
        [Fact]
        public void ValidateCredentials_ShouldUseMachineContext_WhenDomainIsLocalComputer()
        {
            var mockAdService = new Mock<IActiveDirectoryService>();

            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<ContextType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            var service = new PrincipalContextCredentialValidationService(mockAdService.Object);

            string localMachineName = Environment.MachineName;

            var result = service.ValidateCredentials("TestUser", localMachineName, "Password123");

            result.ShouldBeTrue();

            mockAdService.Verify(ad => ad.ValidateCredentials(
                ContextType.Machine,
                localMachineName,
                "TestUser",
                "Password123"), Times.Once);
        }

        [Fact]
        public void ValidateCredentials_ShouldUseDomainContext_WhenDomainIsDifferentFromMachineName()
        {
            var mockAdService = new Mock<IActiveDirectoryService>();
            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<ContextType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            var service = new PrincipalContextCredentialValidationService(mockAdService.Object);

            string someDomain = "FIRMEN_DOMAIN_ABC";

            var result = service.ValidateCredentials("TestUser", someDomain, "Password123");

            result.ShouldBeTrue();

            mockAdService.Verify(ad => ad.ValidateCredentials(
                ContextType.Domain,
                someDomain,
                "TestUser",
                "Password123"), Times.Once);
        }

        [Fact]
        public void ValidateCredentials_ShouldThrowInvalidOperationException_WhenServerIsDown()
        {
            var mockAdService = new Mock<IActiveDirectoryService>();

            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<ContextType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new PrincipalServerDownException());

            var service = new PrincipalContextCredentialValidationService(mockAdService.Object);

            var exception = Should.Throw<InvalidOperationException>(() =>
            {
                service.ValidateCredentials("TestUser", "DOMAIN", "Password");
            });

            exception.Message.ShouldBe("PrincipalServerDown");
        }

        [Fact]
        public void ValidateCredentials_ShouldReturnFalse_OnAnyOtherException()
        {
            var mockAdService = new Mock<IActiveDirectoryService>();

            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<ContextType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Irgendein unerwarteter Fehler im Windows-System"));

            var service = new PrincipalContextCredentialValidationService(mockAdService.Object);

            var result = service.ValidateCredentials("TestUser", "DOMAIN", "Password");

            result.ShouldBeFalse();
        }
    }
}
