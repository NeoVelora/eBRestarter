using BenchmarkDotNet.Attributes;
using eBRestarter.Core.Domain.Validators;

namespace eBRestarter.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class UsernameValidationBenchmark
{
    private const string ValidUsername = "user_123.test-name";
    private const string InvalidUsername = "user!@#invalid$%";

    [Benchmark]
    public bool ValidateValidUsername() => EVisitorUsernamePolicy.IsValid(ValidUsername);

    [Benchmark]
    public bool ValidateInvalidUsername() => EVisitorUsernamePolicy.IsValid(InvalidUsername);
}
