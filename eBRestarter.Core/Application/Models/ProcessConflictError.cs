using eBRestarter.Core.Application.Common.Results;

namespace eBRestarter.Core.Application.Models;

public sealed class ProcessConflictError : Error
{
    public ProcessConflictError(string message) : base(message)
    {
    }
}

