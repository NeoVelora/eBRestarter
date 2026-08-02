using System;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Core.Application.UseCases;

public sealed class ToggleEdgeStartupBoostUseCase(
    IOutboundPortBrowserFactory browserFactory,
    IOutboundPortBrowserConfigRepository startupService) : IUseCaseToggleEdgeStartupBoost
{
    private readonly IOutboundPortBrowserFactory _browserFactory = browserFactory ?? throw new ArgumentNullException(nameof(browserFactory));
    private readonly IOutboundPortBrowserConfigRepository _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));


    public bool IsEnabled() => _startupService.IsBrowserStartupBoostEnabled();

    public bool IsEdgeInstalled() => _browserFactory.Create(BrowserType.Edge).IsInstalled;

    public ToggleEdgeStartupBoostResponse Toggle(bool shouldEnable)
    {
        try
        {
            _startupService.SetBrowserStartupBoost(shouldEnable);
            return new ToggleEdgeStartupBoostResponse(true, shouldEnable);
        }
        catch (Exception ex)
        {
            return new ToggleEdgeStartupBoostResponse(false, !shouldEnable, ex.Message);
        }
    }
}
