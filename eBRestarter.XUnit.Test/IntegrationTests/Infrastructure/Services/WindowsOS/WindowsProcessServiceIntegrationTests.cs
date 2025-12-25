using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Infrastructure.Wrapper;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.XUnit.Test.IntegrationTests.Infrastructure.Services.WindowsOS
{
    public class WindowsProcessServiceIntegrationTests : IDisposable
    {
        private readonly WindowsProcessService _service;
        private const string TestAppName = "notepad";
        private const string TestAppPath = @"C:\Windows\System32\notepad.exe";

        public WindowsProcessServiceIntegrationTests()
        {
            KillExistingNotepads();

            var logger = NullLogger<WindowsProcessService>.Instance;
            var realWrapper = new RealProcessWrapper();

            _service = new WindowsProcessService(logger, realWrapper);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void StartExecutable_And_IsProcessAlive_Should_Work_With_Real_Windows()
        {
            _service.StartExecutable(TestAppPath);

            Thread.Sleep(1000);

            bool isRunning = _service.IsProcessAlive(TestAppName);
            isRunning.ShouldBeTrue("weil Notepad gerade gestartet wurde");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void CloseApplication_Should_Actually_Kill_Notepad()
        {
            Process.Start(TestAppPath);
            Thread.Sleep(1000);

            _service.IsProcessAlive(TestAppName).ShouldBeTrue();

            _service.CloseApplication(TestAppName);
            Thread.Sleep(500);

            bool isRunning = _service.IsProcessAlive(TestAppName);
            isRunning.ShouldBeFalse("weil CloseApplication den Prozess beenden sollte");
        }

        public void Dispose()
        {
            KillExistingNotepads();
        }

        private void KillExistingNotepads()
        {
            foreach (var p in Process.GetProcessesByName(TestAppName))
            {
                try { p.Kill(); } catch { }
            }
        }
    }
}
