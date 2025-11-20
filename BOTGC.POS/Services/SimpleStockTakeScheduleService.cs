namespace BOTGC.POS.Services;

public sealed class SimpleStockTakeScheduleService : IStockTakeScheduleService
{
    public bool IsStockTakeDue(DateTimeOffset now)
    {
        var local = now.ToLocalTime().DayOfWeek;
        return local is DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Saturday;
    }
}