using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

internal static class WorkPlanSnapshots
{
    public static async Task InvalidateUnusedAsync(HrmTeContext db, int templateId)
    {
        var today = DateTime.UtcNow.AddHours(5).Date;
        var plans = await db.WorkPlans.Include(x => x.WorkPlanSegments)
            .Include(x => x.WorkAssignments).ThenInclude(x => x.WorkAssignmentSegments)
            .Include(x => x.WorkAssignments).ThenInclude(x => x.WorkAssignmentOwners)
            .Where(x => x.IsValid && x.IsGenerated && !x.IsManual && !x.IsFinalized && x.WorkDate >= today &&
                (x.WorkTemplateId == templateId || x.WorkAssignments.Any(a => a.IsValid && a.WorkTemplateId == templateId)) &&
                !db.AttendanceLogResolutions.Any(r => r.WorkPlanId == x.WorkPlanId) &&
                !db.AttendanceLogs.Any(l => l.IndividualId == x.IndividualId && l.OrganisationId == x.OrganisationBusinessEntityId && l.Date.Date == x.WorkDate.Date) &&
                !x.WorkPlanSegments.Any(s => s.AttendanceId.HasValue || s.IsCompleted)).ToListAsync();
        foreach (var plan in plans)
        {
            plan.IsValid = false;
            foreach (var segment in plan.WorkPlanSegments) segment.IsValid = false;
            foreach (var assignment in plan.WorkAssignments)
            {
                assignment.IsValid = false;
                foreach (var segment in assignment.WorkAssignmentSegments) segment.IsValid = false;
                foreach (var owner in assignment.WorkAssignmentOwners) owner.IsValid = false;
            }
        }
    }
}
