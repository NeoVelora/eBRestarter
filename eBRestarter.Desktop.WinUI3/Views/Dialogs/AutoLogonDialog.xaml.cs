using eBRestarter.Core.Application.Models.Records;
using Microsoft.UI.Xaml.Controls;


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
