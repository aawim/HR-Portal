using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class AttendanceLogDataLoader : IAttendanceLogDataLoader
    {
        private readonly IDbContextFactory<HrmTeContext> _dbfactory;

        public AttendanceLogDataLoader(IDbContextFactory<HrmTeContext> dbfactory)
        {
            _dbfactory = dbfactory;
        }

        public async Task<AttendanceLogDto?> GetEarliestCheckInAsync(int individualId, DateTime date)
        {
            using var db = await _dbfactory.CreateDbContextAsync();
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var hasOfficialSchedule = await db.WorkAssignments
                .AsNoTracking()
                .AnyAsync(a =>
                    a.IsValid &&
                    a.CancelledDate == null &&
                    a.SchedulePurpose == "Official" &&
                    a.WorkPlan.IsValid &&
                    a.WorkPlan.IndividualId == individualId &&
                    a.WorkPlan.WorkDate == dayStart);

            // Use resolved Official check-in when Official schedule exists; never mistake a mid-day Duty check-in for Official check-in
            return await db.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualId == individualId &&
                    x.AttendanceLogStateId == 1 &&
                    x.Date >= dayStart &&
                    x.Date < dayEnd &&
                    (x.AttendanceLogResolutions.Any(r =>
                        r.IsValid &&
                        r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved &&
                        r.AttendanceClockTypeId == (int)HRM.Enum.AttendanceClockType.CheckIn &&
                        (!hasOfficialSchedule ||
                         (r.SchedulePurpose != "Duty" &&
                          (r.WorkAssignment == null || r.WorkAssignment.SchedulePurpose != "Duty") &&
                          (!r.WorkPlanSegmentId.HasValue || !db.WorkPlanSegments.Any(s => s.WorkPlanSegmentId == r.WorkPlanSegmentId.Value && s.SchedulePurpose == "Duty"))))) ||
                     (!x.AttendanceLogResolutions.Any(r => r.IsValid) && x.InOutModeId == 1)))
                .OrderBy(x => x.Date)
                .Select(x => new AttendanceLogDto
                {
                    AttendanceLogID = x.AttendanceLogId,
                    AttendanceDeviceID = x.AttendanceDeviceId,
                    IndividualID = x.IndividualId,
                    OrganisationID = x.OrganisationId,
                    OrganisationStructureID = x.OrganisationStructureId,
                    InOutModeID = x.InOutModeId,
                    Year = x.Year,
                    Month = x.Month,
                    Day = x.Day,
                    Hour = x.Hour,
                    Minute = x.Minute,
                    Second = x.Second,
                    Date = x.Date,
                    AttendanceLogModeID = x.AttendanceLogModeId,
                    AttendanceLogStateID = x.AttendanceLogStateId,
                    OperationLogID = x.OperationLogId,
                    RelatedAttendanceLogID = x.RelatedAttendanceLogId,
                    ActualInOutMode = x.ActualInOutMode,
                    ResolvedClockType = HRM.Enum.AttendanceClockType.CheckIn
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AttendanceLogDto?> GetLatestCheckOutAsync(int individualId, DateTime date)
        {
            using var db = await _dbfactory.CreateDbContextAsync();
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var hasOfficialSchedule = await db.WorkAssignments
                .AsNoTracking()
                .AnyAsync(a =>
                    a.IsValid &&
                    a.CancelledDate == null &&
                    a.SchedulePurpose == "Official" &&
                    a.WorkPlan.IsValid &&
                    a.WorkPlan.IndividualId == individualId &&
                    a.WorkPlan.WorkDate == dayStart);

            // Use resolved Official check-out when Official schedule exists; never mistake a Duty check-out (e.g. 12:30) for the day's Official check-out
            return await db.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualId == individualId &&
                    x.AttendanceLogStateId == 1 &&
                    x.Date >= dayStart &&
                    x.Date < dayEnd &&
                    (x.AttendanceLogResolutions.Any(r =>
                        r.IsValid &&
                        r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved &&
                        r.AttendanceClockTypeId == (int)HRM.Enum.AttendanceClockType.CheckOut &&
                        r.LogicalEventType != "OfficialBreakOut" &&
                        (!hasOfficialSchedule ||
                         (r.SchedulePurpose != "Duty" &&
                          (r.WorkAssignment == null || r.WorkAssignment.SchedulePurpose != "Duty") &&
                          (!r.WorkPlanSegmentId.HasValue || !db.WorkPlanSegments.Any(s => s.WorkPlanSegmentId == r.WorkPlanSegmentId.Value && (s.SchedulePurpose == "Duty" || s.WorkSegmentType.Code == "LUNCH")))))) ||
                     (!hasOfficialSchedule && !x.AttendanceLogResolutions.Any(r => r.IsValid) && x.InOutModeId == 2)))
                .OrderByDescending(x => x.Date)
                .Select(x => new AttendanceLogDto
                {
                    AttendanceLogID = x.AttendanceLogId,
                    AttendanceDeviceID = x.AttendanceDeviceId,
                    IndividualID = x.IndividualId,
                    OrganisationID = x.OrganisationId,
                    OrganisationStructureID = x.OrganisationStructureId,
                    InOutModeID = x.InOutModeId,
                    Year = x.Year,
                    Month = x.Month,
                    Day = x.Day,
                    Hour = x.Hour,
                    Minute = x.Minute,
                    Second = x.Second,
                    Date = x.Date,
                    AttendanceLogModeID = x.AttendanceLogModeId,
                    AttendanceLogStateID = x.AttendanceLogStateId,
                    OperationLogID = x.OperationLogId,
                    RelatedAttendanceLogID = x.RelatedAttendanceLogId,
                    ActualInOutMode = x.ActualInOutMode,
                    ResolvedClockType = HRM.Enum.AttendanceClockType.CheckOut
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AttendanceLogDto?> GetLogsAsync(int individualId, DateTime from, DateTime to)
        {
            using var db = await _dbfactory.CreateDbContextAsync();
            return await db.AttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualId == individualId &&
                    x.AttendanceLogStateId == 1 &&
                    x.Date >= from &&
                    x.Date <= to)
                .OrderBy(x => x.Date)
                .Select(x => new AttendanceLogDto
                {
                    AttendanceLogID = x.AttendanceLogId,
                    AttendanceDeviceID = x.AttendanceDeviceId,
                    IndividualID = x.IndividualId,
                    OrganisationID = x.OrganisationId,
                    OrganisationStructureID = x.OrganisationStructureId,
                    InOutModeID = x.InOutModeId,
                    Year = x.Year,
                    Month = x.Month,
                    Day = x.Day,
                    Hour = x.Hour,
                    Minute = x.Minute,
                    Second = x.Second,
                    Date = x.Date,
                    AttendanceLogModeID = x.AttendanceLogModeId,
                    AttendanceLogStateID = x.AttendanceLogStateId,
                    OperationLogID = x.OperationLogId,
                    RelatedAttendanceLogID = x.RelatedAttendanceLogId,
                    ActualInOutMode = x.ActualInOutMode
                })
                .FirstOrDefaultAsync();
        }
    }
}
