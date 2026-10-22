using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HRM.Models;
public partial class HrmTeContext
{
    public DbSet<StaffNotification> StaffNotifications => Set<StaffNotification>();
    private static bool Changed(EntityEntry entry, params string[] names) => entry.State is EntityState.Added or EntityState.Deleted || names.Any(n => entry.Property(n).IsModified);

    private async Task CaptureStaffNotificationsAsync(bool asynchronous, CancellationToken ct)
    {
        if (Model.FindEntityType(typeof(StaffNotification)) == null) return;
        ChangeTracker.DetectChanges();
        var alerts = new List<(int Person, int Org, string Kind, string Message, string Url)>();
        async Task AddJob(int jobId, string kind, string message, string url = "/profile")
        {
            var job = ChangeTracker.Entries<Job>().FirstOrDefault(x => x.Entity.JobId == jobId)?.Entity;
            if (job == null) job = asynchronous ? await Jobs.AsNoTracking().SingleOrDefaultAsync(x => x.JobId == jobId, ct) : Jobs.AsNoTracking().SingleOrDefault(x => x.JobId == jobId);
            if (job != null) alerts.Add((job.IndividualID, job.OrganisationID, kind, message, url));
        }
        async Task AddAttendanceRequest(int requestId, int logId)
        {
            if(requestId <= 0) return;
            var request = asynchronous ? await Requests.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == requestId, ct) : Requests.AsNoTracking().SingleOrDefault(x => x.RequestId == requestId);
            var log = ChangeTracker.Entries<AttendanceLog>().FirstOrDefault(x => x.Entity.AttendanceLogId == logId)?.Entity;
            if(log == null) log = asynchronous ? await AttendanceLogs.AsNoTracking().SingleOrDefaultAsync(x => x.AttendanceLogId == logId, ct) : AttendanceLogs.AsNoTracking().SingleOrDefault(x => x.AttendanceLogId == logId);
            if(request != null && log != null && request.ApplicantBusinessEntityId != log.IndividualId && request.OrganisationBusinessEntityID == log.OrganisationId)
                alerts.Add((log.IndividualId, log.OrganisationId, "AttendanceRequest", "An attendance request was submitted for you.", "/online/attendance/attendance-request"));
        }
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            switch (entry.Entity)
            {
                case AttendanceLog clock when clock.AttendanceLogStateId == 1 && Changed(entry, nameof(AttendanceLog.Date), nameof(AttendanceLog.InOutModeId), nameof(AttendanceLog.AttendanceLogStateId)):
                    alerts.Add((clock.IndividualId, clock.OrganisationId, "Clock event", $"A clock event was {(entry.State == EntityState.Added ? "recorded" : entry.State == EntityState.Deleted ? "removed" : "updated")} for you at {clock.Date:dd MMM yyyy HH:mm:ss}.", "/online/attendance/my-attendance")); break;
                case AttendanceLogRequest attendanceRequest when entry.State == EntityState.Added:
                    await AddAttendanceRequest(attendanceRequest.RequestId, attendanceRequest.AttendanceLogId); break;
                case AttendanceLogChangeRequest correction when entry.State == EntityState.Added:
                    await AddAttendanceRequest(correction.RequestId, correction.AttendanceLogId); break;
                case Job job when Changed(entry, nameof(Job.JobStateId), nameof(Job.JoinedDate), nameof(Job.TerminatedDate), nameof(Job.JobTypeId), nameof(Job.OrganisationStructureId), nameof(Job.OrganisationID)):
                    alerts.Add((job.IndividualID, job.OrganisationID, "Employment", entry.State == EntityState.Added ? "An employment record has been added for you." : "Your employment details or employment status have changed.", "/profile")); break;
                case JobLeaveType balance when Changed(entry, nameof(JobLeaveType.RemainingDays), nameof(JobLeaveType.IsValid), nameof(JobLeaveType.IsLeaveInfoUpdated), nameof(JobLeaveType.EffectiveFromDate), nameof(JobLeaveType.EffectiveToDate)):
                    await AddJob(balance.JobId, "LeaveBalance", entry.State == EntityState.Deleted || !balance.IsValid ? "A leave entitlement has been removed or ended." : $"Your leave entitlement was updated. Recorded remaining balance: {balance.RemainingDays?.ToString("0.##") ?? "not confirmed"} days.", "/online/leaves"); break;
                case JobWorkTemplateAssignment assignment when Changed(entry, nameof(JobWorkTemplateAssignment.WorkTemplateID), nameof(JobWorkTemplateAssignment.IsActive), nameof(JobWorkTemplateAssignment.EffectiveFrom), nameof(JobWorkTemplateAssignment.EffectiveTo), nameof(JobWorkTemplateAssignment.ScheduleMode), nameof(JobWorkTemplateAssignment.WorkingDaysMask), nameof(JobWorkTemplateAssignment.RotationTemplateIds), nameof(JobWorkTemplateAssignment.RotationAnchorDate), nameof(JobWorkTemplateAssignment.ScheduledStartTime)):
                    await AddJob(assignment.JobID, "Schedule", $"Your {assignment.SchedulePurpose.ToLowerInvariant()} schedule assignment has been {(entry.State == EntityState.Added ? "added" : !assignment.IsActive || entry.State == EntityState.Deleted ? "ended" : "changed")}.", "/online/duty-roster"); break;
                case WorkAssignment duty when duty.AssignmentSource == HRM.Enum.WorkAssignmentSource.Manual && Changed(entry, nameof(WorkAssignment.IsValid), nameof(WorkAssignment.StartDateTime), nameof(WorkAssignment.EndDateTime), nameof(WorkAssignment.RequiresAttendance), nameof(WorkAssignment.RequiresCheckOut)):
                    if (duty.Description?.StartsWith(HRM.WorkPlanning.Services.DutyRosterService.MovementMarker) == true || duty.CancellationReason?.StartsWith(HRM.WorkPlanning.Services.DutyRosterService.MovementMarker) == true) break;
                    var plan = duty.WorkPlan ?? (asynchronous ? await WorkPlans.AsNoTracking().SingleOrDefaultAsync(x => x.WorkPlanId == duty.WorkPlanId, ct) : WorkPlans.AsNoTracking().SingleOrDefault(x => x.WorkPlanId == duty.WorkPlanId));
                    if(plan != null) await AddJob(plan.JobId, "Duty", $"Your dated duty assignment has been {(duty.IsValid && entry.State != EntityState.Deleted ? "added or changed" : "cancelled")} ({duty.StartDateTime:dd MMM yyyy HH:mm}–{duty.EndDateTime:HH:mm}).", "/online/duty-roster"); break;
                case TeamStaff member when Changed(entry, nameof(TeamStaff.IsValid), nameof(TeamStaff.IsSuperVisor), nameof(TeamStaff.StartDate), nameof(TeamStaff.EndDate)):
                    var team = member.Team ?? (asynchronous ? await Teams.AsNoTracking().SingleOrDefaultAsync(x => x.TeamId == member.TeamId, ct) : Teams.AsNoTracking().SingleOrDefault(x => x.TeamId == member.TeamId));
                    if(team != null) alerts.Add((member.StaffId, team.OrganisationId, "Team", $"Your team membership or supervisor responsibility has changed for {team.Name}.", "/profile")); break;
                case JobPosition position when Changed(entry, nameof(JobPosition.PositionId), nameof(JobPosition.JobPositionStateId), nameof(JobPosition.FromDate), nameof(JobPosition.ToDate)):
                    await AddJob(position.JobId, "Position", "Your position assignment or its effective period has changed."); break;
            }
        }
        foreach (var alert in alerts.Distinct())
        {
            var query = Users.Where(u => u.BusinessEntityID == alert.Person && u.IsActive && u.LocalUserStateId == 1 && u.UserOrganisation!.BusinessEntityID == alert.Org).Select(u => u.UserId);
            var ids = asynchronous ? await query.ToListAsync(ct) : query.ToList();
            foreach (var id in ids)
                if(!ChangeTracker.Entries<StaffNotification>().Any(n => n.State == EntityState.Added && n.Entity.UserId == id && n.Entity.Kind == alert.Kind && n.Entity.Message == alert.Message))
                    StaffNotifications.Add(new StaffNotification { UserId = id, IndividualId = alert.Person, OrganisationId = alert.Org, Kind = alert.Kind,
                        Message = alert.Message.Length > 500 ? alert.Message[..500] : alert.Message, Url = alert.Url, CreatedAtUtc = DateTime.UtcNow });
        }
    }
}
