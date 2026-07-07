using eBRestarter.Core.Application.Enums;
using eBRestarter.Infrastructure.Adapters.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Repositories.Authentication;
using Moq;
using Shouldly;
using System;
using System.DirectoryServices.AccountManagement;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Authentication
{
    public class WindowsCredentialValidationProviderTests
    {
        [Fact]
        public void ValidateCredentials_ShouldUseMachineContext_WhenDomainIsLocalComputer()
        {
            var mockAdService = new Mock<IOutboundPortActiveDirectoryProvider>();

            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<DirectoryContextScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            var useCase = new AdapterWindowsCredentialValidationProvider(mockAdService.Object);

            string localMachineName = Environment.MachineName;

            var result = useCase.ValidateCredentials("TestUser", localMachineName, "Password123");

            result.ShouldBeTrue();

            mockAdService.Verify(ad => ad.ValidateCredentials(
                DirectoryContextScope.Machine,
                localMachineName,
                "TestUser",
                "Password123"), Times.Once);
        }

        [Fact]
        public void ValidateCredentials_ShouldUseDomainContext_WhenDomainIsDifferentFromMachineName()
        {
            var mockAdService = new Mock<IOutboundPortActiveDirectoryProvider>();
            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<DirectoryContextScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            var useCase = new AdapterWindowsCredentialValidationProvider(mockAdService.Object);

            string someDomain = "FIRMEN_DOMAIN_ABC";

            var result = useCase.ValidateCredentials("TestUser", someDomain, "Password123");

            result.ShouldBeTrue();

            mockAdService.Verify(ad => ad.ValidateCredentials(
                DirectoryContextScope.Domain,
                someDomain,
                "TestUser",
                "Password123"), Times.Once);
        }

        [Fact]
        public void ValidateCredentials_ShouldThrowInvalidOperationException_WhenServerIsDown()
        {
            var mockAdService = new Mock<IOutboundPortActiveDirectoryProvider>();

            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<DirectoryContextScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new PrincipalServerDownException());

            var useCase = new AdapterWindowsCredentialValidationProvider(mockAdService.Object);

            var exception = Should.Throw<InvalidOperationException>(() =>
            {
                useCase.ValidateCredentials("TestUser", "DOMAIN", "Password");
            });

            exception.Message.ShouldBe("PrincipalServerDown");
        }

        [Fact]
        public void ValidateCredentials_ShouldReturnFalse_OnAnyOtherException()
        {
            var mockAdService = new Mock<IOutboundPortActiveDirectoryProvider>();

            mockAdService
                .Setup(ad => ad.ValidateCredentials(It.IsAny<DirectoryContextScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Irgendein unerwarteter Fehler im Windows-System"));

            var useCase = new AdapterWindowsCredentialValidationProvider(mockAdService.Object);

            var result = useCase.ValidateCredentials("TestUser", "DOMAIN", "Password");

            result.ShouldBeFalse();
        }
    }
}


