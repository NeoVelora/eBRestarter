using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseConfigureAutoLogon
{
    Result<AutoLogonResultStatus> Execute(ConfigureAutoLogonRequest request);
}

