using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseConfigureAutoLogon
{
    Result<AutoLogonResultStatus> Execute(ConfigureAutoLogonRequest request);
}

