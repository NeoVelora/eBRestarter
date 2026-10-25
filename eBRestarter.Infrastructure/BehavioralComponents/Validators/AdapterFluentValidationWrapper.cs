using System;
using FluentValidation;

using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.Ports.Inbound.Validators;

namespace eBRestarter.Infrastructure.BehavioralComponents.Validators;

public sealed class AdapterFluentValidationWrapper<T>(
    IValidator<T> validator)
    : IInboundPortApplicationValidator<T>
{

    private readonly IValidator<T> _validator = validator ?? throw new ArgumentNullException(nameof(validator));


    public ValidationResult Validate(T request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fluentValidationResult = _validator.Validate(request);

        if (fluentValidationResult.IsValid)
        {
            return ValidationResult.Success();
        }

        var validationErrors = fluentValidationResult.Errors.ConvertAll(validationFailure => new ValidationError(validationFailure.ErrorMessage));

        return new ValidationResult(false, validationErrors);
    }
}

