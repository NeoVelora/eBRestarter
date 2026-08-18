using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using eBRestarter.Core.Application.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.WindowsOS;

[SupportedOSPlatform("windows")]
public sealed partial class AdapterWindowsAutoLogonRepository : IOutboundPortOsAutoLogonRepository
{
    private const string ActiveStateText = "Active";
    private const string AutoAdminLogonDisabledValue = "0";
    private const string AutoAdminLogonEnabledValue = "1";
    private const string AutoAdminLogonKey = "AutoAdminLogon";
    private const string AutoLogonDisabledLogMessage = "AutoLogon disabled.";
    private const string AutoLogonEnabledLogMessage = "AutoLogon successfully enabled for user '{User}'.";
    private const string DefaultDomainNameKey = "DefaultDomainName";
    private const string DefaultPasswordKey = "DefaultPassword";
    private const string DefaultUserNameKey = "DefaultUserName";
    private const string DevicePasswordLessBuildVersionKey = "DevicePasswordLessBuildVersion";
    private const string DisableAutoLogonFailedExceptionMessage = "Failed to disable AutoLogon.";
    private const string EnableAutoLogonFailedExceptionMessage = "Failed to enable AutoLogon.";
    private const string ErrorReadingPasswordLessRegistryKeyLogMessage = "Error while reading the PasswordLess registry key.";
    private const string InactiveStateText = "Inactive";
    private const uint LsaPolicyAccessRights = 0x00000020 | 0x00000800;
    private const int PasswordLessActiveValue = 2;
    private const string PasswordLessDevicePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";
    private const int PasswordLessInactiveValue = 0;
    private const string PasswordLessKeyNotFoundWarningLogMessage = "Registry key for PasswordLess Device not found. Skipping recreation since this is system-specific.";
    private const string PasswordLessModeChangedLogMessage = "Windows Hello Passwordless Mode changed to {State} ({Value}).";
    private const string PasswordLessPermissionExceptionMessage = "Insufficient permissions to modify the PasswordLess registry key. The application must be run as Administrator.";
    private const string PasswordLessUnexpectedExceptionMessage = "An unexpected error occurred while writing the PasswordLess registry key.";
    private const string WinLogonKeyNotFoundExceptionMessage = "Winlogon Registry Key not found.";
    private const string WinLogonPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

    private readonly ILogger<AdapterWindowsAutoLogonRepository> _logger;


    public AdapterWindowsAutoLogonRepository(ILogger<AdapterWindowsAutoLogonRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }


    public void DisableAutoLogon()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinLogonPath, true)
                ?? throw new InvalidOperationException(WinLogonKeyNotFoundExceptionMessage);

            key.SetValue(AutoAdminLogonKey, AutoAdminLogonDisabledValue, RegistryValueKind.String);

            SetLsaSecret(DefaultPasswordKey, null);

            _logger.LogInformation(LogEventIds.Security.AutoLogonDisabled, AutoLogonDisabledLogMessage);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(DisableAutoLogonFailedExceptionMessage, ex);
        }
    }

    public void EnableAutoLogon(string username, string domain, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(WinLogonPath, true)
                ?? throw new InvalidOperationException(WinLogonKeyNotFoundExceptionMessage);

            key.SetValue(AutoAdminLogonKey, AutoAdminLogonEnabledValue, RegistryValueKind.String);
            key.SetValue(DefaultUserNameKey, username, RegistryValueKind.String);

            if (string.IsNullOrWhiteSpace(domain))
            {
                key.DeleteValue(DefaultDomainNameKey, false);
            }
            else
            {
                key.SetValue(DefaultDomainNameKey, domain, RegistryValueKind.String);
            }

            key.DeleteValue(DefaultPasswordKey, false);

            SetLsaSecret(DefaultPasswordKey, password);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(LogEventIds.Security.AutoLogonEnabled, AutoLogonEnabledLogMessage, LogRedaction.MaskIdentifier(username));
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(EnableAutoLogonFailedExceptionMessage, ex);
        }
    }

    public bool IsAutoLogonEnabled()
    {
        using var key = Registry.LocalMachine.OpenSubKey(WinLogonPath, false);

        var val = key?.GetValue(AutoAdminLogonKey) as string;

        return val == AutoAdminLogonEnabledValue;
    }

    public bool IsPasswordlessAuthEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(PasswordLessDevicePath, false);

            if (key?.GetValue(DevicePasswordLessBuildVersionKey) is PasswordLessActiveValue)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventIds.Security.PasswordlessReadFailed, ex, ErrorReadingPasswordLessRegistryKeyLogMessage);
        }

        return false;
    }

    public void SetPasswordlessAuth(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(PasswordLessDevicePath, true);

            if (key is null)
            {
                _logger.LogWarning(LogEventIds.Security.PasswordlessRegistryKeyMissing, PasswordLessKeyNotFoundWarningLogMessage);
            }
            else
            {
                int valueToSet = enable ? PasswordLessActiveValue : PasswordLessInactiveValue;
                key.SetValue(DevicePasswordLessBuildVersionKey, valueToSet, RegistryValueKind.DWord);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(LogEventIds.Security.PasswordlessModeChanged, PasswordLessModeChangedLogMessage, enable ? ActiveStateText : InactiveStateText, valueToSet);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException(PasswordLessPermissionExceptionMessage, ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(PasswordLessUnexpectedExceptionMessage, ex);
        }
    }

    private static void FreeLsaString(LsaUnicodeString lus)
    {
        if (lus.Buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(lus.Buffer);
        }
    }

    private static LsaUnicodeString InitLsaString(string s)
    {
        return new LsaUnicodeString
        {
            Length = (ushort)(s.Length * 2),
            MaximumLength = (ushort)((s.Length + 1) * 2),
            Buffer = Marshal.StringToHGlobalUni(s)
        };
    }

    [LibraryImport("advapi32.dll")]
    private static partial uint LsaClose(IntPtr ObjectHandle);

    [LibraryImport("advapi32.dll")]
    private static partial uint LsaNtStatusToWinError(uint status);

    [LibraryImport("advapi32.dll")]
    private static partial uint LsaOpenPolicy(ref IntPtr SystemName, ref LsaObjectAttributes ObjectAttributes, uint DesiredAccess, out IntPtr PolicyHandle);

    [LibraryImport("advapi32.dll", EntryPoint = "LsaStorePrivateData")]
    private static partial uint LsaStorePrivateData(IntPtr PolicyHandle, ref LsaUnicodeString KeyName, ref LsaUnicodeString PrivateData);

    [LibraryImport("advapi32.dll", EntryPoint = "LsaStorePrivateData")]
    private static partial uint LsaStorePrivateData(IntPtr PolicyHandle, ref LsaUnicodeString KeyName, IntPtr PrivateData);

    private static void SetLsaSecret(string keyName, string? value)
    {
        LsaUnicodeString secretKey = default;
        LsaUnicodeString secretValue = default;
        IntPtr lsaPolicyHandle = IntPtr.Zero;

        try
        {
            secretKey = InitLsaString(keyName);
            if (value is not null)
            {
                secretValue = InitLsaString(value);
            }

            var objectAttributes = new LsaObjectAttributes();
            var localsystem = IntPtr.Zero;

            var result = LsaOpenPolicy(ref localsystem, ref objectAttributes, LsaPolicyAccessRights, out lsaPolicyHandle);
            if (result != 0)
            {
                uint winError = LsaNtStatusToWinError(result);
                throw new Win32Exception((int)winError, $"LsaOpenPolicy failed with NTSTATUS 0x{result:X8}.");
            }

            if (value is null)
            {
                result = LsaStorePrivateData(lsaPolicyHandle, ref secretKey, IntPtr.Zero);
            }
            else
            {
                result = LsaStorePrivateData(lsaPolicyHandle, ref secretKey, ref secretValue);
            }

            if (result != 0)
            {
                uint winError = LsaNtStatusToWinError(result);
                throw new Win32Exception((int)winError, $"LsaStorePrivateData failed with NTSTATUS 0x{result:X8}.");
            }
        }
        finally
        {
            if (lsaPolicyHandle != IntPtr.Zero)
            {
                _ = LsaClose(lsaPolicyHandle);
            }

            FreeLsaString(secretKey);
            FreeLsaString(secretValue);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct LsaUnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LsaObjectAttributes
    {
        public int Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }
}