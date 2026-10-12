using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Validators;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Core.Application.UseCases;

public sealed class DeleteBrowserContentUseCase(
    IOutboundPortBrowserFactory browserFactory,
    IOutboundPortFileDeletion fileDeletionService,
    IInboundPortLocalizationProvider localizationService,
    IOutboundPortOsProcessControl processService,
    TimeProvider timeProvider,
    IInboundPortApplicationValidator<DeleteBrowserContentRequest> validator)
    : IUseCaseDeleteBrowserContent
{
    private const int CloseDelayMilliseconds = 1000;
    private const string DefaultBrowserRunningMessage = "Browser is running";
    private const string DefaultDeleteProgressMessage = "Delete...";
    private const string DefaultNoPathsMessage = "No paths to clean";
    private const string LocalizationKeyAnalyzing = "Cleanup_Analyzing";
    private const string LocalizationKeyBrowserRunning = "Cleanup_BrowserRunning";
    private const string LocalizationKeyClosingBrowser = "Cleanup_ClosingBrowser";
    private const string LocalizationKeyFinished = "Cleanup_Finished";
    private const string LocalizationKeyNoPaths = "Cleanup_NoPaths";
    private const string LocalizationKeyRunning = "Cleanup_Running";

    private readonly IOutboundPortBrowserFactory _browserFactory = browserFactory ?? throw new ArgumentNullException(nameof(browserFactory));
    private readonly IOutboundPortFileDeletion _fileDeletionService = fileDeletionService ?? throw new ArgumentNullException(nameof(fileDeletionService));
    private readonly IInboundPortLocalizationProvider _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    private readonly IOutboundPortOsProcessControl _processService = processService ?? throw new ArgumentNullException(nameof(processService));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly IInboundPortApplicationValidator<DeleteBrowserContentRequest> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    public Task<DeleteBrowserContentResult> ExecuteAsync(
        DeleteBrowserContentRequest request,
        IProgress<DeleteBrowserContentProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(progress);

        return ExecuteCoreAsync(request, progress, cancellationToken);
    }

    private async Task<DeleteBrowserContentResult> ExecuteCoreAsync(
        DeleteBrowserContentRequest request,
        IProgress<DeleteBrowserContentProgress> progress,
        CancellationToken cancellationToken)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
            return new DeleteBrowserContentFailure(validationResult.Errors[0].ErrorMessage);

        try
        {
            var browser = _browserFactory.Create(request.BrowserType);
            var processName = browser.ProcessName;

            if (request.ForceCloseProcess)
            {
                progress.Report(new DeleteBrowserContentProgress(_localizationService.RetrieveString(LocalizationKeyClosingBrowser), 0, 0));
                _processService.CloseApplication(processName);

                await Task.Delay(TimeSpan.FromMilliseconds(CloseDelayMilliseconds), _timeProvider, cancellationToken).ConfigureAwait(false);
            }

            if (_processService.IsProcessAlive(processName))
                return new DeleteBrowserContentProcessConflict(_localizationService.RetrieveString(LocalizationKeyBrowserRunning) ?? DefaultBrowserRunningMessage);

            var browserPaths = browser.ResolvePaths();

            List<string> directoriesToDelete = [];

            if (request.DeleteCache && browserPaths.CacheDirs is { Count: > 0 })
            {
                directoriesToDelete.AddRange(browserPaths.CacheDirs);
            }

            if (request.DeleteCookies && browserPaths.CookiesDirs is { Count: > 0 })
            {
                directoriesToDelete.AddRange(browserPaths.CookiesDirs);
            }

            if (directoriesToDelete.Count == 0)
                return new DeleteBrowserContentFailure(_localizationService.RetrieveString(LocalizationKeyNoPaths) ?? DefaultNoPathsMessage);

            progress.Report(new DeleteBrowserContentProgress(_localizationService.RetrieveString(LocalizationKeyAnalyzing), 0, 0));

            int totalSteps = directoriesToDelete.Count;
            int completedSteps = 0;

            var statusProgress = new Progress<string>(status =>
                progress.Report(new DeleteBrowserContentProgress(status, completedSteps, totalSteps)));

            var directoryProgress = new Progress<int>(completed =>
            {
                completedSteps = completed;

                progress.Report(new DeleteBrowserContentProgress(
                    _localizationService.RetrieveString(LocalizationKeyRunning) ?? DefaultDeleteProgressMessage,
                    completedSteps,
                    totalSteps));
            });

            await _fileDeletionService.DeleteFilesAsync(directoriesToDelete, statusProgress, directoryProgress, cancellationToken).ConfigureAwait(false);

            progress.Report(new DeleteBrowserContentProgress(_localizationService.RetrieveString(LocalizationKeyFinished), totalSteps, totalSteps));

            return new DeleteBrowserContentSuccess();
        }
        catch (Exception exception)
        {
            return new DeleteBrowserContentFailure(exception.Message);
        }
    }
}
