using BenchmarkDotNet.Attributes;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Utility;
using Microsoft.Extensions.Logging.Abstractions;

namespace eBRestarter.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class EncryptionBenchmark
{
    private const string SamplePlainText = "sample-super-secret-api-token-123456789";
    private AdapterWindowsEncryptionUtility _encryption = null!;
    private string _encryptedCipherText = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _encryption = new AdapterWindowsEncryptionUtility(NullLogger<AdapterWindowsEncryptionUtility>.Instance);
        _encryptedCipherText = _encryption.Encrypt(SamplePlainText);
    }

    [Benchmark]
    public string EncryptDpapi() => _encryption.Encrypt(SamplePlainText);

    [Benchmark]
    public string DecryptDpapi() => _encryption.Decrypt(_encryptedCipherText);
}
