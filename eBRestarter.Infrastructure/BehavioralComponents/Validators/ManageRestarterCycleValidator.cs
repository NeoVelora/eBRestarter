using FluentValidation;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Cycle;
using eBRestarter.Core.Domain.Policies;

namespace eBRestarter.Infrastructure.BehavioralComponents.Validators;

public sealed class ManageRestarterCycleValidator : AbstractValidator<ManageRestarterCycleRequest>
{

    private const string BrowserTypeRequiredErrorMessage = "A valid browser type must be selected.";
    private const string PauseTimeCannotBeNegativeErrorMessage = "Pause time cannot be negative.";
    private const string RuntimeGreaterThanZeroErrorMessage = "Runtime must be greater than 0 seconds.";
    private const string UsernameRequiredErrorMessage = "A username is required to start the restarter.";


    public ManageRestarterCycleValidator()
    {
        RuleFor(request => request.BrowserType)
            .IsInEnum()
            .WithMessage(BrowserTypeRequiredErrorMessage);

        RuleFor(request => request.RuntimeSeconds)
            .GreaterThan(0)
            .WithMessage(RuntimeGreaterThanZeroErrorMessage);

        RuleFor(request => request.PauseSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage(PauseTimeCannotBeNegativeErrorMessage);

        RuleFor(request => request.Username)
            .NotEmpty()
            .WithMessage(UsernameRequiredErrorMessage);

        RuleFor(request => request.Username)
            .Must(EVisitorUsernamePolicy.IsValid)
            .WithMessage(EVisitorUsernamePolicy.RuleDescription);
    }
}
