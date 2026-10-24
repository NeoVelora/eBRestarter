using System.Text.Json.Serialization;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

namespace eBRestarter.Infrastructure.BehavioralComponents.Repositories.Authentication;

[JsonSerializable(typeof(ApiCredentials))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
public sealed partial class ApiCredentialsJsonContext : JsonSerializerContext;
