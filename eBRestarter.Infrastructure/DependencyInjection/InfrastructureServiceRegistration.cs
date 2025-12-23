using eBRestarter.Application.Facade;
using eBRestarter.Application.Facade.Interfaces;
using eBRestarter.Application.Services.Ports.Interfaces;
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

            services.AddTransient<IProcessWrapper, RealProcessWrapper>();

#pragma warning disable CA1416 // Plattformkompatibilität überprüfen
            services.AddSingleton<IProcessControlService, WindowsProcessService>();
            services.AddSingleton<ISystemInfoService, WindowsSystemInfoService>();
            services.AddSingleton<IStartupManagerService, WindowsStartupService>();
#pragma warning restore CA1416 // Plattformkompatibilität überprüfen

            services.AddSingleton<IOperatingSystemFacade, OperatingSystemFacade>();

            return services;
        }
    }
}
