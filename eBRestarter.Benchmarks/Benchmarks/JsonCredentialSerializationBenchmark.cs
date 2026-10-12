using System.Text.Json;
using BenchmarkDotNet.Attributes;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Infrastructure.BehavioralComponents.Repositories.Authentication;

namespace eBRestarter.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class JsonCredentialSerializationBenchmark
{
    private static readonly ApiCredentials Credentials = new("evisitor_bench_user", "encrypted_api_key_sample_token_xyz");
    private static readonly string JsonPayload = JsonSerializer.Serialize(Credentials, ApiCredentialsJsonContext.Default.ApiCredentials);

    [Benchmark]
    public string SerializeWithSourceGen()
    {
        return JsonSerializer.Serialize(Credentials, ApiCredentialsJsonContext.Default.ApiCredentials);
    }

    [Benchmark]
    public ApiCredentials? DeserializeWithSourceGen()
    {
        return JsonSerializer.Deserialize(JsonPayload, ApiCredentialsJsonContext.Default.ApiCredentials);
    }
}
