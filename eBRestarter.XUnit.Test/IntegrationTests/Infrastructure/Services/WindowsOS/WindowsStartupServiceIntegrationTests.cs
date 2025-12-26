using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Infrastructure.Wrapper;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;

namespace eBRestarter.XUnit.Test.IntegrationTests.Infrastructure.Services.WindowsOS
{
    [SupportedOSPlatform("windows")]
    public class WindowsStartupServiceIntegrationTests : IDisposable
    {
        private readonly WindowsStartupService _service;

        private const string AppName = "eBRestarter";
        private const string RunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string EdgePath = @"SOFTWARE\Policies\Microsoft\Edge";
        private const string PasswordLessPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

        public WindowsStartupServiceIntegrationTests()
        {
            var logger = NullLogger<WindowsStartupService>.Instance;
            var realRegistry = new WindowsRegistryService();
            var realProcessInfo = new ProcessInfoService();

            _service = new WindowsStartupService(logger, realRegistry, realProcessInfo);

            CleanupRegistry();
        }


        [Fact]
        [Trait("Category", "Integration")]
        public void EnableAutoStart_Should_Create_Real_Registry_Entry()
        {
            _service.EnableAutoStart();

            using var key = Registry.CurrentUser.OpenSubKey(RunPath, false);
            var value = key?.GetValue(AppName);

            value.ShouldNotBeNull("weil EnableAutoStart einen Eintrag erstellen sollte.");

            var pathString = value?.ToString();
            pathString.ShouldEndWith(".exe");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void DisableAutoStart_Should_Remove_Real_Registry_Entry()
        {
            _service.EnableAutoStart();

            using (var checkKey = Registry.CurrentUser.OpenSubKey(RunPath, false))
            {
                checkKey.GetValue(AppName).ShouldNotBeNull();
            }

            _service.DisableAutoStart();

            using var key = Registry.CurrentUser.OpenSubKey(RunPath, false);
            var value = key?.GetValue(AppName);

            value.ShouldBeNull("weil DisableAutoStart den Eintrag entfernen muss.");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void GetStartupEntries_Should_Find_Our_Entry()
        {
            _service.EnableAutoStart();

            var entries = _service.GetStartupEntries();

            entries.ShouldContainKey(AppName, "weil wir uns gerade selbst in den Autostart eingetragen haben.");
        }


        [Fact]
        [Trait("Category", "Integration_Admin")]
        public void SetEdgeStartupBoost_Should_Modify_HKLM_If_Admin()
        {
            if (!IsAdministrator())
            {
                Assert.True(true, "SKIP: Test übersprungen, da keine Administratorrechte vorhanden.");
                return;
            }

            _service.SetEdgeStartupBoost(false);

            using var key = Registry.LocalMachine.OpenSubKey(EdgePath, false);
            key.ShouldNotBeNull("weil der Key erstellt sein sollte.");

            var val = key!.GetValue("StartupBoostEnabled");
            val.ShouldBe(0, "weil wir 'false' übergeben haben (0 = Disabled).");

            _service.SetEdgeStartupBoost(true);
            key.GetValue("StartupBoostEnabled").ShouldBe(1);
        }

        [Fact]
        [Trait("Category", "Integration_Admin")]
        public void SetAutoLogon_Should_Modify_HKLM_If_Admin()
        {
            if (!IsAdministrator())
            {
                Assert.True(true, "SKIP: Test übersprungen, da keine Administratorrechte vorhanden.");
                return;
            }

            _service.SetAutoLogon(true);

            using var key = Registry.LocalMachine.OpenSubKey(PasswordLessPath, false);
            key.ShouldNotBeNull();

            var val = key!.GetValue("DevicePasswordLessBuildVersion");
            val.ShouldBe(0, "weil 'enable=true' (AutoLogon an) bedeutet 'PasswordLess=aus' (0).");

            _service.SetAutoLogon(false);
            key.GetValue("DevicePasswordLessBuildVersion").ShouldBe(2);
        }


        public void Dispose()
        {
            CleanupRegistry();
        }

        private void CleanupRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunPath, true);
                if (key != null && key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch
            {
            }
        }

        private bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
