using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Utility;

[SupportedOSPlatform("windows")]
public sealed partial class AdapterWindowsAuthenticodeVerifier : IOutboundPortExecutableSignatureVerifier
{

    private const string SignatureRejectedLogMessage = "Signature verification failed for {Path} (WinVerifyTrust returned 0x{Status}). The file is unsigned, tampered with, or its certificate chain is not trusted.";
    private const string VerificationErrorLogMessage = "Unexpected error while verifying the code signature of {Path}.";
    private const string WinTrustDllName = "wintrust.dll";
    private const string WinVerifyTrustEntryPoint = "WinVerifyTrust";

    private const uint RevokeWholeChain = 0x00000001;
    private const uint StateActionClose = 0x00000002;
    private const uint StateActionVerify = 0x00000001;
    private const uint TrustProviderFlags = 0x00000100;
    private const uint UiChoiceNone = 0x00000002;
    private const uint UiContextExecute = 0x00000000;
    private const uint UnionChoiceFile = 0x00000001;
    private const int VerificationSucceeded = 0;

    private static readonly Guid GenericVerifyV2ActionId = new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

    private static readonly IntPtr NoInteractiveWindowHandle = new(-1);


    private readonly ILogger<AdapterWindowsAuthenticodeVerifier> _logger;


    public AdapterWindowsAuthenticodeVerifier(ILogger<AdapterWindowsAuthenticodeVerifier> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }


    public bool IsTrustedPublisher(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            int verificationStatus = InvokeWinVerifyTrust(filePath);

            if (verificationStatus == VerificationSucceeded)
            {
                return true;
            }

            _logger.LogError(LogEventIds.Security.SignatureVerificationRejected, SignatureRejectedLogMessage, filePath, verificationStatus.ToString("X8"));

            return false;
        }
        catch (Exception exception)
        {
            _logger.LogError(LogEventIds.Security.SignatureVerificationErrored, exception, VerificationErrorLogMessage, filePath);

            return false;
        }
    }

    private static int InvokeWinVerifyTrust(string filePath)
    {
        IntPtr filePathBuffer = IntPtr.Zero;
        IntPtr fileInfoBuffer = IntPtr.Zero;
        IntPtr trustDataBuffer = IntPtr.Zero;

        var actionId = GenericVerifyV2ActionId;

        try
        {
            filePathBuffer = Marshal.StringToCoTaskMemUni(filePath);

            var fileInfo = new WinTrustFileInfo
            {
                StructSize = (uint)Marshal.SizeOf<WinTrustFileInfo>(),
                FilePath = filePathBuffer,
                FileHandle = IntPtr.Zero,
                KnownSubject = IntPtr.Zero
            };

            fileInfoBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf<WinTrustFileInfo>());
            Marshal.StructureToPtr(fileInfo, fileInfoBuffer, fDeleteOld: false);

            var trustData = new WinTrustData
            {
                StructSize = (uint)Marshal.SizeOf<WinTrustData>(),
                PolicyCallbackData = IntPtr.Zero,
                SipClientData = IntPtr.Zero,
                UIChoice = UiChoiceNone,
                RevocationChecks = RevokeWholeChain,
                UnionChoice = UnionChoiceFile,
                FileInfoPtr = fileInfoBuffer,
                StateAction = StateActionVerify,
                StateData = IntPtr.Zero,
                UrlReference = IntPtr.Zero,
                ProviderFlags = TrustProviderFlags,
                UIContext = UiContextExecute,
                SignatureSettings = IntPtr.Zero
            };

            trustDataBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf<WinTrustData>());
            Marshal.StructureToPtr(trustData, trustDataBuffer, fDeleteOld: false);

            int verificationStatus = WinVerifyTrust(NoInteractiveWindowHandle, ref actionId, trustDataBuffer);

            var stateToClose = Marshal.PtrToStructure<WinTrustData>(trustDataBuffer);
            stateToClose.StateAction = StateActionClose;
            Marshal.StructureToPtr(stateToClose, trustDataBuffer, fDeleteOld: false);

            _ = WinVerifyTrust(NoInteractiveWindowHandle, ref actionId, trustDataBuffer);

            return verificationStatus;
        }
        finally
        {
            if (trustDataBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(trustDataBuffer);
            }

            if (fileInfoBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(fileInfoBuffer);
            }

            if (filePathBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(filePathBuffer);
            }
        }
    }

    [LibraryImport(WinTrustDllName, EntryPoint = WinVerifyTrustEntryPoint)]
    private static partial int WinVerifyTrust(IntPtr hwnd, ref Guid actionId, IntPtr trustData);


    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustFileInfo
    {
        public uint StructSize;
        public IntPtr FilePath;
        public IntPtr FileHandle;
        public IntPtr KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustData
    {
        public uint StructSize;
        public IntPtr PolicyCallbackData;
        public IntPtr SipClientData;
        public uint UIChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr FileInfoPtr;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UIContext;
        public IntPtr SignatureSettings;
    }
}
