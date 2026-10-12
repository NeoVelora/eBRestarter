using System;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Validators;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Logging;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Core.Application.UseCases;

public sealed class ConfigureAutoLogonUseCase(
    IOutboundPortOsAutoLogonRepository autoLogonPort,
    IOutboundPortCredentialValidationProvider credentialValidationPort,
    IOutboundPortApplicationLogger<ConfigureAutoLogonUseCase> logger,
    IOutboundPortSystemInfoProvider systemInfoPort,
    IInboundPortApplicationValidator<ConfigureAutoLogonRequest> validator) : IUseCaseConfigureAutoLogon
{
    private const string ErrorMessageAdminRequiredActivation = "Administrator rights required to disable passwordless mode.";
    private const string ErrorMessageAdminRequiredDeactivation = "Administrator rights required to restore passwordless mode.";
    private const string ErrorMessageCredentialsMissing = "Credentials missing.";
    private const string ErrorMessageDomainError = "Domain error.";
    private const string ErrorMessageInvalidCredentials = "Invalid credentials.";
    private const string ErrorMessageValidationFailed = "Validation failed.";

    private readonly IOutboundPortOsAutoLogonRepository _autoLogonPort = autoLogonPort ?? throw new ArgumentNullException(nameof(autoLogonPort));
    private readonly IOutboundPortCredentialValidationProvider _credentialValidationPort = credentialValidationPort ?? throw new ArgumentNullException(nameof(credentialValidationPort));
    private readonly IOutboundPortApplicationLogger<ConfigureAutoLogonUseCase> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOutboundPortSystemInfoProvider _systemInfoPort = systemInfoPort ?? throw new ArgumentNullException(nameof(systemInfoPort));
    private readonly IInboundPortApplicationValidator<ConfigureAutoLogonRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    public ConfigureAutoLogonResult Execute(ConfigureAutoLogonRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return new AutoLogonValidationError(ErrorMessageValidationFailed);
        }

        try
        {
            return request.IsDeactivateAction
                ? HandleDeactivation(request)
                : HandleActivation(request);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(
                LogEventIds.Security.AutoLogonConfigurationFailed,
                exception,
                "AutoLogon configuration failed while reaching the domain controller.");

            return new AutoLogonDomainError(ErrorMessageDomainError);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                LogEventIds.Security.AutoLogonConfigurationFailed,
                ex,
                "AutoLogon configuration failed with an unexpected error.");

            return new AutoLogonUnexpectedError(ex.Message);
        }
    }

    private ConfigureAutoLogonResult HandleDeactivation(ConfigureAutoLogonRequest request)
    {
        if (request.RestorePasswordlessMode)
        {
            if (!_systemInfoPort.IsUserAdministrator())
                return new AutoLogonAdminRequired(ErrorMessageAdminRequiredDeactivation);

            _autoLogonPort.SetPasswordlessAuth(true);
        }

        _autoLogonPort.DisableAutoLogon();
        return new AutoLogonDeactivated();
    }

    private ConfigureAutoLogonResult HandleActivation(ConfigureAutoLogonRequest request)
    {
        if (request.DisablePasswordlessMode && !_systemInfoPort.IsUserAdministrator())
            return new AutoLogonAdminRequired(ErrorMessageAdminRequiredActivation);

        if (!request.DisablePasswordlessMode && _autoLogonPort.IsPasswordlessAuthEnabled())
            return new AutoLogonWindowsHelloBlockActive();

        if (string.IsNullOrWhiteSpace(request.Username) || request.Password is null)
            return new AutoLogonValidationError(ErrorMessageCredentialsMissing);

        string domain = request.Domain ?? string.Empty;

        if (!_credentialValidationPort.ValidateCredentials(request.Username, domain, request.Password))
            return new AutoLogonValidationError(ErrorMessageInvalidCredentials);

        return ApplyAutoLogonActivation(request, domain);
    }

    private ConfigureAutoLogonResult ApplyAutoLogonActivation(ConfigureAutoLogonRequest request, string domain)
    {
        bool passwordlessWasDisabledByUs = false;

        try
        {
            if (request.DisablePasswordlessMode)
            {
                _autoLogonPort.SetPasswordlessAuth(false);
                passwordlessWasDisabledByUs = true;
            }

            if (_autoLogonPort.IsPasswordlessAuthEnabled())
            {
                RestorePasswordlessMode(passwordlessWasDisabledByUs);

                return new AutoLogonWindowsHelloBlockActive();
            }

            _autoLogonPort.EnableAutoLogon(request.Username!, domain, request.Password!);

            return new AutoLogonActivated();
        }
        catch
        {
            RestorePasswordlessMode(passwordlessWasDisabledByUs);
            throw;
        }
    }

    private void RestorePasswordlessMode(bool passwordlessWasDisabledByUs)
    {
        if (!passwordlessWasDisabledByUs)
            return;

        try
        {
            _autoLogonPort.SetPasswordlessAuth(true);
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(
                LogEventIds.Security.PasswordlessRollbackFailed,
                rollbackException,
                "AutoLogon rollback failed; passwordless sign-in remains disabled on this machine.");
        }
    }
}
