using FluentValidation;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Infrastructure.BehavioralComponents.Validators;

public sealed class DeleteBrowserContentValidator : AbstractValidator<DeleteBrowserContentRequest>
{

    private const string BrowserTypeRequiredErrorMessage = "A valid browser type must be selected.";
    private const string DeletionOptionRequiredErrorMessage = "At least one deletion option (Cache or Cookies) must be selected.";


    public DeleteBrowserContentValidator()
    {
        RuleFor(request => request.BrowserType)
            .IsInEnum()
            .WithMessage(BrowserTypeRequiredErrorMessage);

        RuleFor(request => request)
            .Must(request => request.DeleteCache || request.DeleteCookies)
            .WithMessage(DeletionOptionRequiredErrorMessage);
    }
}
