using eBRestarter.Core.Application.Common.Results;

namespace eBRestarter.Core.Application.Ports.Inbound.Validators;

public interface IInboundPortApplicationValidator<in T>
{
    ValidationResult Validate(T request);
}
