using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Enums;
using eBRestarter.Infrastructure.Browsers;
using eBRestarter.Infrastructure.Services.WindowsOS;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.XUnit.Test.IntegrationTests.Infrastructure
{
    public class BrowserDataGeneratorTests
    {
        [Theory]
        [InlineData(BrowserType.Brave, 10000)]
        [InlineData(BrowserType.Vivaldi, 10000)]
        public void GenerateBrowserDumpFiles_IntegrationTest(BrowserType browserType, int filesPerFolder)
        {
            var realFileSystemService = new WindowsFileSystemService();

            var osFacadeMock = new Mock<IOperatingSystemFacade>();
            osFacadeMock.Setup(os => os.WindowsFileSystemService).Returns(realFileSystemService);

            osFacadeMock.Setup(os => os.WindowsRegistryService).Returns(new WindowsRegistryService());

            IBrowser browser = CreateBrowserInstance(browserType, osFacadeMock.Object);

            var paths = browser.GetPaths();

            int sizeInKb = 3;

            int totalFilesCreated = 0;

            if (paths.CacheDirs != null)
                totalFilesCreated += GenerateDummyFilesInDirectories(paths.CacheDirs, filesPerFolder, sizeInKb);

            if (paths.CookiesDirs != null)
                totalFilesCreated += GenerateDummyFilesInDirectories(paths.CookiesDirs, filesPerFolder, sizeInKb);

            if (paths.ExtensionsDirs != null)
                totalFilesCreated += GenerateDummyFilesInDirectories(paths.ExtensionsDirs, filesPerFolder, sizeInKb);

            Assert.True(totalFilesCreated > 0, $"Es wurden keine Dateien für {browserType} erstellt. Ist der Browser installiert und wurde er mindestens einmal gestartet?");
        }


        private int GenerateDummyFilesInDirectories(IEnumerable<string> directories, int count, int sizeInKb)
        {
            int createdCount = 0;

            byte[] dummyContent = new byte[sizeInKb * 1024];
            new Random().NextBytes(dummyContent);

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                for (int i = 0; i < count; i++)
                {
                    string filePath = Path.Combine(dir, $"dummy_{Guid.NewGuid():N}.tmp");
                    File.WriteAllBytes(filePath, dummyContent);
                    createdCount++;
                }
            }

            return createdCount;
        }

        private IBrowser CreateBrowserInstance(BrowserType type, IOperatingSystemFacade osFacade)
        {
            return type switch
            {
                BrowserType.Chrome => new ChromeBrowser(osFacade, Mock.Of<ILogger<ChromeBrowser>>()),
                BrowserType.Firefox => new FirefoxBrowser(osFacade, Mock.Of<ILogger<FirefoxBrowser>>()),
                BrowserType.Edge => new EdgeBrowser(osFacade, Mock.Of<ILogger<EdgeBrowser>>()),
                BrowserType.Brave => new BraveBrowser(osFacade, Mock.Of<ILogger<BraveBrowser>>()),
                BrowserType.Vivaldi => new VivaldiBrowser(osFacade, Mock.Of<ILogger<VivaldiBrowser>>()),
                _ => throw new NotImplementedException($"Browser {type} wird nicht vom Test unterstützt.")
            };
        }
    }
}
