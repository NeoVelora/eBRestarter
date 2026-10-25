using Microsoft.Extensions.Logging;
using System;
using System.Management;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using eBRestarter.Core.Application.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.WindowsOS;

[SupportedOSPlatform("windows")]
public sealed class AdapterWmiHardwareProvider : IOutboundPortHardwareInfoProvider, IOutboundPortOsEditionProvider
{
    private const long BytesPerKilobyte = 1024;
    private const string ErrorRetrievingWmiDataLogMessage = "Error retrieving WMI data: {Class}.{Property}";
    private const string NotAvailableFallbackText = "N/A";
    private const string UnknownOsEditionFallbackText = "Windows (Unknown)";
    private const string WmiClassOperatingSystem = "Win32_OperatingSystem";
    private const string WmiClassProcessor = "Win32_Processor";
    private const string WmiClassVideoController = "Win32_VideoController";
    private const string WmiPropertyCaption = "Caption";
    private const string WmiPropertyName = "Name";
    private const string WmiPropertyTotalVisibleMemorySize = "TotalVisibleMemorySize";

    private readonly ILogger<AdapterWmiHardwareProvider> _logger;


    public AdapterWmiHardwareProvider(ILogger<AdapterWmiHardwareProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }


    public async Task<HardwareInfo> RetrieveHardwareInfoAsync()
    {
        return await Task.Run(() =>
        {
            var cpu = RetrieveWmiValue(WmiClassProcessor, WmiPropertyName) ?? NotAvailableFallbackText;
            var gpu = RetrieveWmiValue(WmiClassVideoController, WmiPropertyName) ?? NotAvailableFallbackText;
            var ramRaw = RetrieveWmiValue(WmiClassOperatingSystem, WmiPropertyTotalVisibleMemorySize);

            string ramFormatted = NotAvailableFallbackText;

            if (long.TryParse(ramRaw, out long ramKb))
            {
                ramFormatted = (ramKb * BytesPerKilobyte).ToSizeSuffix();
            }

            return new HardwareInfo
            {
                ProcessorName = cpu,
                GraphicsCardName = gpu,
                InstalledRam = ramFormatted
            };
        }).ConfigureAwait(false);
    }

    public async Task<string> RetrieveOsEditionAsync()
    {
        return await Task.Run(() => RetrieveWmiValue(WmiClassOperatingSystem, WmiPropertyCaption) ?? UnknownOsEditionFallbackText).ConfigureAwait(false);
    }

    private string? RetrieveWmiValue(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            using var collection = searcher.Get();

            using var enumerator = collection.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                return null;
            }

            using var managementObject = enumerator.Current;

            return managementObject[property]?.ToString();
        }
        catch (Exception exception)
        {
            _logger.LogError(LogEventIds.OperatingSystem.WmiQueryFailed, exception, ErrorRetrievingWmiDataLogMessage, wmiClass, property);
        }

        return null;
    }
}
