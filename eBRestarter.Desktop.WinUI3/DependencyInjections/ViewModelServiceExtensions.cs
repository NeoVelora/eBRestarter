using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ViewModels;
using eBRestarter.Desktop.WinUI3.Views.Pages;
using eBRestarter.Desktop.WinUI3.Views.UserControls;
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

            services.AddSingleton<InstalledBrowsersViewModel>();
            services.AddSingleton<UC_InstalledBrowsers>();

            services.AddSingleton<ViewModelOptions>();
            services.AddSingleton<UC_Options>();

            services.AddSingleton<ViewModelInfocenter>();
            services.AddSingleton<UC_Infocenter>();

            services.AddSingleton<ViewModelAbout>();

            services.AddTransient<NetworkTrafficViewModel>();
            services.AddTransient<ViewModelDeleteBrowserContent>();

            return services;
        }
    }
}
