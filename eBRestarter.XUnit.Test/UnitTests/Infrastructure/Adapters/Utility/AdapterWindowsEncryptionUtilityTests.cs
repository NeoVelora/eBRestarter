using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Utilities;

namespace eBRestarter.XUnit.Test.UnitTests.Infrastructure.Adapters.Utility;

[Trait("Category", "Unit")]
public class AdapterWindowsEncryptionUtilityTests
{
    private readonly AdapterWindowsEncryptionUtility _sut = new(NullLogger<AdapterWindowsEncryptionUtility>.Instance);

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new AdapterWindowsEncryptionUtility(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Encrypt_ShouldReturnEmpty_WhenInputIsNullOrWhiteSpace(string? input)
    {
        string result = _sut.Encrypt(input!);

        result.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decrypt_ShouldReturnEmpty_WhenInputIsNullOrWhiteSpace(string? input)
    {
        string result = _sut.Decrypt(input!);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Encrypt_And_Decrypt_ShouldRoundTripPlainText()
    {
        const string plainText = "SuperSecret_ApiKey_2026!#$";

        string encrypted = _sut.Encrypt(plainText);
        string decrypted = _sut.Decrypt(encrypted);

        encrypted.ShouldNotBeNullOrWhiteSpace();
        encrypted.ShouldNotBe(plainText);
        decrypted.ShouldBe(plainText);
    }

    [Theory]
    [InlineData("NotValidBase64!")]
    [InlineData("abc")]
    [InlineData("???===+++")]
    public void Decrypt_ShouldReturnEmpty_WithoutThrowing_WhenInputIsNotValidBase64(string invalidBase64)
    {
        string decrypted = _sut.Decrypt(invalidBase64);

        decrypted.ShouldBe(string.Empty);
    }

    [Fact]
    public void Decrypt_ShouldReturnEmpty_WhenInputIsValidBase64_ButNotValidDPAPIPayload()
    {
        string fakePayload = Convert.ToBase64String([1, 2, 3, 4, 5, 6, 7, 8]);

        string decrypted = _sut.Decrypt(fakePayload);

        decrypted.ShouldBe(string.Empty);
    }
}
