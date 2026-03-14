using eBRestarter.Core.Application.Interfaces.Update;
using eBRestarter.Core.Application.UseCases.ManageApplicationUpdates;
using eBRestarter.Core.Domain.Models.Records;
using Moq;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.XUnit.Test.Application.UseCases;

public class ManageApplicationUpdatesServiceTests
{
    private readonly Mock<IUpdateService> _mockUpdateService;
    private readonly ManageApplicationUpdatesService _sut;

    public ManageApplicationUpdatesServiceTests()
    {
        _mockUpdateService = new Mock<IUpdateService>();
        _sut = new ManageApplicationUpdatesService(_mockUpdateService.Object);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReturnResponseWithAvailableStatus()
    {
        var updateInfo = new UpdateInfo { IsUpdateAvailable = true, LatestVersion = "2.0.0", Changelog = "Release Notes", DownloadUrl = "http://download.url" };
        _mockUpdateService.Setup(u => u.CheckForUpdateAsync()).ReturnsAsync(updateInfo);

        var result = await _sut.CheckForUpdatesAsync();

        result.IsUpdateAvailable.ShouldBeTrue();
        result.LatestVersion.ShouldBe("2.0.0");
        _mockUpdateService.Verify(u => u.CheckForUpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task PerformUpdateAsync_WhenUpdateAvailable_ShouldDownloadAndInstall()
    {
        var updateInfo = new UpdateInfo { IsUpdateAvailable = true, LatestVersion = "2.0.0", Changelog = "Release Notes", DownloadUrl = "http://download.url" };
        _mockUpdateService.Setup(u => u.CheckForUpdateAsync()).ReturnsAsync(updateInfo);

        await _sut.PerformUpdateAsync();

        _mockUpdateService.Verify(u => u.CheckForUpdateAsync(), Times.Once);
        _mockUpdateService.Verify(u => u.DownloadAndInstallAsync(updateInfo), Times.Once);
    }

    [Fact]
    public async Task PerformUpdateAsync_WhenNoUpdate_ShouldNotDownload()
    {
        var updateInfo = new UpdateInfo { IsUpdateAvailable = false, LatestVersion = "1.0.0", Changelog = string.Empty, DownloadUrl = string.Empty };
        _mockUpdateService.Setup(u => u.CheckForUpdateAsync()).ReturnsAsync(updateInfo);

        await _sut.PerformUpdateAsync();

        _mockUpdateService.Verify(u => u.CheckForUpdateAsync(), Times.Once);
        _mockUpdateService.Verify(u => u.DownloadAndInstallAsync(It.IsAny<UpdateInfo>()), Times.Never);
    }
}
