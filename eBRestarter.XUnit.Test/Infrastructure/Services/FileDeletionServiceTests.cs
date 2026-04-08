using eBRestarter.Infrastructure.Services;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services
{
    public class FileDeletionServiceTests : IDisposable
    {
        private readonly FileDeletionService _sut;
        private readonly string _baseTestDir;

        public FileDeletionServiceTests()
        {
            _sut = new FileDeletionService();
            _baseTestDir = Path.Combine(Path.GetTempPath(), $"FileDeletionTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_baseTestDir);
        }


        [Fact]
        public async Task CountFilesAsync_ShouldReturnCorrectNumberOfFiles_IncludingSubdirectories()
        {
            string subDir = Path.Combine(_baseTestDir, "Sub");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(_baseTestDir, "file1.txt"), "test");
            File.WriteAllText(Path.Combine(_baseTestDir, "file2.txt"), "test");
            File.WriteAllText(Path.Combine(subDir, "file3.txt"), "test");

            var dirs = new List<string> { _baseTestDir };

            int count = await _sut.CountFilesAsync(dirs);

            count.ShouldBe(3);
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

            var statusReporter = new Progress<string>(s => statusUpdates.Add(s));
            var valueReporter = new Progress<int>(v => progressUpdates.Add(v));

            await _sut.DeleteFilesAsync(
                new List<string> { subDir },
                statusReporter,
                valueReporter,
                CancellationToken.None);

            File.Exists(normalFile).ShouldBeFalse("Normale Datei sollte gelöscht sein.");
            File.Exists(protectedFile).ShouldBeTrue("moz-extension Datei sollte ignoriert worden sein.");


            statusUpdates.ShouldContain("Abschließe Bereinigung...");
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
            Should.NotThrow(() => _sut.DeleteSingleFile("C:\\NonExistentFile.xyz"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_baseTestDir))
            {
                try { Directory.Delete(_baseTestDir, true); } catch { }
            }
        }
    }
}