using System;

using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Handlers;
using eBRestarter.Core.Domain.Services;

namespace eBRestarter.Core.Application.BehavioralComponents.Handlers;

public sealed class NextRestartDateHandler(IRestartCalculationService restartCalculationService) : IInboundPortNextRestartDateHandler
{
    private readonly IRestartCalculationService _restartCalculationService = restartCalculationService ?? throw new ArgumentNullException(nameof(restartCalculationService));

    public DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime) =>
        _restartCalculationService.RetrieveNextRestartDate(intervalDays, restartClockTime);
}
