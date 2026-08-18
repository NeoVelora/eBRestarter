using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Service.WindowsOS;
using eBRestarter.Tests.TestDoubles;

namespace eBRestarter.Tests.Infrastructure.Handlers
{
    public class WindowsFileDeletionServiceAdapterTests : IDisposable
    {
        private readonly AdapterWindowsFileDeletionService _sut;
        private readonly string _baseTestDir;

        public WindowsFileDeletionServiceAdapterTests()
        {
            _sut = new AdapterWindowsFileDeletionService(Microsoft.Extensions.Logging.Abstractions.NullLogger<AdapterWindowsFileDeletionService>.Instance);
            _baseTestDir = Path.Combine(Path.GetTempPath(), $"FileDeletionTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_baseTestDir);
        }

        [Fact]
        public async Task DeleteFilesAsync_ShouldCleanUpEverything_ExceptExclusions()
        {
            string subDir = Path.Combine(_baseTestDir, "ToBeDeleted");
            Directory.CreateDirectory(subDir);

            string normalFile = Path.Combine(subDir, "normal.txt");
            string protectedFile = Path.Combine(subDir, "moz-extension-data.txt");

            File.WriteAllText(normalFile, "bye");
            File.WriteAllText(protectedFile, "stay");

            var statusUpdates = new List<string>();
            var progressUpdates = new List<int>();

            var statusReporter = new SynchronousProgress<string>(statusUpdates.Add);
            var valueReporter = new SynchronousProgress<int>(progressUpdates.Add);

            await _sut.DeleteFilesAsync(
                new List<string> { subDir },
                statusReporter,
                valueReporter,
                CancellationToken.None);

            File.Exists(normalFile).ShouldBeFalse("Standard file should be deleted.");
            File.Exists(protectedFile).ShouldBeTrue("moz-extension file should be preserved.");

            statusUpdates.ShouldContain("Finalizing cleanup operations...");
            progressUpdates.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task DeleteFilesAsync_ShouldStop_WhenCanceled()
        {
            for (int i = 0; i < 10; i++)
            {
                File.WriteAllText(Path.Combine(_baseTestDir, $"file{i}.txt"), "data");
            }

            var cts = new CancellationTokenSource();
            var statusReporter = new Progress<string>(_ => { });
            var valueReporter = new Progress<int>(_ => { });

            cts.Cancel();

            var exception = await Record.ExceptionAsync(async () =>
                await _sut.DeleteFilesAsync(new List<string> { _baseTestDir }, statusReporter, valueReporter, cts.Token)
            );

            exception.ShouldBeOfType<TaskCanceledException>();
            Directory.GetFiles(_baseTestDir).Length.ShouldBe(10);
        }

        [Fact]
        public void DeleteSingleFile_ShouldWork_IfFileExists()
        {
            string path = Path.Combine(_baseTestDir, "single.txt");
            File.WriteAllText(path, "content");

            _sut.DeleteSingleFile(path);

            File.Exists(path).ShouldBeFalse();
        }

        [Fact]
        public void DeleteSingleFile_ShouldNotThrow_IfFileDoesNotExist()
        {
            const string nonExistentPath = @"C:\NonExistentFile.xyz";

            Should.NotThrow(() => _sut.DeleteSingleFile(nonExistentPath));
        }

        public void Dispose()
        {
            if (Directory.Exists(_baseTestDir))
            {
                try { Directory.Delete(_baseTestDir, true); } catch { }
            }

            GC.SuppressFinalize(this);
        }
    }
}
