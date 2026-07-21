using HRM.Components.Shared.Calendar;

namespace HRM.Services.Interfaces
{
    public interface ICalendarService
    {
        Task<List<CalendarEvent>> GetWorkspaceCalendar();

        Task<List<CalendarEvent>> GetOrganisationCalendar();

        Task<List<CalendarEvent>> GetTeamCalendar(int teamId);

        Task<List<CalendarEvent>> GetLeaveCalendar(int organisationId);

        Task<List<CalendarEvent>> GetStaffCalendar(int staffId);
    }
}
 