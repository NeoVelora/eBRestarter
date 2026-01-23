using eBRestarter.Core.Application.Interfaces.Authentication;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Models.Records;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace eBRestarter.Infrastructure.Services.Authentication
{
    public class JsonCredentialStore : ICredentialStore
    {
        private readonly IWindowsFileSystemService _fileSystem;
        private readonly string _storagePath;

        public JsonCredentialStore(IWindowsFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;

            var appData = fileSystem.GetEnvironmentPath("LocalAppData");

            _storagePath = Path.Combine(appData, "Skylar", "eBRestarter", "eBRestarterConfig.json");
        }

        public void SaveCredentials(ApiCredentials credentials)
        {
            var json = JsonSerializer.Serialize(credentials);
            _fileSystem.WriteAllText(_storagePath, json);
        }

        public ApiCredentials? LoadCredentials()
        {
            if (!_fileSystem.FileExists(_storagePath)) return null;

            try
            {
                var json = _fileSystem.ReadAllText(_storagePath);
                return JsonSerializer.Deserialize<ApiCredentials>(json);
            }
            catch { return null; }
        }

        public void ClearCredentials()
        {
            if (_fileSystem.FileExists(_storagePath)) _fileSystem.DeleteFile(_storagePath);
        }

        public ApiCredentials? ImportFromLegacyFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
                var user = reader.ReadString();
                var key = reader.ReadString();
                return new ApiCredentials(user, key);
            }
            catch
            {
                return null;
            }
        }
    }
}
