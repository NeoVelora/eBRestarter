using System;
using System.IO;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.WindowsOS;

public sealed class AdapterWindowsFileSystem : IOutboundPortFileSystem
{

    private const string AppDataEnvironmentVariableName = "appdata";
    private const string LocalAppDataEnvironmentVariableName = "localappdata";
    private const string ProgramFilesEnvironmentVariableName = "programfiles";
    private const string ProgramFilesX86EnvironmentVariableName = "programfiles(x86)";


    public string CombinePaths(params string[] paths) => Path.Combine(paths);

    public void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(path);
    }

    public void DeleteFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public string[] GetDirectories(string path, string searchPattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Directory.GetDirectories(path, searchPattern);
    }

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public Stream OpenRead(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.OpenRead(path);
    }

    public string[] ReadAllLines(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllLines(path);
    }

    public string ReadAllText(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.ReadAllText(path);
    }

    public string ResolveEnvironmentPath(string variable)
    {
        var path = Environment.GetEnvironmentVariable(variable);

        if (!string.IsNullOrEmpty(path))
        {
            return path;
        }

        return variable.ToLowerInvariant() switch
        {
            AppDataEnvironmentVariableName => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            LocalAppDataEnvironmentVariableName => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProgramFilesEnvironmentVariableName => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            ProgramFilesX86EnvironmentVariableName => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            _ => string.Empty
        };
    }

    public void WriteAllText(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllText(path, content);
    }
}
