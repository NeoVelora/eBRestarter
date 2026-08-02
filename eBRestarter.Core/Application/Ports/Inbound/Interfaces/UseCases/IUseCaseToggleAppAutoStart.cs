namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseToggleAppAutoStart
{
    Task<bool> InitializeAndGetStateAsync();
    Task ToggleAsync(bool shouldEnable);
}
