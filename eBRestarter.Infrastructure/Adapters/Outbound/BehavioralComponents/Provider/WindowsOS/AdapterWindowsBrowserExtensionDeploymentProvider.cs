using System;
using System.IO;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS;

public sealed class AdapterWindowsBrowserExtensionDeploymentProvider : IOutboundPortBrowserExtensionDeployment, IOutboundPortBrowserExtensionPathProvider
{
#if !DEBUG
    private const string AllDirectoriesSearchPattern = "*";
    private const string AllFilesSearchPattern = "*.*";
#endif
#if DEBUG
    private const string DebugSolutionRelativePath = @"..\..\..\..\..\..\";
    private const string ExtensionProjectFolderName = "eBRestarter.TabRestarterExtension";
#endif
    private const string ExtensionFolderName = "TabRestarterExtension";


    public void EnsureExtensionIsDeployed()
    {
#if !DEBUG
        string targetPath = RetrieveExtensionFolderPath();
        string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExtensionFolderName);

        if (Directory.Exists(targetPath) || !Directory.Exists(sourcePath))
        {
            return;
        }

        Directory.CreateDirectory(targetPath);
        CopyDirectoryContents(sourcePath, targetPath);
#endif
    }

    public string RetrieveExtensionFolderPath()
    {
#if DEBUG
        // In Debug mode, navigate 6 directory levels up to reach the solution root directory context.
        var solutionDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DebugSolutionRelativePath));
        return Path.Combine(solutionDirectory, ExtensionProjectFolderName, ExtensionFolderName);
#else
        
        string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataDirectory, ExtensionFolderName);
#endif
    }

#if !DEBUG
        private static void CopyDirectoryContents(string sourceDir, string targetDir)
    {
        foreach (var dirPath in Directory.GetDirectories(sourceDir, AllDirectoriesSearchPattern, SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, dirPath);
            Directory.CreateDirectory(Path.Combine(targetDir, relativePath));
        }

        foreach (var newPath in Directory.GetFiles(sourceDir, AllFilesSearchPattern, SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, newPath);
            File.Copy(newPath, Path.Combine(targetDir, relativePath), overwrite: true);
        }
    }
#endif
}