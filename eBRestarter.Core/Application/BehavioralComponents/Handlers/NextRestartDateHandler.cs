using System;

using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Handlers;
using eBRestarter.Core.Domain.Handlers;

namespace eBRestarter.Core.Application.BehavioralComponents.Handlers;

public sealed class NextRestartDateHandler(IRestartCalculationHandler restartCalculationHandler) : IInboundPortNextRestartDateHandler
{
    private readonly IRestartCalculationHandler _restartCalculationHandler = restartCalculationHandler ?? throw new ArgumentNullException(nameof(restartCalculationHandler));


    public DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime) =>
        _restartCalculationHandler.RetrieveNextRestartDate(intervalDays, restartClockTime);
}
