using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ViewModels;
using eBRestarter.Desktop.WinUI3.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.DependencyInjections
{
    public static class ViewModelServiceExtensions
    {
        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<EBRestarter>();

            services.AddSingleton<ViewModelRestarterProperties>();

            services.AddSingleton<P_CommonOverview>();

            return services;
        }
    }
}
