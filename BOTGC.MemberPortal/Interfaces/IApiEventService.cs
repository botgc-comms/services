namespace BOTGC.MemberPortal.Interfaces;

public interface IApiEventService
{
    void TriggerDetector(string detectorName, int memberId, string? correlationId = null, int quietPeriodSeconds = 5);
}