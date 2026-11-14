using HRM.DTOs.Calendar;

namespace HRM.Services.Calendar.Contracts;

public interface IOrganisationCalendarResolver
{
    Task<EffectiveWorkdayResolutionDto> ResolveAsync(int organisationId, DateTime date, int? individualId = null, int? jobId = null);
    Task<Dictionary<DateTime, EffectiveWorkdayResolutionDto>> ResolveRangeAsync(int organisationId, DateTime from, DateTime to, int? individualId = null, int? jobId = null);
}
