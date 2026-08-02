using FluentValidation;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Infrastructure.BehavioralComponents.Validators;

public sealed class ConfigureAutoLogonValidator : AbstractValidator<ConfigureAutoLogonRequest>
{

    private const string PasswordRequiredErrorMessage = "Password is required when activating AutoLogon.";
    private const string UsernameRequiredErrorMessage = "Username is required when activating AutoLogon.";


    public ConfigureAutoLogonValidator()
    {
        RuleFor(request => request.Username)
            .NotEmpty()
            .When(request => !request.IsDeactivateAction)
            .WithMessage(UsernameRequiredErrorMessage);

        RuleFor(request => request.Password)
            .NotEmpty()
            .When(request => !request.IsDeactivateAction)
            .WithMessage(PasswordRequiredErrorMessage);
    }
}
