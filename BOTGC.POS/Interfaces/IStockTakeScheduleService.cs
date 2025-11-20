namespace BOTGC.POS.Services;

public interface IStockTakeScheduleService
{
    bool IsStockTakeDue(DateTimeOffset now);
}