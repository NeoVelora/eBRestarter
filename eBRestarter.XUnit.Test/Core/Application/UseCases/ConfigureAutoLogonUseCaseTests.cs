using System;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.AutoLogon;
using eBRestarter.Core.Application.Ports.Inbound.Validators;
using eBRestarter.Core.Application.Ports.Outbound.Logging;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.UseCases.AutoLogon;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace eBRestarter.Tests.Core.Application.UseCases;

public class ConfigureAutoLogonUseCaseTests
{
    private readonly IOutboundPortOsAutoLogonRepository _mockAutoLogonPort;
    private readonly IOutboundPortCredentialValidationProvider _mockCredentialValidationPort;
    private readonly IOutboundPortApplicationLogger<ConfigureAutoLogonUseCase> _mockLogger;
    private readonly IOutboundPortSystemInfoProvider _mockSystemInfoPort;
    private readonly IInboundPortApplicationValidator<ConfigureAutoLogonRequest> _mockValidator;
    private readonly ConfigureAutoLogonUseCase _sut;

    public ConfigureAutoLogonUseCaseTests()
    {
        _mockAutoLogonPort = Substitute.For<IOutboundPortOsAutoLogonRepository>();
        _mockCredentialValidationPort = Substitute.For<IOutboundPortCredentialValidationProvider>();
        _mockLogger = Substitute.For<IOutboundPortApplicationLogger<ConfigureAutoLogonUseCase>>();
        _mockSystemInfoPort = Substitute.For<IOutboundPortSystemInfoProvider>();
        _mockValidator = Substitute.For<IInboundPortApplicationValidator<ConfigureAutoLogonRequest>>();

        _mockValidator.Validate(Arg.Any<ConfigureAutoLogonRequest>()).Returns(ValidationResult.Success());

        _sut = new ConfigureAutoLogonUseCase(
            _mockAutoLogonPort,
            _mockCredentialValidationPort,
            _mockLogger,
            _mockSystemInfoPort,
            _mockValidator);
    }

    [Fact]
    public void Execute_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        Should.Throw<ArgumentNullException>(() => _sut.Execute(null!));
    }

    [Fact]
    public void Execute_ShouldReturnValidationError_WhenValidatorFails()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: true);
        _mockValidator.Validate(request).Returns(ValidationResult.Fail("Field invalid"));

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonValidationError).ShouldBeTrue();
    }

    [Fact]
    public void Execute_ShouldReturnDeactivated_WhenDeactivateActionIsTrue()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: true);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonDeactivated).ShouldBeTrue();
        _mockAutoLogonPort.Received(1).DisableAutoLogon();
    }

    [Fact]
    public void Execute_ShouldReturnAdminRequired_WhenDeactivateRestorePasswordlessAndUserIsNotAdmin()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: true, RestorePasswordlessMode: true);
        _mockSystemInfoPort.IsUserAdministrator().Returns(false);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonAdminRequired).ShouldBeTrue();
        _mockAutoLogonPort.DidNotReceive().DisableAutoLogon();
    }

    [Fact]
    public void Execute_ShouldRestorePasswordlessAndDeactivate_WhenUserIsAdmin()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: true, RestorePasswordlessMode: true);
        _mockSystemInfoPort.IsUserAdministrator().Returns(true);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonDeactivated).ShouldBeTrue();
        _mockAutoLogonPort.Received(1).SetPasswordlessAuth(true);
        _mockAutoLogonPort.Received(1).DisableAutoLogon();
    }

    [Fact]
    public void Execute_ShouldReturnWindowsHelloBlockActive_WhenWindowsHelloIsActiveAndNotDisabled()
    {
        var request = new ConfigureAutoLogonRequest(
            IsDeactivateAction: false,
            DisablePasswordlessMode: false,
            Username: "user",
            Password: "pass");
        _mockAutoLogonPort.IsPasswordlessAuthEnabled().Returns(true);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonWindowsHelloBlockActive).ShouldBeTrue();
        _mockCredentialValidationPort.DidNotReceive().ValidateCredentials(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void Execute_ShouldReturnAdminRequired_WhenActivationDisablesPasswordlessAndUserIsNotAdmin()
    {
        var request = new ConfigureAutoLogonRequest(
            IsDeactivateAction: false,
            DisablePasswordlessMode: true,
            Username: "user",
            Password: "pass");
        _mockSystemInfoPort.IsUserAdministrator().Returns(false);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonAdminRequired).ShouldBeTrue();
    }

    [Fact]
    public void Execute_ShouldReturnValidationError_WhenCredentialsAreMissing()
    {
        var request = new ConfigureAutoLogonRequest(
            IsDeactivateAction: false,
            Username: "",
            Password: null);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonValidationError).ShouldBeTrue();
    }

    [Fact]
    public void Execute_ShouldReturnValidationError_WhenCredentialsAreInvalid()
    {
        var request = new ConfigureAutoLogonRequest(
            IsDeactivateAction: false,
            Username: "User",
            Domain: "DOM",
            Password: "WrongPassword");
        _mockAutoLogonPort.IsPasswordlessAuthEnabled().Returns(false);
        _mockCredentialValidationPort.ValidateCredentials("User", "DOM", "WrongPassword").Returns(false);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonValidationError).ShouldBeTrue();
        _mockAutoLogonPort.DidNotReceive().EnableAutoLogon(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void Execute_ShouldReturnActivated_WhenCredentialsAreValid()
    {
        var request = new ConfigureAutoLogonRequest(
            IsDeactivateAction: false,
            Username: "Admin",
            Domain: "Workgroup",
            Password: "TopSecret");
        _mockAutoLogonPort.IsPasswordlessAuthEnabled().Returns(false);
        _mockCredentialValidationPort.ValidateCredentials("Admin", "Workgroup", "TopSecret").Returns(true);

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonActivated).ShouldBeTrue();
        _mockAutoLogonPort.Received(1).EnableAutoLogon("Admin", "Workgroup", "TopSecret");
    }

    [Fact]
    public void Execute_ShouldReturnDomainError_OnInvalidOperationException()
    {
        var request = new ConfigureAutoLogonRequest(
            IsDeactivateAction: false,
            Username: "u",
            Domain: "d",
            Password: "p");
        _mockAutoLogonPort.IsPasswordlessAuthEnabled().Returns(false);
        _mockCredentialValidationPort.ValidateCredentials("u", "d", "p").Returns(true);
        _mockAutoLogonPort
            .When(x => x.EnableAutoLogon(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException());

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonDomainError).ShouldBeTrue();
    }

    [Fact]
    public void Execute_ShouldReturnUnexpectedError_OnGeneralException()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: true);
        _mockAutoLogonPort
            .When(x => x.DisableAutoLogon())
            .Do(_ => throw new InvalidCastException("Critical system error"));

        var outcome = _sut.Execute(request);

        (outcome is AutoLogonUnexpectedError unexpected &&
         unexpected.Message == "An unexpected error occurred during AutoLogon configuration.").ShouldBeTrue();

        _mockLogger.Received(1).LogError(
            LogEventIds.Security.AutoLogonConfigurationFailed,
            Arg.Any<InvalidCastException>(),
            Arg.Any<string>());
    }
}
