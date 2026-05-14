using System.IO;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;

public interface IWindowsFileSystemService
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string CombinePaths(params string[] paths);
    string ResolveEnvironmentPath(string variable);
    void DeleteFile(string path);
    void WriteAllText(string path, string content);
    string ReadAllText(string path);
    string[] ReadAllLines(string path);
    
    void CreateDirectory(string path);
    string? GetDirectoryName(string path);
    Stream OpenRead(string path);
}
