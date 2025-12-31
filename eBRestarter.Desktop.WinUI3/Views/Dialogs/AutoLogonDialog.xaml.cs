using eBRestarter.Core.Domain.Models.Records;
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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace eBRestarter.Desktop.WinUI3.Views.Dialogs
{
    public sealed partial class AutoLogonDialog : ContentDialog
    {
        public AutoLogonDialog()
        {
            this.InitializeComponent();
        }

        public void SetDefaults(string user, string domain)
        {
            if (!string.IsNullOrEmpty(user))
            {
                TxtUsername.Text = user;
            }

            if (!string.IsNullOrEmpty(domain))
            {
                TxtDomain.Text = domain;
            }
        }

        public AutoLogonCredentials GetCredentials()
        {
            return new AutoLogonCredentials(
                TxtUsername.Text,
                TxtDomain.Text,
                PbPassword.Password
            );
        }
    }
}
