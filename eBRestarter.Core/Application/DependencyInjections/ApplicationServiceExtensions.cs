using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.DependencyInjections
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IComputerRestartScheduler, ComputerRestartScheduler>();
            return services;
        }
    }
}
