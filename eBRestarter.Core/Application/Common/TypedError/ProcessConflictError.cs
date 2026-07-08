using eBRestarter.Core.Application.Common.Results;

namespace eBRestarter.Core.Application.Common.TypedError;

public sealed class ProcessConflictError : Error
{
    public ProcessConflictError(string message) : base(message)
    {
    }
}

