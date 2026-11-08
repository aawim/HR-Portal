using Microsoft.EntityFrameworkCore;
using HRM.Models;

namespace HRM.Services;

public partial class WorkTemplateService
{
    private static async Task<bool> HasLiveAssignmentsAsync(HrmTeContext db, int templateId)
    {
        var today = DateTime.UtcNow.AddHours(5).Date;
        var attached = await db.JobWorkTemplateAssignments.AsNoTracking()
            .Where(x => x.IsActive && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .Select(x => new { x.WorkTemplateID, x.RotationTemplateIds }).ToListAsync();
        var scoped = db.Model.FindEntityType(typeof(ScopeScheduleAssignment)) == null ? [] : await db.ScopeScheduleAssignments.AsNoTracking().Where(x => x.IsActive && (x.EffectiveTo == null || x.EffectiveTo >= today)).Select(x => new { WorkTemplateID = x.WorkTemplateId, x.RotationTemplateIds }).ToListAsync(); return attached.Concat(scoped).Any(x => x.WorkTemplateID == templateId || HRM.Helpers.StaffScheduleCalendar.Parse(x.RotationTemplateIds).Contains(templateId)) ||
            await db.JobWorkTemplates.AnyAsync(x => x.IsActive && x.WorkTemplateId == templateId && (x.EffectiveTo == null || x.EffectiveTo >= today));
    }
}
