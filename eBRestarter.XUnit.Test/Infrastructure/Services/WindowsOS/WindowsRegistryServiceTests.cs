using eBRestarter.Infrastructure.Services.WindowsOS;
using Microsoft.Win32;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Security;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsRegistryServiceTests : IDisposable
    {
        private readonly WindowsRegistryAdapter _sut;
        private readonly string _tempTestKey;

        public WindowsRegistryServiceTests()
        {
            _sut = new WindowsRegistryAdapter();

            _tempTestKey = $@"Software\eBRestarter_TestSandbox_{Guid.NewGuid()}";
        }

        [Fact]
        public void SetAndGetCurrentUserValue_ShouldWriteAndReadCorrectly()
        {
            string valueName = "TestString";
            string expectedValue = "Hallo Registry!";

            _sut.SetCurrentUserValue(_tempTestKey, valueName, expectedValue);
            var actualValue = _sut.RetrieveCurrentUserValue(_tempTestKey, valueName);

            actualValue.ShouldNotBeNull();
            actualValue.ToString().ShouldBe(expectedValue);
        }

        [Fact]
        public void DeleteCurrentUserValue_ShouldRemoveValue_WithoutCrashing()
        {
            string valueName = "DeleteMe";
            _sut.SetCurrentUserValue(_tempTestKey, valueName, "Trash");

            _sut.DeleteCurrentUserValue(_tempTestKey, valueName);
            var resultAfterDelete = _sut.RetrieveCurrentUserValue(_tempTestKey, valueName);

            resultAfterDelete.ShouldBeNull();

            Should.NotThrow(() => _sut.DeleteCurrentUserValue(_tempTestKey, valueName));
        }

        [Fact]
        public void GetCurrentUserValues_ShouldReturnAllValuesAsDictionary()
        {
            _sut.SetCurrentUserValue(_tempTestKey, "Wert1", "A");
            _sut.SetCurrentUserValue(_tempTestKey, "Wert2", 42);
            _sut.SetCurrentUserValue(_tempTestKey, "Wert3", "C");

            Dictionary<string, object> results = _sut.RetrieveCurrentUserValues(_tempTestKey);

            results.ShouldNotBeNull();
            results.Count.ShouldBeGreaterThanOrEqualTo(3);

            results["Wert1"].ToString().ShouldBe("A");
            results["Wert2"].ShouldBe(42);
        }

        [Fact]
        public void GetCurrentUserValues_ShouldReturnEmptyDictionary_WhenKeyDoesNotExist()
        {
            var results = _sut.RetrieveCurrentUserValues($@"Software\GhostKey_{Guid.NewGuid()}");

            results.ShouldNotBeNull();
            results.ShouldBeEmpty();
        }

        [Fact]
        public void GetLocalMachineValue_ShouldReadExistingWindowsKey()
        {
            string winNtPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            string valueName = "ProductName";

            var result = _sut.RetrieveLocalMachineValue(winNtPath, valueName);

            result.ShouldNotBeNull();
            result.ToString().ShouldContain("Windows");
        }

        [Fact]
        public void SetLocalMachineValue_ShouldWriteIfAdmin_OrThrowSecurityException()
        {
            string hklmTestKey = $@"SOFTWARE\eBRestarter_HKLM_Test_{Guid.NewGuid()}";

            try
            {
                _sut.SetLocalMachineValue(hklmTestKey, "AdminTest", "Success", RegistryValueKind.String);

                var result = _sut.RetrieveLocalMachineValue(hklmTestKey, "AdminTest");
                result.ShouldNotBeNull();
                result.ToString().ShouldBe("Success");

                Registry.LocalMachine.DeleteSubKeyTree(hklmTestKey, false);
            }
            catch (Exception ex)
            {
                (ex is UnauthorizedAccessException || ex is SecurityException)
                    .ShouldBeTrue("Erwartete fehlende Berechtigung, da der Test-Runner nicht als Administrator läuft.");
            }
        }
        public void Dispose()
        {
            try
            {
                using var baseKey = Registry.CurrentUser.OpenSubKey("Software", true);
                if (baseKey != null)
                {
                    string keyToDelete = _tempTestKey.Replace(@"Software\", "");
                    baseKey.DeleteSubKeyTree(keyToDelete, false);
                }
            }
            catch
            {
            }
        }
    }
}

