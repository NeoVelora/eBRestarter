using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS.Authentication;

public sealed class AdapterWindowsCredentialValidationProvider(
    IOutboundPortActiveDirectoryProvider adProviderPort,
    ILogger<AdapterWindowsCredentialValidationProvider> logger) : IOutboundPortCredentialValidationProvider
{

    private const string CredentialValidationErrorLogMessage = "Credential validation for user {User} in scope {Scope} failed with an unexpected error; treating the credentials as invalid.";
    private const string CredentialValidationRejectedLogMessage = "Credential validation REJECTED for user {User} in scope {Scope}.";
    private const string PrincipalServerDownMessage = "PrincipalServerDown";


    private readonly IOutboundPortActiveDirectoryProvider _adProviderPort = adProviderPort ?? throw new ArgumentNullException(nameof(adProviderPort));
    private readonly ILogger<AdapterWindowsCredentialValidationProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    public bool ValidateCredentials(string username, string domain, string password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        var contextScope = !string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            ? DirectoryContextScope.Domain
            : DirectoryContextScope.Machine;

        try
        {
            bool isValid = _adProviderPort.ValidateCredentials(contextScope, domain, username, password);

            if (!isValid)
            {
                _logger.LogWarning(LogEventIds.Security.CredentialValidationRejected, CredentialValidationRejectedLogMessage, LogRedaction.MaskIdentifier(username), contextScope);
            }

            return isValid;
        }
        catch (PrincipalServerDownException exception)
        {
            throw new InvalidOperationException(PrincipalServerDownMessage, exception);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(LogEventIds.Security.CredentialValidationErrored, exception, CredentialValidationErrorLogMessage, LogRedaction.MaskIdentifier(username), contextScope);

            return false;
        }
    }
}
