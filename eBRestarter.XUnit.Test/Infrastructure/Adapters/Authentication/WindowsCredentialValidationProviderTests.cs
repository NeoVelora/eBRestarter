using NSubstitute;
using Shouldly;
using System;
using System.DirectoryServices.AccountManagement;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS.Authentication;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Logging;

namespace eBRestarter.XUnit.Test.Infrastructure.Services.Authentication
{
    public class WindowsCredentialValidationProviderTests
    {
        [Fact]
        public void ValidateCredentials_ShouldUseMachineContext_WhenDomainIsLocalComputer()
        {
            var mockAdService = Substitute.For<IOutboundPortActiveDirectoryProvider>();

            mockAdService
                .ValidateCredentials(Arg.Any<DirectoryContextScope>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(true);

            var useCase = new AdapterWindowsCredentialValidationProvider(
                mockAdService,
                new FakeLogger<AdapterWindowsCredentialValidationProvider>());

            string localMachineName = Environment.MachineName;

            var result = useCase.ValidateCredentials("TestUser", localMachineName, "Password123");

            result.ShouldBeTrue();

            mockAdService.Received(1).ValidateCredentials(
                DirectoryContextScope.Machine,
                localMachineName,
                "TestUser",
                "Password123");
        }

        [Fact]
        public void ValidateCredentials_ShouldUseDomainContext_WhenDomainIsDifferentFromMachineName()
        {
            var mockAdService = Substitute.For<IOutboundPortActiveDirectoryProvider>();
            mockAdService
                .ValidateCredentials(Arg.Any<DirectoryContextScope>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(true);

            var useCase = new AdapterWindowsCredentialValidationProvider(
                mockAdService,
                new FakeLogger<AdapterWindowsCredentialValidationProvider>());

            string someDomain = "FIRMEN_DOMAIN_ABC";

            var result = useCase.ValidateCredentials("TestUser", someDomain, "Password123");

            result.ShouldBeTrue();

            mockAdService.Received(1).ValidateCredentials(
                DirectoryContextScope.Domain,
                someDomain,
                "TestUser",
                "Password123");
        }

        [Fact]
        public void ValidateCredentials_ShouldThrowInvalidOperationException_WhenServerIsDown()
        {
            var mockAdService = Substitute.For<IOutboundPortActiveDirectoryProvider>();

            mockAdService
                .ValidateCredentials(Arg.Any<DirectoryContextScope>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Throws(new PrincipalServerDownException());

            var useCase = new AdapterWindowsCredentialValidationProvider(
                mockAdService,
                new FakeLogger<AdapterWindowsCredentialValidationProvider>());

            var exception = Should.Throw<InvalidOperationException>(() =>
            {
                useCase.ValidateCredentials("TestUser", "DOMAIN", "Password");
            });

            exception.Message.ShouldBe("PrincipalServerDown");
            exception.InnerException.ShouldBeOfType<PrincipalServerDownException>();
        }

        [Fact]
        public void ValidateCredentials_ShouldReturnFalse_OnAnyOtherException()
        {
            var mockAdService = Substitute.For<IOutboundPortActiveDirectoryProvider>();

            mockAdService
                .ValidateCredentials(Arg.Any<DirectoryContextScope>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Throws(new Exception("Unexpected error in Windows subsystem"));

            var useCase = new AdapterWindowsCredentialValidationProvider(
                mockAdService,
                new FakeLogger<AdapterWindowsCredentialValidationProvider>());

            var result = useCase.ValidateCredentials("TestUser", "DOMAIN", "Password");

            result.ShouldBeFalse();
        }
    }
}
