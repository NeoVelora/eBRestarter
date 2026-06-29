namespace eBRestarter.Core.Application.Ports.Outbound.Formatters;

public interface ITimeFormatterPort
{
    string Format(int seconds);
}