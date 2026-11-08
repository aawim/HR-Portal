using HRM.DTOs.Attendance;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed class MyAttendanceSupportService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : IMyAttendanceSupportService
{
    public async Task<MyAttendanceSupport> LoadAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        from = from.Date; to = to.Date;
        if (to < from || (to - from).TotalDays > 3660) throw new ArgumentException("Choose a valid attendance date range.");
        var individualId = await access.GetCurrentIndividualIdAsync();
        var organisationId = await access.GetCurrentOrganisationIdAsync();
        if (individualId <= 0 || organisationId <= 0) throw new UnauthorizedAccessException("Your employee session is unavailable.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var rest = await StaffScheduleCalendar.RestDaysAsync(db, individualId, from, to, ct, organisationId);
        var eligibility = await AttendanceEligibilityCalendar.LoadAsync(db, individualId, from, to, organisationId, ct: ct);
        var leaves = await db.Leaves.AsNoTracking().Where(l => l.Job.IndividualID == individualId && l.Job.OrganisationID == organisationId &&
            l.LeaveStateId == 4 && l.FromDate < to.AddDays(1) && l.ToDate >= from)
            .Select(l => new ApprovedAttendanceLeave(l.FromDate, l.ToDate, l.LeaveTypeId.HasValue ? l.LeaveType.Name : "Leave")).ToListAsync(ct);
        return new(rest, eligibility, leaves);
    }
}
