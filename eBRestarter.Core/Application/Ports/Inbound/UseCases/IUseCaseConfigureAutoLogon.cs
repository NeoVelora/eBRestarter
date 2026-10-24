using eBRestarter.Core.Application.ObjectArchetypes.DTOs.AutoLogon;

namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

public interface IUseCaseConfigureAutoLogon
{
    ConfigureAutoLogonResult Execute(ConfigureAutoLogonRequest request);
}

