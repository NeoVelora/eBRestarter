using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Application.Services.Ports.Interfaces
{
    public interface IFileSystemService
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        string GetEnvironmentPath(string variable);
        string CombinePaths(params string[] paths);
    }
}
