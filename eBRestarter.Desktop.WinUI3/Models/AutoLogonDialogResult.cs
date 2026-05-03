using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Desktop.WinUI3.Models;

public sealed class AutoLogonDialogResult
{
    public bool IsDeactivateAction { get; set; }
    public AutoLogonCredentials? Credentials { get; set; }

    public static AutoLogonDialogResult Deactivate() => new() { IsDeactivateAction = true };
    public static AutoLogonDialogResult Save(AutoLogonCredentials creds) => new() { IsDeactivateAction = false, Credentials = creds };
    public static AutoLogonDialogResult? Cancel() => null;
}
