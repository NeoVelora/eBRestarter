using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Service.WindowsOS;

public sealed class AdapterWindowsFileDeletionService : IOutboundPortFileDeletion
{
    private const string AccessDeniedDirectoryLogMessage = "Access denied to directory: {Dir}";
    private const string AllFilesSearchPattern = "*";
    private const int DefaultReportInterval = 10;
    private const string DeletingFileStatusFormat = "Deleting: {0}";
    private const string DirectoryCouldNotBeDeletedLogMessage = "Directory could not be deleted (it might not be empty): {Dir}";
    private const string ErrorDeletingFileLogMessage = "Error while deleting individual file: {FilePath}";
    private const string ErrorDeletingFilePayloadLogMessage = "Error while deleting file payload: {File}";
    private const string ErrorGatheringFilesLogMessage = "Error while gathering files from directory: {Dir}";
    private const string FinalizingCleanupStatusMessage = "Finalizing cleanup operations...";
    private const int InitialCounterValue = 0;
    private const string ProtectedFirefoxExtensionFilter = "moz-extension";

    private readonly ILogger<AdapterWindowsFileDeletionService> _logger;


    public AdapterWindowsFileDeletionService(ILogger<AdapterWindowsFileDeletionService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }


    public Task<int> CountFilesAsync(List<string> directories)
    {
        ArgumentNullException.ThrowIfNull(directories);

        return Task.Run(() =>
        {
            return directories.Where(Directory.Exists).Sum(dir =>
            {
                try
                {
                    return Directory.EnumerateFiles(dir, AllFilesSearchPattern, SearchOption.AllDirectories).Count();
                }
                catch (Exception exception)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(exception, AccessDeniedDirectoryLogMessage, dir);
                    }

                    return InitialCounterValue;
                }
            });
        });
    }

    public Task DeleteFilesAsync(List<string> directories, IProgress<string> statusReporter, IProgress<int> valueReporter, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(directories);
        ArgumentNullException.ThrowIfNull(statusReporter);
        ArgumentNullException.ThrowIfNull(valueReporter);

        return Task.Run(() =>
        {
            var context = new DeletionContext
            {
                DeletedCount = InitialCounterValue,
                UpdateCounter = InitialCounterValue,
                ReportInterval = DefaultReportInterval,
                StatusReporter = statusReporter,
                ValueReporter = valueReporter,
                Token = token
            };

            foreach (var dir in directories)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                ProcessDirectory(dir, context, _logger);
            }

            statusReporter.Report(FinalizingCleanupStatusMessage);
            valueReporter.Report(context.DeletedCount);

        }, token);
    }

    public void DeleteSingleFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, ErrorDeletingFileLogMessage, filePath);
            }
        }
    }

    private static void ProcessDirectory(string dir, DeletionContext context, ILogger logger)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(dir, AllFilesSearchPattern, SearchOption.AllDirectories);
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(exception, ErrorGatheringFilesLogMessage, dir);
            }

            return;
        }

        foreach (var file in files)
        {
            if (context.Token.IsCancellationRequested)
            {
                return;
            }

            ProcessFile(file, context, logger);
        }

        if (context.Token.IsCancellationRequested)
        {
            return;
        }

        try
        {
            Directory.Delete(dir, recursive: false);
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(exception, DirectoryCouldNotBeDeletedLogMessage, dir);
            }
        }
    }

    private static void ProcessFile(string file, DeletionContext context, ILogger logger)
    {
        try
        {
            if (file.Contains(ProtectedFirefoxExtensionFilter, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            File.Delete(file);
            context.DeletedCount++;
            context.UpdateCounter++;

            if (context.UpdateCounter >= context.ReportInterval)
            {
                string statusMessage = string.Format(DeletingFileStatusFormat, Path.GetFileName(file));
                context.StatusReporter.Report(statusMessage);
                context.ValueReporter.Report(context.DeletedCount);
                context.UpdateCounter = InitialCounterValue;
            }
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(exception, ErrorDeletingFilePayloadLogMessage, file);
            }
        }
    }


    private sealed class DeletionContext
    {
        public int DeletedCount { get; set; }
        public int UpdateCounter { get; set; }
        public int ReportInterval { get; set; }
        public IProgress<string> StatusReporter { get; set; } = null!;
        public IProgress<int> ValueReporter { get; set; } = null!;
        public CancellationToken Token { get; set; }
    }
}