using eBRestarter.Core.Application.Facade;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Infrastructure.Browsers;
using eBRestarter.Infrastructure.Factories;
using eBRestarter.Infrastructure.Services.Config;
using eBRestarter.Infrastructure.Services.RestSharp;
using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Infrastructure.Wrapper;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {

                builder.AddDebug();
            });

#pragma warning disable CA1416 // Plattformkompatibilität überprüfen

            services.AddTransient<IProcessWrapper, RealProcessWrapper>();
            services.AddSingleton<IWindowsProcessControlService, WindowsProcessService>();
            services.AddSingleton<IWindowsSystemInfoService, WindowsSystemInfoService>();

            services.AddTransient<IProcessInfoService, ProcessInfoService>();
            services.AddTransient<IWindowsRegistryService, WindowsRegistryService>();
            services.AddTransient<IWindowsFileSystemService, WindowsFileSystemService>();
            services.AddSingleton<IWindowsStartupManagerService, WindowsStartupService>();
#pragma warning restore CA1416 // Plattformkompatibilität überprüfen

            services.AddSingleton<IOperatingSystemFacade, OperatingSystemFacade>();

            services.AddTransient<ChromeBrowser>();
            services.AddTransient<FirefoxBrowser>();
            services.AddTransient<EdgeBrowser>();
            services.AddTransient<BraveBrowser>();

            services.AddSingleton<IBrowserFactory, BrowserFactory>();

            services.AddTransient<IRestClientService, RestSharpClientService>();

            services.AddSingleton<IPathService, WindowsPathService>();
            services.AddSingleton<IEVisitorConfigService, EVRestarterConfigService>();

            return services;
        }
    }
}
