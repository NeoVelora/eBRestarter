using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Core.Domain.Models.Records;
using Microsoft.Extensions.Logging;
using System.Management;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Infrastructure.Services
{
    public class WindowsWmiHardwareService : IHardwareInfoService, IOsEditionService
    {
        private readonly ILogger<WindowsWmiHardwareService> _logger;

        public WindowsWmiHardwareService(ILogger<WindowsWmiHardwareService> logger)
        {
            _logger = logger;
        }

        public async Task<HardwareInfo> GetHardwareInfoAsync()
        {
            return await Task.Run(() =>
            {
                var cpu = GetWmiValue("Win32_Processor", "Name") ?? "N/A";
                var gpu = GetWmiValue("Win32_VideoController", "Name") ?? "N/A";
                var ramRaw = GetWmiValue("Win32_OperatingSystem", "TotalVisibleMemorySize");

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

        public async Task<string> GetOsEditionAsync()
        {
            return await Task.Run(() => GetWmiValue("Win32_OperatingSystem", "Caption") ?? "Windows (Unbekannt)");
        }

        private string? GetWmiValue(string wmiClass, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (var obj in searcher.Get())
                {
                    return obj[property]?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen von WMI Daten: {Class}.{Property}", wmiClass, property);
            }
            return null;
        }
    }
}
