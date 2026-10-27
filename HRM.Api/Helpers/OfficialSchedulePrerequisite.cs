using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class OfficialSchedulePrerequisite
{
    public const string Message = "Assign default official working hours covering the duty dates before adding duty schedules.";
    // A default attachment may have rest days; duty can still be rostered on those days.
    public static async Task<bool> CoversAsync(HrmTeContext db, int job, int org, DateTime from, DateTime? to, CancellationToken ct = default, int? excluding = null)
    {
        var employeeJob=await db.Jobs.AsNoTracking().SingleOrDefaultAsync(j=>j.JobId==job && j.OrganisationID==org,ct);
        if(employeeJob==null) return false;
        var candidates=await EffectiveStaffSchedules.LoadAsync(db,employeeJob,from.Date,to?.Date??DateTime.MaxValue.Date,ct,excluding);
        var intervals=candidates.Where(c=>c.Assignment.SchedulePurpose=="Official").Select(c=>c.Assignment).Select(a =>
            (Start: a.WorkTemplate.EffectiveFrom.HasValue && a.WorkTemplate.EffectiveFrom.Value.Date > a.EffectiveFrom.Date ? a.WorkTemplate.EffectiveFrom.Value.Date : a.EffectiveFrom.Date,
            End: a.WorkTemplate.EffectiveTo.HasValue && (!a.EffectiveTo.HasValue || a.WorkTemplate.EffectiveTo.Value.Date < a.EffectiveTo.Value.Date) ? a.WorkTemplate.EffectiveTo.Value.Date : a.EffectiveTo?.Date ?? DateTime.MaxValue.Date))
            .Where(a=>a.End>=a.Start).OrderBy(a=>a.Start).ToList();
        var cursor = from.Date; var finish = to?.Date ?? DateTime.MaxValue.Date;
        foreach (var range in intervals)
        {
            if (range.End < cursor) continue;
            if (range.Start > cursor) return false;
            if (range.End >= finish) return true;
            cursor = range.End.AddDays(1);
        }
        return false;
    }
}
