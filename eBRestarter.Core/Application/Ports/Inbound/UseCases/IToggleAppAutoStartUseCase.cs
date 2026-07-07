namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

public interface IToggleAppAutoStartUseCase
{
    Task<bool> InitializeAndGetStateAsync();
    Task ToggleAsync(bool enable);
}

