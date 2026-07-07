using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Common.Results;
namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseDeleteBrowserContent
{
    Task<Result> ExecuteAsync(
        DeleteBrowserContentRequest request,
        IProgress<DeleteBrowserContentProgress> progress,
        CancellationToken cancel);
}

