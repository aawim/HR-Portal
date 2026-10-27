using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;




public sealed partial class LeaveAccrualSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
:global::HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts,global::HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations{
    private async Task<int> RequireOrganisationAsync()
    {
        var actor=await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") && !await access.HasUserGroupAsync("Human Resource: Staff") &&
            !await access.HasUserGroupAsync("Human Resource :Head"))
            throw new UnauthorizedAccessException("Only HR or tenant administrators can manage leave accrual.");
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Tenant access denied.");
        return org;
    }

    public async Task<LeaveAccrualSettingsData> LoadAsync()
    {
        var org = await RequireOrganisationAsync();
        await using var db = await factory.CreateDbContextAsync();
        return new(org, await db.Organisations.Where(x => x.BusinessEntityID == org).Select(x => x.OrganisationName).SingleAsync(),
            await db.JobTypes.OrderBy(x => x.TypeName).Select(x => new AccrualOption(x.JobTypeId, x.TypeName)).ToListAsync(),
            await db.LeaveTypes.Where(x => x.IsGlobal || x.OrganisationId == org).OrderBy(x => x.Name)
                .Select(x => new AccrualOption(x.LeaveTypeId, x.Name + " (#" + x.LeaveTypeId + ")")).ToListAsync(),
            await db.JobTypeLeaveAccrualRules.AsNoTracking().Where(x => x.OrganisationId == org).ToListAsync());
    }

    public async Task SaveAsync(JobTypeLeaveAccrualRule model)
    {
        var org = await RequireOrganisationAsync();
        if (model.GrantDays <= 0 || model.GrantDays > 366 || decimal.Round(model.GrantDays, 2) != model.GrantDays ||
            model.FirstGrantAfterMonths < 0 || model.FirstGrantAfterMonths > 1200 || model.RepeatEveryMonths < 1 || model.RepeatEveryMonths > 120)
            throw new InvalidOperationException("Enter grant days (up to 366, two decimals), waiting months (0–1200), and recurrence months (1–120).");
        if (model.CarryForwardMode is not ("Unspecified" or "None" or "Unlimited" or "Capped") ||
            (model.CarryForwardMode == "Capped" && (!model.CarryForwardLimit.HasValue || model.CarryForwardLimit < 0)) ||
            model.MaximumBalance < 0)
            throw new InvalidOperationException("Select valid carry-forward settings and non-negative limits.");
        if (model.IsEnabled && model.CarryForwardMode == "Unspecified")
            throw new InvalidOperationException("Confirm carry-forward settings before enabling automatic accrual.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.JobTypes.AnyAsync(x => x.JobTypeId == model.JobTypeId) ||
            !await db.LeaveTypes.AnyAsync(x => x.LeaveTypeId == model.LeaveTypeId && (x.IsGlobal || x.OrganisationId == org)))
            throw new UnauthorizedAccessException("Select an available job type and a leave type belonging to this tenant.");
        var entity = model.Id == 0 ? new JobTypeLeaveAccrualRule { OrganisationId = org } :
            await db.JobTypeLeaveAccrualRules.SingleOrDefaultAsync(x => x.Id == model.Id && x.OrganisationId == org)
            ?? throw new UnauthorizedAccessException("Rule not found in this tenant.");
        if (entity.Id != 0 && entity.Version != model.Version) throw new InvalidOperationException("This rule changed. Reload before saving.");
        if (entity.Id != 0 && (entity.JobTypeId != model.JobTypeId || entity.LeaveTypeId != model.LeaveTypeId))
            throw new InvalidOperationException("Create a separate rule for a different job type or leave type.");
        if (await db.JobTypeLeaveAccrualRules.AnyAsync(x => x.OrganisationId == org && x.JobTypeId == model.JobTypeId &&
            x.LeaveTypeId == model.LeaveTypeId && x.Id != model.Id)) throw new InvalidOperationException("A rule already exists for this job type and leave type.");
        if (model.EffectiveFrom.Date < LeaveAccrualCalculator.Today && (entity.Id == 0 || entity.EffectiveFrom.Date != model.EffectiveFrom.Date))
            throw new InvalidOperationException("New processing start dates cannot be backdated. Existing balances are preserved.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE,
            $"Tenant {org}: leave accrual rule job type {model.JobTypeId}, leave {model.LeaveTypeId}; {model.GrantDays} days, first after {model.FirstGrantAfterMonths} months, every {model.RepeatEveryMonths} months; carry {model.CarryForwardMode}; enabled {model.IsEnabled}.");
        if (entity.Id == 0) db.JobTypeLeaveAccrualRules.Add(entity); else entity.Version++;
        entity.JobTypeId = model.JobTypeId; entity.LeaveTypeId = model.LeaveTypeId;
        entity.GrantDays = model.GrantDays; entity.FirstGrantAfterMonths = model.FirstGrantAfterMonths;
        entity.RepeatEveryMonths = model.RepeatEveryMonths; entity.CarryForwardMode = model.CarryForwardMode;
        entity.CarryForwardLimit = model.CarryForwardMode == "Capped" ? model.CarryForwardLimit : null;
        entity.MaximumBalance = model.MaximumBalance; entity.EffectiveFrom = model.EffectiveFrom.Date;
        entity.IsEnabled = model.IsEnabled; entity.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<List<AccrualPreview>> PreviewAsync(int ruleId)
    {
        var org = await RequireOrganisationAsync();
        return await new LeaveAccrualProcessor(factory).PreviewAsync(org, ruleId);
    }

    public async Task<int> RunAsync(int ruleId)
    {
        var org = await RequireOrganisationAsync();
        return await new LeaveAccrualProcessor(factory).RunAsync(org, ruleId);
    }
}
