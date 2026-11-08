using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed partial class JobTypeOvertimeSettingsService(IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access, IOperationLogService logs)
:global::HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts,global::HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations{
    private async Task<int> RequireOrganisationAsync()
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) ||
            !(actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin") ||
              await access.HasPermissionAsync(WorkingHoursSettingsService.ConfigurePermission)))
            throw new UnauthorizedAccessException("You do not have permission to configure overtime in this organisation.");
        return org;
    }

    public async Task<JobTypeOvertimeRule> LoadAsync(int jobTypeId)
    {
        var org = await RequireOrganisationAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.JobTypes.AnyAsync(t => t.JobTypeId == jobTypeId))
            throw new InvalidOperationException("Job Type not found.");
        return await db.JobTypeOvertimeRules.AsNoTracking().SingleOrDefaultAsync(r => r.OrganisationId == org && r.JobTypeId == jobTypeId)
            ?? new JobTypeOvertimeRule { OrganisationId = org, JobTypeId = jobTypeId };
    }

    public async Task SaveAsync(JobTypeOvertimeRule model)
    {
        var org = await RequireOrganisationAsync();
        // A stale form must never write a policy into a newly switched tenant.
        if (model.OrganisationId != org) throw new InvalidOperationException("Your organisation changed. Reopen Job Type configuration before saving.");
        if (model.IsAllowed && (model.MaximumWeeklyHours <= 0 || model.MaximumWeeklyHours > 168 ||
            decimal.Round(model.MaximumWeeklyHours, 2) != model.MaximumWeeklyHours ||
            model.MaximumDailyHours.HasValue && (model.MaximumDailyHours <= 0 || model.MaximumDailyHours > 24 ||
                model.MaximumDailyHours > model.MaximumWeeklyHours || decimal.Round(model.MaximumDailyHours.Value, 2) != model.MaximumDailyHours)))
            throw new InvalidOperationException("Enter a weekly OT limit above 0 and up to 168 hours. The optional daily limit must be above 0, no more than 24 hours, and no more than the weekly limit. Use up to two decimal places.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.JobTypes.AnyAsync(t => t.JobTypeId == model.JobTypeId && t.IsActive != false))
            throw new InvalidOperationException("Select an active Job Type.");
        var entity = await db.JobTypeOvertimeRules.SingleOrDefaultAsync(r => r.OrganisationId == org && r.JobTypeId == model.JobTypeId);
        if (entity != null && (entity.Id != model.Id || entity.Version != model.Version) || entity == null && model.Id != 0)
            throw new InvalidOperationException("This OT policy changed. Reload it before saving.");
        // The audit service saves every tracked entity. Persist the audit before
        // tracking a new policy, whose required audit foreign key is not set yet.
        var weekly = model.IsAllowed ? model.MaximumWeeklyHours : 0;
        var daily = model.IsAllowed ? model.MaximumDailyHours : null;
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE,
            $"OT policy: organisation {org}, Job Type {model.JobTypeId}, allowed {model.IsAllowed}, weekly maximum {weekly}, daily maximum {daily}. Previous policy ID {model.Id}, version {model.Version}.");
        if (entity == null)
        {
            entity = new JobTypeOvertimeRule { OrganisationId = org, JobTypeId = model.JobTypeId };
            db.JobTypeOvertimeRules.Add(entity);
        }
        else entity.Version++;
        entity.IsAllowed = model.IsAllowed;
        entity.MaximumWeeklyHours = weekly;
        entity.MaximumDailyHours = daily;
        entity.OperationLogId = audit.OperationLogId;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
