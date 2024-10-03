using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace eBRestarter.Classes.InternetBrowser
{
    public sealed partial class Chrome : Browser, IChromiumCommonalities
    {
        private readonly string _chromeLocalFilePath                 = @Path.GetPathRoot(Environment.SystemDirectory) + "\\Users\\" + Environment.UserName + "\\AppData\\Local\\Google\\Chrome\\User Data";

        private readonly string _standardWindowsChromeBrowserPathx64 = @Path.GetPathRoot(Environment.SystemDirectory) + "\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
        private readonly string _standardWindowsChromeBrowserPathx86 = @Path.GetPathRoot(Environment.SystemDirectory) + "\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";

        private const string CHROME_VERSION                         = @"HKEY_CURRENT_USER\SOFTWARE\Google\Chrome\BLBeacon";
        private const string CHROME_PROCESS                         = "chrome.exe";

        private const string EBESUCHER_ADD_ON_NAME_FOR_CHROME       = "\\agchmcconfdfcenopioeilpgjngelefk";

        private string DefaultDirectoryChrome           { get => _chromeLocalFilePath + IChromiumCommonalities.DEFAULTSECTIONS; }
        public override string InternetCacheDirectory   { get => DefaultDirectoryChrome + IChromiumCommonalities.INTERNET_CACHE_SECTIONS; }
        public override string CookiesPathDirectory     { get => DefaultDirectoryChrome + IChromiumCommonalities.COOKIE_SECTIONS; }
        public override string ExtensionsDirectory      { get => DefaultDirectoryChrome + IChromiumCommonalities.EXTENSIONS + EBESUCHER_ADD_ON_NAME_FOR_CHROME; }

        public override List<string> InternetCacheAndCookiesDirectoryList
        {
            get
            {
                var directoryListInternetCacheAndCookies = new List<string>();

                directoryListInternetCacheAndCookies!.AddRange(InternetCacheDirectoryList);
                directoryListInternetCacheAndCookies!.AddRange(InternetCookies);

                return directoryListInternetCacheAndCookies;
            }
        }

        public override List<string> InternetCacheDirectoryList
        {
            get
            {
                var directoryListInternetCacheAndCookies = new List<string>();

                directoryListInternetCacheAndCookies!.Add(DefaultDirectoryChrome + IChromiumCommonalities.INTERNET_CACHE_SERVICE_WORKER);
                directoryListInternetCacheAndCookies!.Add(DefaultDirectoryChrome + IChromiumCommonalities.INTERNET_CACHE_SECTIONS);

                return directoryListInternetCacheAndCookies;
            }
        }


        public override List<string> InternetCookies
        {
            get
            {
                var directoryListInternetCacheAndCookies = new List<string>();

                directoryListInternetCacheAndCookies!.Add(DefaultDirectoryChrome + IChromiumCommonalities.COOKIE_SECTIONS);
                directoryListInternetCacheAndCookies!.Add(DefaultDirectoryChrome + IChromiumCommonalities.COOKIES);

                return directoryListInternetCacheAndCookies;
            }
        }

        [GeneratedRegex(@"[^0-9-.]")]
        private static partial Regex MyRegex();

        public Chrome() { }

        public Chrome(string websiteLink) : base(websiteLink) { }

        public Chrome(string websiteLink, string addedLinkComponent) : base(websiteLink, addedLinkComponent) { }

        public override void StartBrowser()
        {
            try
            {
                #pragma warning disable CA1416
                Windows!.Process = new Process();
                Windows.Process.StartInfo.FileName = CHROME_PROCESS;
                Windows.Process.StartInfo.UseShellExecute = true;
                Windows.Process.StartInfo.Arguments = WebsiteLink + AddedLinkComponent;
                Windows.Process.Start();
            }
            catch (Exception e){

                Log.Error(e, "Es ist eine Ausnahme im Chromebereich aufgetreten. Logging...");

            }
        }

        public override void CloseBrowser()
        {
            Windows!.CloseApplication("chrome");
        }

        public override string BrowserVersion
        {
            get {

                string? CVersionBrowserRegex = Windows!.GetCurrentBrowserVersion(CHROME_VERSION, "version", "");

                if (CVersionBrowserRegex != null)
                {
                    try
                    {
                        string CVersionBrowserRegex2 = MyRegex().Replace(CVersionBrowserRegex, "");

                        return CVersionBrowserRegex2[..];
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        Log.Error(e, "Es ist eine Ausnahme im Chromebereich aufgetreten. Logging...");

                        return null!;
                    }
                }
                else
                {
                    return null!;
                }
            }          
        }


        public override bool BrowserExist
        {
            get {
                if ((File.Exists(_standardWindowsChromeBrowserPathx64)) == true && (!eBRestarter.Classes.OperatingSystem.WindowsOS.IsDirectory(_standardWindowsChromeBrowserPathx64))
                 && (File.Exists(_standardWindowsChromeBrowserPathx86)) == true && (!eBRestarter.Classes.OperatingSystem.WindowsOS.IsDirectory(_standardWindowsChromeBrowserPathx86)))
                {
                    return true;
                }
                else if (File.Exists(_standardWindowsChromeBrowserPathx64) == true && (!eBRestarter.Classes.OperatingSystem.WindowsOS.IsDirectory(_standardWindowsChromeBrowserPathx64)))
                {
                    return true;
                }

                else if (File.Exists(_standardWindowsChromeBrowserPathx86) == true && (!eBRestarter.Classes.OperatingSystem.WindowsOS.IsDirectory(_standardWindowsChromeBrowserPathx86)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }          
        }

        public override bool ExtensionsExist
        {
            get
            {
                if (Directory.Exists(ExtensionsDirectory))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

    }
}
