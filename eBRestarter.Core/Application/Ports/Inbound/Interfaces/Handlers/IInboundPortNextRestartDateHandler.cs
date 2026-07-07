namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Handlers;

public interface IInboundPortNextRestartDateHandler
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}

