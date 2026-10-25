using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Security;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.BehavioralComponents.Serialization;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.Authentication;

public sealed class AdapterJsonCredentialStoreRepository(
    IOutboundPortEncryption encryption,
    IOutboundPortFileSystem fileSystem,
    ILogger<AdapterJsonCredentialStoreRepository> logger,
    IOutboundPortAppPathProvider pathProvider)
    : IOutboundPortCredentialStoreRepository
{

    private const string AppFolderName = "eBRestarter";
    private const string ConfigFileName = "eBRestarterConfig.json";
    private const string SkylarFolderName = "Skylar";

    private const int MaximumJsonFileCharacterCount = 64 * 1024;
    private const string StoredCredentialsTooLargeLogMessage = "Stored credentials file at {Path} exceeds maximum permitted length of 64 KB.";

    private const string DecryptionFailedLogMessage = "The stored API key could not be decrypted (possibly copied from another machine or user profile). The key must be re-entered.";
    private const string EncryptionFailedExceptionMessage = "The API key could not be encrypted and was therefore NOT written to disk.";
    private const string LegacyImportFailedLogMessage = "Legacy credential file {Path} could not be read.";
    private const string LoadFailedLogMessage = "Stored credentials at {Path} could not be read.";

    private static readonly JsonSerializerOptions StrictJsonOptions = new()
    {
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver = ApiCredentialsJsonContext.Default
    };


    private readonly IOutboundPortEncryption _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
    private readonly IOutboundPortFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger<AdapterJsonCredentialStoreRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

            return LegacyCredentialFileReader.Read(stream);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(LogEventIds.Configuration.CredentialStoreLegacyImportFailed, exception, LegacyImportFailedLogMessage, filePath);

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

            if (json.Length > MaximumJsonFileCharacterCount)
            {
                _logger.LogError(LogEventIds.Configuration.ConfigurationInvalidJson, StoredCredentialsTooLargeLogMessage, _storagePath);
                return null;
            }

            var storedCredentials = JsonSerializer.Deserialize<ApiCredentials>(json, StrictJsonOptions);

            if (storedCredentials is null)
            {
                return null;
            }

            var plainApiKey = _encryption.Decrypt(storedCredentials.ApiKey);

            if (string.IsNullOrEmpty(plainApiKey) && !string.IsNullOrEmpty(storedCredentials.ApiKey))
            {
                _logger.LogWarning(LogEventIds.Configuration.ApiKeyDecryptionFailed, DecryptionFailedLogMessage);
            }

            return storedCredentials with { ApiKey = plainApiKey };
        }
        catch (Exception exception)
        {
            _logger.LogError(LogEventIds.Configuration.CredentialStoreLoadFailed, exception, LoadFailedLogMessage, _storagePath);

            return null;
        }
    }

    public void SaveCredentials(ApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var cipherText = string.IsNullOrEmpty(credentials.ApiKey)
            ? string.Empty
            : _encryption.Encrypt(credentials.ApiKey);

        if (!string.IsNullOrEmpty(credentials.ApiKey) && string.IsNullOrEmpty(cipherText))
        {
            throw new InvalidOperationException(EncryptionFailedExceptionMessage);
        }

        var json = JsonSerializer.Serialize(credentials with { ApiKey = cipherText }, StrictJsonOptions);

        _fileSystem.WriteAllText(_storagePath, json);
    }


    private static class LegacyCredentialFileReader
    {
        private const long MaximumLegacyFileSizeBytes = 64 * 1024;

        public static ApiCredentials? Read(Stream stream)
        {
            if (stream.CanSeek && stream.Length > MaximumLegacyFileSizeBytes)
            {
                return null;
            }

            using var reader = new BinaryReader(stream);

            var username = reader.ReadString();
            var apiKey = reader.ReadString();

            return new ApiCredentials(username, apiKey);
        }
    }
}

