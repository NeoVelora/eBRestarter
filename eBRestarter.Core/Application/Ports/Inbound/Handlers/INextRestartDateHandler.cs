namespace eBRestarter.Core.Application.Ports.Inbound.Handlers;

public interface INextRestartDateHandler
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}

