using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Desktop.WinUI3.Models;

public sealed class AutoLogonDialogResult
{
    public bool IsDeactivateAction { get; set; }
    public bool DisablePasswordlessMode { get; set; }
    public bool RestorePasswordlessMode { get; set; }
    public AutoLogonCredentials? Credentials { get; set; }

    public static AutoLogonDialogResult Deactivate(bool restorePasswordlessMode = false) => new()
    {
        IsDeactivateAction = true,
        RestorePasswordlessMode = restorePasswordlessMode
    };

    public static AutoLogonDialogResult Save(AutoLogonCredentials creds, bool disablePasswordlessMode = false) => new()
    {
        IsDeactivateAction = false,
        Credentials = creds,
        DisablePasswordlessMode = disablePasswordlessMode
    };

    public static AutoLogonDialogResult? Cancel() => null;
}
