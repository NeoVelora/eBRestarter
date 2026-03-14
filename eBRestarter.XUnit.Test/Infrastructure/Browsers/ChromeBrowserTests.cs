using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Browsers;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.IO;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers;

public class ChromeBrowserTests
{
    private readonly Mock<IOperatingSystemFacade> _mockOsFacade;
    private readonly Mock<IWindowsProcessControlService> _mockProcessService;
    private readonly Mock<IWindowsRegistryService> _mockRegistryService;
    private readonly Mock<IWindowsFileSystemService> _mockFileSystemService;
    private readonly Mock<ILogger<ChromeBrowser>> _mockLogger;
    private readonly ChromeBrowser _sut;

    public ChromeBrowserTests()
    {
        _mockOsFacade = new Mock<IOperatingSystemFacade>();
        _mockProcessService = new Mock<IWindowsProcessControlService>();
        _mockRegistryService = new Mock<IWindowsRegistryService>();
        _mockFileSystemService = new Mock<IWindowsFileSystemService>();
        _mockLogger = new Mock<ILogger<ChromeBrowser>>();

        _mockOsFacade.Setup(os => os.WindowsProcessControlService).Returns(_mockProcessService.Object);
        _mockOsFacade.Setup(os => os.WindowsRegistryService).Returns(_mockRegistryService.Object);
        _mockOsFacade.Setup(os => os.WindowsFileSystemService).Returns(_mockFileSystemService.Object);

        _mockFileSystemService.Setup(f => f.GetEnvironmentPath("LocalAppData")).Returns(@"C:\Users\Test\AppData\Local");
        _mockFileSystemService.Setup(f => f.GetEnvironmentPath("ProgramFiles")).Returns(@"C:\Program Files");
        _mockFileSystemService.Setup(f => f.GetEnvironmentPath("ProgramFiles(x86)")).Returns(@"C:\Program Files (x86)");
        _mockFileSystemService.Setup(f => f.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => Path.Combine(paths));

        _sut = new ChromeBrowser(_mockOsFacade.Object, _mockLogger.Object);
    }

    [Fact]
    public void Start_ShouldCallProcessServiceWithCorrectUrlAndArguments()
    {
        var url = "https://www.google.com";
        var expectedExePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        
        _mockFileSystemService.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileSystemService.Setup(f => f.FileExists(expectedExePath)).Returns(true);
        _mockFileSystemService.Setup(f => f.GetEnvironmentPath("ProgramFiles")).Returns(@"C:\Program Files");
        _mockFileSystemService.Setup(f => f.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => Path.Combine(paths));

        _sut.Start(url);

        _mockProcessService.Verify(p => p.OpenUrlInBrowser(expectedExePath, url), Times.Once);
    }

    [Fact]
    public void Start_WhenNoExeFound_ShouldLogError()
    {
        _mockFileSystemService.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        _sut.Start("https://test.com");

        _mockProcessService.Verify(p => p.OpenUrlInBrowser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Close_ShouldKillChromeProcess()
    {
        _sut.Close();

        _mockProcessService.Verify(p => p.CloseApplication("chrome"), Times.Once);
    }

    [Theory]
    [InlineData("115.0.5790.170", "115.0.5790.170")]
    [InlineData(" 115.0.0.1 ", "115.0.0.1")]
    [InlineData("115.0.0.1 (x64 de)", "115.0.0.1")]
    [InlineData(null, "Unknown")]
    public void BrowserVersion_ShouldCleanAndReturnVersion(string? rawVersion, string expectedVersion)
    {
        _mockRegistryService.Setup(r => r.GetCurrentUserValue(@"Software\Google\Chrome\BLBeacon", "version")).Returns(rawVersion);

        var result = _sut.BrowserVersion;

        result.ShouldBe(expectedVersion);
    }

    [Fact]
    public void IsExtensionInstalled_WhenNoProfileDirsFound_ShouldReturnFalse()
    {
        _mockFileSystemService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);

        var result = _sut.IsExtensionInstalled();

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsExtensionInstalled_WhenExtensionDirExists_ShouldReturnTrue()
    {
        var root = @"C:\Users\Test\AppData\Local\Google\Chrome\User Data";
        _mockFileSystemService.Setup(f => f.GetEnvironmentPath("LocalAppData")).Returns(@"C:\Users\Test\AppData\Local");
        _mockFileSystemService.Setup(f => f.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => string.Join("\\", paths));
        
        _mockFileSystemService.Setup(f => f.DirectoryExists($@"{root}\Default")).Returns(true);
        
        var extPath = $@"{root}\Default\Extensions\agchmcconfdfcenopioeilpgjngelefk";
        
        _mockFileSystemService.Setup(f => f.DirectoryExists(extPath)).Returns(true);

        var result = _sut.IsExtensionInstalled();

        result.ShouldBeTrue();
    }
}