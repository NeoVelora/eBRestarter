using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

public interface IUseCaseDeleteBrowserContent
{
    Task<DeleteBrowserContentResult> ExecuteAsync(
        DeleteBrowserContentRequest request,
        IProgress<DeleteBrowserContentProgress> progress,
        CancellationToken cancellationToken);
}
