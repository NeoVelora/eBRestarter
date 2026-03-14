using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Infrastructure.Services;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Services;

public class FileDeletionServiceTests : IDisposable
{
    private readonly FileDeletionService _sut;
    private readonly string _testDirectory;

    public FileDeletionServiceTests()
    {
        _sut = new FileDeletionService();
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileDeletionServiceTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); } catch { }
        }
    }

    [Fact]
    public async Task CountFilesAsync_ShouldReturnCorrectFileCount()
    {
        var dir1 = Path.Combine(_testDirectory, "dir1");
        var dir2 = Path.Combine(_testDirectory, "dir2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        File.WriteAllText(Path.Combine(dir1, "file1.txt"), "test");
        File.WriteAllText(Path.Combine(dir1, "file2.txt"), "test");
        File.WriteAllText(Path.Combine(dir2, "file3.txt"), "test");

        var directories = new List<string> { dir1, dir2, "NonExistentDirectory" };

        var count = await _sut.CountFilesAsync(directories);

        count.ShouldBe(3);
    }

    [Fact]
    public async Task DeleteFilesAsync_ShouldDeleteFilesAndDirectories()
    {
        var dir1 = Path.Combine(_testDirectory, "dir1");
        Directory.CreateDirectory(dir1);
        var file1 = Path.Combine(dir1, "file1.txt");
        var file2 = Path.Combine(dir1, "file2.txt");
        File.WriteAllText(file1, "test");
        File.WriteAllText(file2, "test");

        var directories = new List<string> { dir1 };
        
        var reportedStatuses = new List<string>();
        var reportedValues = new List<int>();
        var statusReporter = new Progress<string>(s => reportedStatuses.Add(s));
        var valueReporter = new Progress<int>(v => reportedValues.Add(v));

        await _sut.DeleteFilesAsync(directories, statusReporter, valueReporter, CancellationToken.None);

        File.Exists(file1).ShouldBeFalse();
        File.Exists(file2).ShouldBeFalse();
        Directory.Exists(dir1).ShouldBeFalse();

        reportedStatuses.ShouldContain("Abschließe Bereinigung...");
        reportedValues.ShouldContain(2);
    }

    [Fact]
    public async Task DeleteFilesAsync_WhenCancellationRequested_ShouldStopExecution()
    {
        var dir1 = Path.Combine(_testDirectory, "dir1");
        Directory.CreateDirectory(dir1);
        File.WriteAllText(Path.Combine(dir1, "file1.txt"), "test");
        File.WriteAllText(Path.Combine(dir1, "file2.txt"), "test");

        var directories = new List<string> { dir1 };
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var statusReporter = new Progress<string>();
        var valueReporter = new Progress<int>();

        try
        {
            await _sut.DeleteFilesAsync(directories, statusReporter, valueReporter, cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        Directory.Exists(dir1).ShouldBeTrue();
    }

    [Fact]
    public void DeleteSingleFile_ShouldDeleteFile()
    {
        var file = Path.Combine(_testDirectory, "singlefile.txt");
        File.WriteAllText(file, "content");

        _sut.DeleteSingleFile(file);

        File.Exists(file).ShouldBeFalse();
    }

    [Fact]
    public void DeleteSingleFile_WhenFileDoesNotExist_ShouldNotThrow()
    {
        var file = Path.Combine(_testDirectory, "nonexistent.txt");

        Should.NotThrow(() => _sut.DeleteSingleFile(file));
    }
}