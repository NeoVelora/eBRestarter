namespace eBRestarter.Core.Application.Interfaces;

public interface IOsEditionService
{
    Task<string> GetOsEditionAsync();
}
