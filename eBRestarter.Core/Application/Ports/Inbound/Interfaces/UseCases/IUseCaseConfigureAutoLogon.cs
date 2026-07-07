using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Common.Results;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseConfigureAutoLogon
{
    Result<AutoLogonResultStatus> Execute(ConfigureAutoLogonRequest request);
}

