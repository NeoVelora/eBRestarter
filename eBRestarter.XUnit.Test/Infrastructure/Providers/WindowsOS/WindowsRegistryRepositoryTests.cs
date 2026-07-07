using eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Repositories;
using Microsoft.Win32;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Security;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsRegistryRepositoryTests : IDisposable
    {
        private readonly AdapterWindowsRegistryRepository _sut;
        private readonly string _tempTestKey;

        public WindowsRegistryRepositoryTests()
        {
            _sut = new AdapterWindowsRegistryRepository();

            _tempTestKey = $@"Software\eBRestarter_TestSandbox_{Guid.NewGuid()}";
        }

        [Fact]
        public void SetAndGetCurrentUserValue_ShouldWriteAndReadCorrectly()
        {
            string valueName = "TestString";
            string expectedValue = "Hallo Registry!";

            _sut.SetUserValue(_tempTestKey, valueName, expectedValue);
            var actualValue = _sut.GetUserValue(_tempTestKey, valueName);

            actualValue.ShouldNotBeNull();
            actualValue.ToString().ShouldBe(expectedValue);
        }

        [Fact]
        public void DeleteUserValue_ShouldRemoveValue_WithoutCrashing()
        {
            string valueName = "DeleteMe";
            _sut.SetUserValue(_tempTestKey, valueName, "Trash");

            _sut.DeleteUserValue(_tempTestKey, valueName);
            var resultAfterDelete = _sut.GetUserValue(_tempTestKey, valueName);

            resultAfterDelete.ShouldBeNull();

            Should.NotThrow(() => _sut.DeleteUserValue(_tempTestKey, valueName));
        }

        [Fact]
        public void GetCurrentUserValues_ShouldReturnAllValuesAsDictionary()
        {
            _sut.SetUserValue(_tempTestKey, "Wert1", "A");
            _sut.SetUserValue(_tempTestKey, "Wert2", 42);
            _sut.SetUserValue(_tempTestKey, "Wert3", "C");

            Dictionary<string, object> results = _sut.GetUserValues(_tempTestKey);

            results.ShouldNotBeNull();
            results.Count.ShouldBeGreaterThanOrEqualTo(3);

            results["Wert1"].ToString().ShouldBe("A");
            results["Wert2"].ShouldBe(42);
        }

        [Fact]
        public void GetCurrentUserValues_ShouldReturnEmptyDictionary_WhenKeyDoesNotExist()
        {
            var results = _sut.GetUserValues($@"Software\GhostKey_{Guid.NewGuid()}");

            results.ShouldNotBeNull();
            results.ShouldBeEmpty();
        }

        [Fact]
        public void GetLocalMachineValue_ShouldReadExistingWindowsKey()
        {
            string winNtPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            string valueName = "ProductName";

            var result = _sut.GetSystemValue(winNtPath, valueName);

            result.ShouldNotBeNull();
            result.ToString().ShouldContain("Windows");
        }

        [Fact]
        public void SetSystemValue_ShouldWriteIfAdmin_OrThrowSecurityException()
        {
            string hklmTestKey = $@"SOFTWARE\eBRestarter_HKLM_Test_{Guid.NewGuid()}";

            try
            {
                _sut.SetSystemValue(hklmTestKey, "AdminTest", "Success");

                var result = _sut.GetSystemValue(hklmTestKey, "AdminTest");
                result.ShouldNotBeNull();
                result.ToString().ShouldBe("Success");

                Registry.LocalMachine.DeleteSubKeyTree(hklmTestKey, false);
            }
            catch (Exception ex)
            {
                (ex is UnauthorizedAccessException || ex is SecurityException)
                    .ShouldBeTrue("Erwartete fehlende Berechtigung, da der Test-Runner nicht als Administrator l�uft.");
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

