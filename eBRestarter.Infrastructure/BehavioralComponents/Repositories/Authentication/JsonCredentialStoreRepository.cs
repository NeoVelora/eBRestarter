using System;
using System.IO;
using System.Text.Json;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Infrastructure.BehavioralComponents.Repositories.Authentication;

public sealed class JsonCredentialStoreRepository(
    IOutboundPortFileSystem fileSystem,
    IOutboundPortAppPathProvider pathProvider)
    : IOutboundPortCredentialStoreRepository
{

    private const string AppFolderName = "eBRestarter";
    private const string ConfigFileName = "eBRestarterConfig.json";
    private const string SkylarFolderName = "Skylar";


    private readonly IOutboundPortFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

    private readonly string _storagePath = (fileSystem ?? throw new ArgumentNullException(nameof(fileSystem))).CombinePaths(
        (pathProvider ?? throw new ArgumentNullException(nameof(pathProvider))).RetrieveLocalAppDataDirectory(),
        SkylarFolderName, AppFolderName, ConfigFileName);


    public void ClearCredentials()
    {
        if (_fileSystem.FileExists(_storagePath))
        {
            _fileSystem.DeleteFile(_storagePath);
        }
    }

    public ApiCredentials? ImportFromLegacyFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!_fileSystem.FileExists(filePath))
        {
            return null;
        }

        try
        {
            using var stream = _fileSystem.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            var username = reader.ReadString();
            var apiKey = reader.ReadString();

            return new ApiCredentials(username, apiKey);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public ApiCredentials? LoadCredentials()
    {
        if (!_fileSystem.FileExists(_storagePath))
        {
            return null;
        }

        try
        {
            var json = _fileSystem.ReadAllText(_storagePath);
            return JsonSerializer.Deserialize<ApiCredentials>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SaveCredentials(ApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var json = JsonSerializer.Serialize(credentials);
        _fileSystem.WriteAllText(_storagePath, json);
    }
}
