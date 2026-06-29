namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface INextRestartDateProvider
{
    DateTime RetrieveNextRestartDate(int intervalDays, int restartClockTime);
}

