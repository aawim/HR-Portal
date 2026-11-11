using HRM.DTOs.Calendar;
using HRM.DTOs.Leave;

namespace HRM.Services.Calendar.Contracts;

public abstract class OrganisationCalendarServiceContracts
{
    public interface IOperations
    {
        Task<EffectiveWorkdayResolutionDto> ResolveEffectiveWorkdayAsync(DateTime date, int? individualId = null);
        Task<List<OrganisationCalendarDaySummaryDto>> GetOrganisationCalendarMonthAsync(DateTime month);
        Task<OrganisationCalendarDaySummaryDto> GetCalendarDayDetailAsync(DateTime date);
    }
}

public abstract class SupervisorCalendarServiceContracts
{
    public interface IOperations
    {
        Task<List<SupervisorTeamOptionDto>> GetSupervisedTeamsAsync();
        Task<List<SupervisorCalendarScheduleItemDto>> GetTeamSchedulesAsync(DateTime from, DateTime to, int? teamId, int? staffId);
        Task<List<SupervisorCalendarTaskDto>> GetTasksAsync(DateTime from, DateTime to, int? teamId);
        Task<int> SaveTaskAsync(SupervisorCalendarTaskDto task);
        Task UpdateTaskStatusAsync(int taskId, string status);
        Task DeleteTaskAsync(int taskId);
    }
}

public abstract class CalendarImpactServiceContracts
{
    public interface IOperations
    {
        Task<List<LeaveImpactAssessmentDto>> GetAssessmentsForExceptionAsync(int exceptionId);
        Task AssessImpactForExceptionAsync(int exceptionId);
        Task ActOnAssessmentAsync(int assessmentId, string action); // e.g. "Reviewed", "Dismissed"
    }
}
