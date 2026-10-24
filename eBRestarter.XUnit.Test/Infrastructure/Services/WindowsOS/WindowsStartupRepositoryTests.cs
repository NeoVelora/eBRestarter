using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.WindowsOS;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsStartupRepositoryTests
    {
        private readonly FakeLogger<AdapterWindowsStartupRepository> _mockLogger;
        private readonly IOutboundPortSystemConfigurationRepository _mockRegistry;
        private readonly IOutboundPortProcessInfoProvider _mockProcessInfo;
        private readonly AdapterWindowsStartupRepository _sut;

        private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";
        private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

        public WindowsStartupRepositoryTests()
        {
            _mockLogger = new FakeLogger<AdapterWindowsStartupRepository>();
            _mockRegistry = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            _mockProcessInfo = Substitute.For<IOutboundPortProcessInfoProvider>();

            _sut = new AdapterWindowsStartupRepository(_mockLogger, _mockProcessInfo, _mockRegistry);
        }

        [Fact]
        public void EnableAutoStart_ShouldSetRegistryKey_WithCurrentExePath()
        {
            string fakeExePath = @"C:\TestApp\eBRestarter.exe";
            _mockProcessInfo.GetCurrentExecutablePath().Returns(fakeExePath);

            _sut.EnableAutoStart();

            _mockRegistry.Received(1).SetUserValue(
                RegistryPathRun,
                "eBRestarter",
                $"\"{fakeExePath}\"");
        }

        [Fact]
        public void DisableAutoStart_ShouldDeleteRegistryKey()
        {
            _sut.DisableAutoStart();

            _mockRegistry.Received(1).DeleteUserValue(RegistryPathRun, "eBRestarter");
        }

        [Fact]
        public void EnableAutoStart_ShouldCatchException_AndNotCrash()
        {
            _mockProcessInfo.GetCurrentExecutablePath().Returns("dummy.exe");
            _mockRegistry
                .When(r => r.SetUserValue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>()))
                .Do(_ => throw new UnauthorizedAccessException("Access Denied"));

            Action act = () => _sut.EnableAutoStart();

            var exception = act.ShouldThrow<InvalidOperationException>();
            exception.InnerException.ShouldBeOfType<UnauthorizedAccessException>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsAutoStartEnabledAsync_ShouldReturnCorrectStatus(bool isEnabledInRegistry)
        {
            var fakeRegistryEntries = new Dictionary<string, object>();

            if (isEnabledInRegistry)
            {
                fakeRegistryEntries.Add("eBRestarter", @"C:\Path\eB.exe");
            }
            fakeRegistryEntries.Add("SomeOtherApp", @"C:\Other\app.exe");

            _mockRegistry.GetUserValues(RegistryPathRun).Returns(fakeRegistryEntries);

            bool result = await _sut.IsAutoStartEnabledAsync();

            result.ShouldBe(isEnabledInRegistry);
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void SetBrowserStartupBoost_ShouldWriteCorrectDWordValue(bool enable, int expectedDword)
        {
            _sut.SetBrowserStartupBoost(enable);

            _mockRegistry.Received(1).SetSystemValue(
                RegistryPathEdgePolicies,
                "StartupBoostEnabled",
                expectedDword);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(null, true)]
        [InlineData("InvalidString", true)]
        public void IsBrowserStartupBoostEnabled_ShouldReturnExpectedResult(object? registryValue, bool expectedResult)
        {
            _mockRegistry.GetSystemValue(RegistryPathEdgePolicies, "StartupBoostEnabled")
                         .Returns(registryValue);

            bool result = _sut.IsBrowserStartupBoostEnabled();

            result.ShouldBe(expectedResult);
        }

        [Theory]
        [InlineData(true, 0)]
        [InlineData(false, 2)]
        public void SetAutoLogon_ShouldSetInvertedPasswordLessValue(bool enableAutoLogon, int expectedDword)
        {
            _sut.SetAutoLogon(enableAutoLogon);

            _mockRegistry.Received(1).SetSystemValue(
                RegistryPathPasswordLess,
                "DevicePasswordLessBuildVersion",
                expectedDword);
        }

        [Fact]
        public void SetAutoLogon_ShouldCatchUnauthorizedAccessException_Gracefully()
        {
            _mockRegistry
                .When(r => r.SetSystemValue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>()))
                .Do(_ => throw new UnauthorizedAccessException("Need Admin Rights"));

            Action act = () => _sut.SetAutoLogon(true);

            var exception = act.ShouldThrow<UnauthorizedAccessException>();
            exception.Message.ShouldContain("Administrator");
        }
    }
}
