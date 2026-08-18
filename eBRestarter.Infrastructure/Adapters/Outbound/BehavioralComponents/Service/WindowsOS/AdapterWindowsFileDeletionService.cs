using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces;
using eBRestarter.Infrastructure.BehavioralComponents.Logging;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Service.WindowsOS;

public sealed class AdapterWindowsFileDeletionService : IOutboundPortFileDeletion
{
    private const string AllFilesSearchPattern = "*";
    private const int DefaultReportInterval = 10;
    private const string DeletingFileStatusFormat = "Deleting: {0}";
    private const string FinalizingCleanupStatusMessage = "Finalizing cleanup operations...";
    private const int InitialCounterValue = 0;
    private const string ProtectedFirefoxExtensionFilter = "moz-extension";


    private readonly ILogger<AdapterWindowsFileDeletionService> _logger;


    public AdapterWindowsFileDeletionService(ILogger<AdapterWindowsFileDeletionService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }


    public Task DeleteFilesAsync(List<string> directories, IProgress<string> statusReporter, IProgress<int> completedDirectoriesReporter, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(directories);
        ArgumentNullException.ThrowIfNull(statusReporter);
        ArgumentNullException.ThrowIfNull(completedDirectoriesReporter);

        return Task.Run(() =>
        {
            var context = new DeletionContext
            {
                DeletedCount = InitialCounterValue,
                UpdateCounter = InitialCounterValue,
                ReportInterval = DefaultReportInterval,
                StatusReporter = statusReporter,
                Token = token
            };

            int completedDirectories = InitialCounterValue;

            foreach (var dir in directories)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                ProcessDirectory(dir, context, _logger);

                completedDirectories++;
                completedDirectoriesReporter.Report(completedDirectories);
            }

            statusReporter.Report(FinalizingCleanupStatusMessage);
            completedDirectoriesReporter.Report(directories.Count);

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
                _logger.LogSingleFileDeletionFailed(exception, filePath);
            }
        }
    }

    private static void ProcessDirectory(string dir, DeletionContext context, ILogger logger)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        if (IsReparsePoint(dir))
        {
            logger.LogReparsePointSkipped(dir);

            return;
        }

        string cleanupRoot;
        IEnumerable<string> files;

        try
        {
            cleanupRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dir)) + Path.DirectorySeparatorChar;

            files = Directory.EnumerateFiles(dir, AllFilesSearchPattern, SearchOption.AllDirectories);
        }
        catch (Exception exception)
        {
            logger.LogDirectoryEnumerationFailed(exception, dir);

            return;
        }

        foreach (var file in files)
        {
            if (context.Token.IsCancellationRequested)
            {
                return;
            }

            if (!IsInsideCleanupRoot(file, cleanupRoot, logger))
            {
                continue;
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
            logger.LogDirectoryNotRemoved(exception, dir);
        }
    }

    private static bool IsInsideCleanupRoot(string file, string cleanupRoot, ILogger logger)
    {
        try
        {
            var resolvedPath = Path.GetFullPath(file);

            if (resolvedPath.StartsWith(cleanupRoot, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            logger.LogPathOutsideCleanupRoot(file, cleanupRoot);
        }
        catch (Exception exception)
        {
            logger.LogPathResolutionFailed(exception, file, cleanupRoot);
        }

        return false;
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }
        catch (Exception)
        {
            return true;
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
                context.UpdateCounter = InitialCounterValue;
            }
        }
        catch (Exception exception)
        {
            logger.LogFileDeletionFailed(exception, file);
        }
    }


    private sealed class DeletionContext
    {
        public int DeletedCount { get; set; }
        public int UpdateCounter { get; set; }
        public int ReportInterval { get; set; }
        public IProgress<string> StatusReporter { get; set; } = null!;
        public CancellationToken Token { get; set; }
    }
}