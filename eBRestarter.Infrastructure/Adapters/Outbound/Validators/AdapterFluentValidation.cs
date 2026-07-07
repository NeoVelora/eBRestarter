using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Validators;
using FluentValidation;
using ValidationResult = eBRestarter.Core.Application.Common.Results.ValidationResult;

namespace eBRestarter.Infrastructure.Adapters.Outbound.Validators;

public sealed class AdapterFluentValidation<T>(IValidator<T> validator) : IInboundPortApplicationValidator<T>
{
    public ValidationResult Validate(T request)
    {
        var result = validator.Validate(request);
        if (result.IsValid)
        {
            return ValidationResult.Success();
        }

        var errors = result.Errors.Select(e => new ValidationError(e.ErrorMessage)).ToList();
        return new ValidationResult(false, errors);
    }
}
