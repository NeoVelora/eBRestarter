using eBRestarter.Desktop.WinUI3.DependencyInjections;
using eBRestarter.Desktop.WinUI3.Services;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Display.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Services.Maps;


namespace eBRestarter.Desktop.WinUI3
{
    public partial class App : Application
    {
        public static Window? MainWindoweBRestarter { get; private set; }

        public static IHost? AppHost { get; private set; }

        public static Microsoft.UI.Dispatching.DispatcherQueue? AppDispatcherQueue { get; private set; }

        public App()
        {
            InitializeComponent();

            AppHost = CreateHostBuilder().Build();


            ApplicationLanguages.PrimaryLanguageOverride = "en-US";
        }

        private static IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder()
                 .ConfigureAppConfiguration((ctx, cfg) =>
                 {
                     cfg.Sources.Clear();
                     cfg.SetBasePath(AppContext.BaseDirectory);
                     cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                 })
                 .ConfigureServices((ctx, services) =>
                 {


                     ServiceRegistration.ConfigureServices(services);

                 });

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindoweBRestarter = AppHost!.Services.GetRequiredService<EBRestarter>();
            AppDispatcherQueue = MainWindoweBRestarter.DispatcherQueue;
            MainWindoweBRestarter.Activate();
        }
    }
}
