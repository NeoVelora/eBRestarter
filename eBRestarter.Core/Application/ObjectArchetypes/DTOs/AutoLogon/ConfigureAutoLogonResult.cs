namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.AutoLogon;

#pragma warning disable S2094

public sealed record AutoLogonActivated;

public sealed record AutoLogonDeactivated;

public sealed record AutoLogonWindowsHelloBlockActive;

public sealed record AutoLogonAdminRequired(string Message);

public sealed record AutoLogonValidationError(string Message);

public sealed record AutoLogonDomainError(string Message);

public sealed record AutoLogonUnexpectedError(string Message);

public union ConfigureAutoLogonResult(
    AutoLogonActivated,
    AutoLogonDeactivated,
    AutoLogonWindowsHelloBlockActive,
    AutoLogonAdminRequired,
    AutoLogonValidationError,
    AutoLogonDomainError,
    AutoLogonUnexpectedError);
