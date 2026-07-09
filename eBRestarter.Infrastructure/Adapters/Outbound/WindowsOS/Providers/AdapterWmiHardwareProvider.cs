using eBRestarter.Core.Application.Extensions;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using Microsoft.Extensions.Logging;
using System.Management;

namespace eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Providers;

public sealed class AdapterWmiHardwareProvider(ILogger<AdapterWmiHardwareProvider> logger) : IOutboundPortHardwareInfoProvider, IOutboundPortOsEditionProvider
{
    private readonly ILogger<AdapterWmiHardwareProvider> _logger = logger;

    public async Task<HardwareInfo> RetrieveHardwareInfoAsync()
    {
        return await Task.Run(() =>
        {
            var cpu = RetrieveWmiValue("Win32_Processor", "Name") ?? "N/A";
            var gpu = RetrieveWmiValue("Win32_VideoController", "Name") ?? "N/A";
            var ramRaw = RetrieveWmiValue("Win32_OperatingSystem", "TotalVisibleMemorySize");

            string ramFormatted = "N/A";

            if (long.TryParse(ramRaw, out long ramKb))
            {
                ramFormatted = (ramKb * 1024).ToSizeSuffix();
            }

            return new HardwareInfo
            {
                ProcessorName = cpu,
                GraphicsCardName = gpu,
                InstalledRam = ramFormatted
            };
        });
    }

    public async Task<string> RetrieveOsEditionAsync()
    {
        return await Task.Run(() => RetrieveWmiValue("Win32_OperatingSystem", "Caption") ?? "Windows (Unkwone)");
    }

    private string? RetrieveWmiValue(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            using var collection = searcher.Get();
            using var enumerator = collection.GetEnumerator();

            if (enumerator.MoveNext())
            {
                return enumerator.Current[property]?.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving WMI data: {Class}.{Property}", wmiClass, property);
        }
        return null;
    }
}


