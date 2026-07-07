using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using System.Text.Json;

namespace eBRestarter.Infrastructure.Repositories.Authentication;

public sealed class JsonCredentialStoreRepository : IOutboundPortCredentialStoreRepository
{
    private readonly IOutboundPortFileSystem _fileSystem;
    private readonly IOutboundPortAppPathProvider _pathProvider;
    private readonly string _storagePath;

    public JsonCredentialStoreRepository(IOutboundPortFileSystem fileSystem, IOutboundPortAppPathProvider pathProvider)
    {
        _fileSystem = fileSystem;
        _pathProvider = pathProvider;

        var appData = _pathProvider.RetrieveLocalAppDataDirectory();
        _storagePath = _fileSystem.CombinePaths(appData, "Skylar", "eBRestarter", "eBRestarterConfig.json");
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
        catch
        {
            return null;
        }
    }

    public void ClearCredentials()
    {
        if (_fileSystem.FileExists(_storagePath))
        {
            _fileSystem.DeleteFile(_storagePath);
        }
    }

    public ApiCredentials? ImportFromLegacyFile(string filePath)
    {
        if (!_fileSystem.FileExists(filePath)) return null;

        try
        {
            using var stream = _fileSystem.OpenRead(filePath);
            using var reader = new BinaryReader(stream);
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


