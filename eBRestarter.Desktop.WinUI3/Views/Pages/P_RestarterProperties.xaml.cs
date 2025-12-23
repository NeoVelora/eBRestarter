using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace eBRestarter.Desktop.WinUI3.Views.Pages
{
    public sealed partial class P_RestarterProperties : Page
    {
        public ViewModelRestarterProperties ViewModelRestarterProperties { get; }

        public P_RestarterProperties()
        {
            this.InitializeComponent();

            ViewModelRestarterProperties = App.AppHost.Services.GetRequiredService<ViewModelRestarterProperties>();

            this.DataContext = ViewModelRestarterProperties;
        }
    }
}
