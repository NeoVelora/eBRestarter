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

        if (string.Equals(variable, AppDataEnvironmentVariableName, StringComparison.OrdinalIgnoreCase))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        if (string.Equals(variable, LocalAppDataEnvironmentVariableName, StringComparison.OrdinalIgnoreCase))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (string.Equals(variable, ProgramFilesEnvironmentVariableName, StringComparison.OrdinalIgnoreCase))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        if (string.Equals(variable, ProgramFilesX86EnvironmentVariableName, StringComparison.OrdinalIgnoreCase))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        }

        return string.Empty;
    }

    public void WriteAllText(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllText(path, content);
    }
}
