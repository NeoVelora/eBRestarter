using eBRestarter.Core.Application.Interfaces.Authentication;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.UseCases.ConfigureAutoLogon;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.XUnit.Test.Application.UseCases;

public class ConfigureAutoLogonServiceTests
{
    private readonly Mock<IWindowsAutoLogonService> _mockAutoLogonService;
    private readonly Mock<ICredentialValidationService> _mockValidationService;
    private readonly ConfigureAutoLogonService _sut;

    public ConfigureAutoLogonServiceTests()
    {
        _mockAutoLogonService = new Mock<IWindowsAutoLogonService>();
        _mockValidationService = new Mock<ICredentialValidationService>();
        _sut = new ConfigureAutoLogonService(_mockAutoLogonService.Object, _mockValidationService.Object);
    }

    [Fact]
    public void Execute_WhenDeactivateAction_ShouldDisableAndReturnDeactivatedStatus()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: true);

        var result = _sut.Execute(request);

        result.Success.ShouldBeTrue();
        result.Status.ShouldBe(AutoLogonResultStatus.Deactivated);
        _mockAutoLogonService.Verify(s => s.DisableAutoLogon(), Times.Once);
    }

    [Fact]
    public void Execute_WhenCredentialsInvalid_ShouldReturnValidationError()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: false, Username: "testUser", Domain: "testDomain", Password: "wrongPassword");
        _mockValidationService.Setup(v => v.ValidateCredentials("testUser", "testDomain", "wrongPassword")).Returns(false);

        var result = _sut.Execute(request);

        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AutoLogonResultStatus.ValidationError);
        _mockAutoLogonService.Verify(s => s.EnableAutoLogon(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Execute_WhenCredentialsValid_ShouldEnableAndReturnActivatedStatus()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: false, Username: "testUser", Domain: "testDomain", Password: "validPassword");
        _mockValidationService.Setup(v => v.ValidateCredentials("testUser", "testDomain", "validPassword")).Returns(true);

        var result = _sut.Execute(request);

        result.Success.ShouldBeTrue();
        result.Status.ShouldBe(AutoLogonResultStatus.Activated);
        _mockAutoLogonService.Verify(s => s.EnableAutoLogon("testUser", "testDomain", "validPassword"), Times.Once);
    }

    [Fact]
    public void Execute_WhenInvalidOperationExceptionThrown_ShouldReturnDomainError()
    {
        var request = new ConfigureAutoLogonRequest(IsDeactivateAction: false, Username: "testUser", Domain: "testDomain", Password: "validPassword");
        _mockValidationService.Setup(v => v.ValidateCredentials("testUser", "testDomain", "validPassword")).Returns(true);
        _mockAutoLogonService.Setup(s => s.EnableAutoLogon(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new InvalidOperationException());

        var result = _sut.Execute(request);

        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AutoLogonResultStatus.DomainError);
    }
}
