using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;

namespace eBRestarter.Infrastructure.Services.WindowsOS
{
    [SupportedOSPlatform("windows")]
    public class WindowsFileSystemService : IWindowsFileSystemService
    {
        public bool FileExists(string path) => File.Exists(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
        public string CombinePaths(params string[] paths) => Path.Combine(paths);
        public string GetEnvironmentPath(string variable)
        {
            var path = Environment.GetEnvironmentVariable(variable);

            if (!string.IsNullOrEmpty(path)) return path;

            return variable.ToLower() switch
            {
                "appdata" => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "localappdata" => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "programfiles" => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "programfiles(x86)" => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                _ => string.Empty
            };
        }

        public void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Pfad darf nicht leer sein.", nameof(path));
            }

            File.Delete(path);
        }

        public void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        public string ReadAllText(string path)
        {
            var content = File.ReadAllText(path);

            return content;
        }
    }
}
