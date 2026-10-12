using System;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Core.Application.BehavioralComponents.Providers;

public sealed class SystemInformationProvider(
    IOutboundPortHardwareInfoProvider hardwareInfoProvider,
    IOutboundPortOsEditionProvider osEditionProvider,
    IOutboundPortSystemInfoProvider systemInfoProvider) : IInboundPortSystemInformationProvider
{
    private readonly IOutboundPortHardwareInfoProvider _hardwareInfoProvider = hardwareInfoProvider ?? throw new ArgumentNullException(nameof(hardwareInfoProvider));
    private readonly IOutboundPortOsEditionProvider _osEditionProvider = osEditionProvider ?? throw new ArgumentNullException(nameof(osEditionProvider));
    private readonly IOutboundPortSystemInfoProvider _systemInfoProvider = systemInfoProvider ?? throw new ArgumentNullException(nameof(systemInfoProvider));

    public async Task<SystemInformationResponse> RetrieveAsync()
    {
        var hardwareTask = _hardwareInfoProvider.RetrieveHardwareInfoAsync();
        var editionTask = _osEditionProvider.RetrieveOsEditionAsync();

        await Task.WhenAll(hardwareTask, editionTask).ConfigureAwait(false);

        var hardware = await hardwareTask.ConfigureAwait(false);
        var edition = await editionTask.ConfigureAwait(false);

        var osDisplayVersion = _systemInfoProvider.RetrieveCurrentOsDisplayVersion();
        var osBuildVersion = _systemInfoProvider.RetrieveCurrentOsBuildVersion();
        var browserName = _systemInfoProvider.RetrieveCurrentStandardBrowserName();

        return new SystemInformationResponse(
            ProcessorName: hardware.ProcessorName,
            GraphicsCardName: hardware.GraphicsCardName,
            InstalledRam: hardware.InstalledRam,
            OsEdition: edition,
            OsDisplayVersion: osDisplayVersion,
            OsBuildVersion: osBuildVersion,
            StandardBrowserName: browserName);
    }
}