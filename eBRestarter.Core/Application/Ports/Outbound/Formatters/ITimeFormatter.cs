namespace eBRestarter.Core.Application.Ports.Outbound.Formatters;

public interface ITimeFormatter
{
    string Format(int seconds);
}