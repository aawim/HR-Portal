using System.Data;
using HRM.Components.Shared;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record WorkingHourOption(int Id, string Name);
public sealed record WorkingHoursSettingsData(string Organisation, List<WorkingHourOption> JobTypes,
    List<JobTypeWorkingHourRule> Rules, List<OrganisationScheduleAmendment> Amendments, List<OrganisationWorkingHourRequirement> Requirements, bool CanConfigure, bool CanAmend);

public sealed class WorkingHoursSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
{
    public const string ConfigurePermission = HRM.Authorization.Permissions.WorkingHoursConfigure;
    public const string AmendPermission = HRM.Authorization.Permissions.OfficialScheduleAmend;
    public const string ReviewPermission = HRM.Authorization.Permissions.WorkingHoursReview;

    private async Task<int> OrganisationAsync()
    {
        await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select an organisation you can access.");
        return org;
    }
    private async Task<bool> AllowedAsync(string permission)
    {
        var actor = await access.RequireContextAsync();
        return actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin") ||
            await access.HasPermissionAsync(permission);
    }
    private async Task<int> RequireAsync(string permission)
    {
        var org = await OrganisationAsync();
        if (!await AllowedAsync(permission)) throw new UnauthorizedAccessException("You do not have permission to manage this configuration. Contact your HR team.");
        return org;
    }
    public async Task<WorkingHoursSettingsData> LoadAsync()
    {
        var org = await OrganisationAsync();
        var configure = await AllowedAsync(ConfigurePermission); var amend = await AllowedAsync(AmendPermission);
        if (!configure && !amend) throw new UnauthorizedAccessException("You do not have permission to manage working hours.");
        await using var db = await factory.CreateDbContextAsync();
        var from = DateTime.UtcNow.AddHours(5).Date.AddMonths(-3);
        return new(await db.Organisations.Where(x => x.BusinessEntityID == org).Select(x => x.OrganisationName).SingleAsync(),
            await db.JobTypes.OrderBy(x => x.TypeName).Select(x => new WorkingHourOption(x.JobTypeId, x.TypeName)).ToListAsync(),
            await db.JobTypeWorkingHourRules.AsNoTracking().Where(x => x.OrganisationId == org).OrderByDescending(x => x.EffectiveFrom).ToListAsync(),
            await db.OrganisationScheduleAmendments.AsNoTracking().Where(x => x.OrganisationId == org && x.WorkDate >= from).OrderByDescending(x => x.WorkDate).Take(100).ToListAsync(),
            await db.OrganisationWorkingHourRequirements.AsNoTracking().Where(x => x.OrganisationId == org).OrderByDescending(x => x.FromDate).Take(100).ToListAsync(), configure, amend);
    }
    public async Task SaveRuleAsync(JobTypeWorkingHourRule model)
    {
        var org = await RequireAsync(ConfigurePermission);
        if (model.JobTypeId <= 0 || 
            model.WeekStartsOn is < 0 or > 6 || model.EffectiveFrom.Year < 2000 ||
            (int)model.EffectiveFrom.DayOfWeek != model.WeekStartsOn ||
            model.EffectiveTo.HasValue && (model.EffectiveTo < model.EffectiveFrom || (int)model.EffectiveTo.Value.AddDays(1).DayOfWeek != model.WeekStartsOn))
            throw new InvalidOperationException("Select a Job Type and an effective period covering complete weeks.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.JobTypes.AnyAsync(x => x.JobTypeId == model.JobTypeId)) throw new InvalidOperationException("Select an existing Job Type.");
        var entity = model.Id == 0 ? new JobTypeWorkingHourRule { OrganisationId = org } :
            await db.JobTypeWorkingHourRules.SingleOrDefaultAsync(x => x.Id == model.Id && x.OrganisationId == org)
            ?? throw new UnauthorizedAccessException("Configuration not found in this organisation.");
        if (entity.Id != 0 && entity.Version != model.Version) throw new InvalidOperationException("This rule changed. Reload before saving.");
        if (entity.Id != 0 && entity.JobTypeId != model.JobTypeId) throw new InvalidOperationException("Create a separate rule for a different Job Type.");
        if (await db.JobTypeWorkingHourRules.AnyAsync(x => x.OrganisationId == org && x.JobTypeId == model.JobTypeId && x.Id != model.Id &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo >= model.EffectiveFrom) && (!model.EffectiveTo.HasValue || x.EffectiveFrom <= model.EffectiveTo)))
            throw new InvalidOperationException("This Job Type already has a rule covering these dates. End the previous rule before adding another.");
        // Keep week boundaries stable when effective periods change.
        if (await db.JobTypeWorkingHourRules.AnyAsync(x => x.OrganisationId == org && x.JobTypeId == model.JobTypeId && x.Id != model.Id && x.WeekStartsOn != model.WeekStartsOn))
            throw new InvalidOperationException("Use the same week start as the existing rules for this Job Type.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE,
            $"Working hours: tenant {org}, Job Type {model.JobTypeId}, template-derived hours and roster days, week start {model.WeekStartsOn}, duty counts {model.CountDutyHours}, {model.EffectiveFrom:yyyy-MM-dd} to {model.EffectiveTo:yyyy-MM-dd}.");
        if (entity.Id == 0) db.JobTypeWorkingHourRules.Add(entity); else entity.Version++;
        entity.JobTypeId = model.JobTypeId; entity.WorkingDaysMask = 31;
        entity.WeekStartsOn = model.WeekStartsOn; entity.CountDutyHours = model.CountDutyHours;
        entity.EffectiveFrom = model.EffectiveFrom.Date; entity.EffectiveTo = model.EffectiveTo?.Date; entity.OperationLogId = audit.OperationLogId;
        await NotifyJobsAsync(db, org, db.Jobs.Where(j => j.OrganisationID == org && j.JobStateId == 4 && j.JobTypeId == model.JobTypeId &&
            (!model.EffectiveTo.HasValue || j.JoinedDate <= model.EffectiveTo) && (!j.TerminatedDate.HasValue || j.TerminatedDate >= model.EffectiveFrom)),
            "Working hours", $"Working hours for your Job Type follow your assigned official template from {model.EffectiveFrom:dd MMM yyyy}. View your weekly reconciliation for leave, amendments and make-up time.");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task SaveRequirementAsync(OrganisationWorkingHourRequirement model)
    {
        var org = await RequireAsync(ConfigurePermission);
        var today = DateTime.UtcNow.AddHours(5).Date;
        if (model.FromDate.Date < today || model.ToDate.Date < model.FromDate.Date || model.ToDate.Date > today.AddYears(1) ||
            model.RequiredDailyMinutes is < 1 or > 1440 || string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length > 1000)
            throw new InvalidOperationException("Select today or future dates, daily hours up to 24 and a reason.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (model.JobTypeId.HasValue && !await db.JobTypes.AnyAsync(t => t.JobTypeId == model.JobTypeId))
            throw new InvalidOperationException("Select an existing Job Type.");
        var entity = model.Id == 0 ? new OrganisationWorkingHourRequirement { OrganisationId = org } :
            await db.OrganisationWorkingHourRequirements.SingleOrDefaultAsync(r => r.Id == model.Id && r.OrganisationId == org)
            ?? throw new UnauthorizedAccessException("Configuration not found in this organisation.");
        if (entity.Id != 0 && (entity.Version != model.Version || entity.FromDate < today))
            throw new InvalidOperationException("This period has started or changed. Reload and add a future period.");
        if (await db.OrganisationWorkingHourRequirements.AnyAsync(r => r.OrganisationId == org && r.JobTypeId == model.JobTypeId &&
            r.Id != model.Id && r.IsActive && model.IsActive && r.FromDate <= model.ToDate && r.ToDate >= model.FromDate))
            throw new InvalidOperationException("An active requirement already covers these dates for this Job Type.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE,
            $"Temporary daily working hours: tenant {org}, Job Type {model.JobTypeId}, {model.RequiredDailyMinutes} minutes, {model.FromDate:yyyy-MM-dd} to {model.ToDate:yyyy-MM-dd}, active {model.IsActive}. {model.Reason.Trim()}");
        if (entity.Id == 0) db.OrganisationWorkingHourRequirements.Add(entity); else entity.Version++;
        entity.JobTypeId = model.JobTypeId; entity.FromDate = model.FromDate.Date; entity.ToDate = model.ToDate.Date;
        entity.RequiredDailyMinutes = model.RequiredDailyMinutes; entity.Reason = model.Reason.Trim(); entity.IsActive = model.IsActive; entity.OperationLogId = audit.OperationLogId;
        await NotifyJobsAsync(db, org, db.Jobs.Where(j => j.OrganisationID == org && j.JobStateId == 4 &&
            (!model.JobTypeId.HasValue || j.JobTypeId == model.JobTypeId) && j.JoinedDate <= model.ToDate && (!j.TerminatedDate.HasValue || j.TerminatedDate >= model.FromDate)),
            "Temporary daily working hours", $"{model.RequiredDailyMinutes / 60m:0.##} hours per working day from {model.FromDate:dd MMM yyyy} to {model.ToDate:dd MMM yyyy}. {(model.IsActive ? "Check your work plan for the required finish time. Shorter template hours are extended; longer hours remain unchanged." : "This requirement was withdrawn.")} {entity.Reason}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task SaveAmendmentAsync(OrganisationScheduleAmendment model)
    {
        var org = await RequireAsync(AmendPermission);
        if (model.WorkDate.Date < DateTime.UtcNow.AddHours(5).Date || model.WorkDate.Date > DateTime.UtcNow.AddHours(5).Date.AddYears(1) ||
            model.OriginalStart < TimeSpan.Zero || model.RevisedStart >= TimeSpan.FromDays(1) || model.RevisedStart <= model.OriginalStart ||
            string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length > 1000)
            throw new InvalidOperationException("Select today or a future date, a later official start time and a reason (up to 1,000 characters).");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var originalClock = TimeOnly.FromTimeSpan(model.OriginalStart);
        var revisedClock = TimeOnly.FromTimeSpan(model.RevisedStart);
        var officialTemplateIds = db.JobWorkTemplateAssignments.Where(a => a.Job.OrganisationID == org && a.IsActive && a.SchedulePurpose == "Official" &&
            a.EffectiveFrom <= model.WorkDate && (!a.EffectiveTo.HasValue || a.EffectiveTo >= model.WorkDate)).Select(a => a.WorkTemplateID)
            .Union(db.JobWorkTemplates.Where(a => a.Job.OrganisationID == org && a.IsActive && a.EffectiveFrom <= model.WorkDate &&
                (!a.EffectiveTo.HasValue || a.EffectiveTo >= model.WorkDate)).Select(a => a.WorkTemplateId))
            .Union(db.ScopeScheduleAssignments.Where(a=>a.OrganisationId==org && a.IsActive && a.SchedulePurpose=="Official" && a.EffectiveFrom<=model.WorkDate && (!a.EffectiveTo.HasValue || a.EffectiveTo>=model.WorkDate)).Select(a=>a.WorkTemplateId));
        if (await db.WorkTemplates.AnyAsync(t => t.IsActive && officialTemplateIds.Contains(t.WorkTemplateId) &&
            t.DefaultStartTime == originalClock && !t.EndsNextDay && t.DefaultEndTime <= revisedClock))
            throw new InvalidOperationException("The temporary start must be earlier than the matching official template's end time.");
        if (await db.WorkPlans.AnyAsync(p => p.OrganisationBusinessEntityId == org && p.WorkDate == model.WorkDate.Date && p.IsValid && p.IsFinalized))
            throw new InvalidOperationException("An attendance plan for this date is finalized. HR must reopen it before amending the schedule.");
        if (await db.WorkAssignments.AnyAsync(a => a.WorkPlan.OrganisationBusinessEntityId == org && a.WorkPlan.WorkDate == model.WorkDate.Date &&
            a.WorkPlan.IsValid && a.IsValid && a.SchedulePurpose == "Official" && a.StartDateTime.TimeOfDay == model.OriginalStart && a.EndDateTime <= model.WorkDate.Date.Add(model.RevisedStart)))
            throw new InvalidOperationException("The amended start must be earlier than the official end time.");
        var entity = model.Id == 0 ? new OrganisationScheduleAmendment { OrganisationId = org } :
            await db.OrganisationScheduleAmendments.SingleOrDefaultAsync(x => x.Id == model.Id && x.OrganisationId == org)
            ?? throw new UnauthorizedAccessException("Amendment not found in this organisation.");
        if (entity.Id != 0 && entity.Version != model.Version) throw new InvalidOperationException("This amendment changed. Reload before saving.");
        if (await db.OrganisationScheduleAmendments.AnyAsync(x => x.OrganisationId == org && x.WorkDate == model.WorkDate.Date && x.OriginalStart == model.OriginalStart && x.Id != model.Id))
            throw new InvalidOperationException("An amendment already exists for this date and original start. Edit that amendment instead.");
        if (entity.Id != 0 && (entity.WorkDate.Date != model.WorkDate.Date || entity.OriginalStart != model.OriginalStart))
            throw new InvalidOperationException("The date and original start cannot be changed. Withdraw this amendment and create another.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE,
            $"Official schedule amendment: tenant {org}, {model.WorkDate:yyyy-MM-dd}, {model.OriginalStart} to {model.RevisedStart}; end unchanged; waived minutes not owed; active {model.IsActive}; {model.Reason.Trim()}.");
        if (entity.Id == 0) db.OrganisationScheduleAmendments.Add(entity); else entity.Version++;
        entity.WorkDate = model.WorkDate.Date; entity.OriginalStart = model.OriginalStart; entity.RevisedStart = model.RevisedStart;
        entity.Reason = model.Reason.Trim(); entity.IsActive = model.IsActive; entity.OperationLogId = audit.OperationLogId;
        var notification = model.IsActive
            ? $"Official schedules starting at {model.OriginalStart:hh\\:mm} on {model.WorkDate:dd MMM yyyy} now start at {model.RevisedStart:hh\\:mm}. End unchanged; waived time needs no make-up. {entity.Reason}"
            : $"The official start-time amendment for {model.WorkDate:dd MMM yyyy} was withdrawn. Original schedules apply.";
        await NotifyJobsAsync(db, org, db.Jobs.Where(j => j.OrganisationID == org && j.JobStateId == 4 && j.JoinedDate <= model.WorkDate &&
            (!j.TerminatedDate.HasValue || j.TerminatedDate >= model.WorkDate)), "Schedule amendment", notification);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    private static async Task NotifyJobsAsync(HrmTeContext db, int org, IQueryable<Job> jobs, string kind, string message)
    {
        if (message.Length > 500) message = message[..500];
        var people = await jobs.Select(j => j.IndividualID).Distinct().ToListAsync();
        var users = await db.Users.Where(u => people.Contains(u.BusinessEntityID) && u.IsActive && u.LocalUserStateId == 1)
            .Select(u => new { u.UserId, u.BusinessEntityID }).ToListAsync();
        foreach (var u in users) db.StaffNotifications.Add(new StaffNotification { UserId = u.UserId, IndividualId = u.BusinessEntityID,
            OrganisationId = org, Kind = kind, CreatedAtUtc = DateTime.UtcNow, Url = "/online", Message = message });
    }
}
