namespace eBRestarter.Core.Application.Ports.Inbound.Handlers;

public interface IInboundPortNextRestartDateHandler
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}

