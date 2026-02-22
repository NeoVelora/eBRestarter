namespace eBRestarter.Core.Domain.Models.Records.Config;

public record Computer
{
    public DateTime? NextRestartDate { get; set; }
    public int ComputerRestartIntervalDays { get; set; } = 0;
    public int RestartClockTime { get; set; } = 0;
}
