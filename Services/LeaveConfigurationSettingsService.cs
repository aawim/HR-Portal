using System.Data;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using HRM.Components.Shared;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed record LeaveConfigurationData(List<Organisation> Organisations,List<LeaveConfigurableValueType> Types,List<JobType> JobTypes,List<LeaveConfigurableValue> Values);
public sealed class LeaveConfigurationSettingsService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService logs)
{
    private async Task<int> RequireAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0 || !await access.HasOrganisationAccessAsync(org) || !(actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage") || await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Leave.Configuration.Manage")))
            throw new UnauthorizedAccessException("You do not have permission to manage leave configuration in this organisation.");
        return org;
    }
    public async Task<LeaveConfigurationData> LoadAsync()
    {
        var org=await RequireAsync();await using var db=await factory.CreateDbContextAsync();
        return new(await db.Organisations.AsNoTracking().Where(x=>x.BusinessEntityID==org).ToListAsync(),await db.LeaveConfigurableValueTypes.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(),await db.JobTypes.AsNoTracking().OrderBy(x=>x.TypeName).ToListAsync(),
            await db.LeaveConfigurableValues.AsNoTracking().Include(x=>x.Organisation).Include(x=>x.JobType).Include(x=>x.LeaveConfigurableValueType).Where(x=>x.OrganisationId==org).OrderBy(x=>x.LeaveConfigurableValueId).ToListAsync());
    }
    public async Task SaveAsync(LeaveConfigurableValue model)
    {
        var org=await RequireAsync();if(model.OrganisationId!=org)throw new UnauthorizedAccessException("Configuration must belong to your current organisation.");
        if(string.IsNullOrWhiteSpace(model.Value)||model.Value.Length>1000)throw new InvalidOperationException("Enter a configuration value of up to 1,000 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.LeaveConfigurableValueTypes.AnyAsync(x=>x.LeaveConfigurableValueTypeId==model.LeaveConfigurableValueTypeId)||model.JobTypeId.HasValue&&!await db.JobTypes.AnyAsync(x=>x.JobTypeId==model.JobTypeId))throw new InvalidOperationException("Select valid configuration and Job Types.");
        var entity=model.LeaveConfigurableValueId==0?new LeaveConfigurableValue{OrganisationId=org}:await db.LeaveConfigurableValues.SingleOrDefaultAsync(x=>x.LeaveConfigurableValueId==model.LeaveConfigurableValueId&&x.OrganisationId==org)??throw new UnauthorizedAccessException("Configuration not available in this organisation.");
        if(await db.LeaveConfigurableValues.AnyAsync(x=>x.OrganisationId==org&&x.LeaveConfigurableValueId!=model.LeaveConfigurableValueId&&x.JobTypeId==model.JobTypeId&&x.LeaveConfigurableValueTypeId==model.LeaveConfigurableValueTypeId))throw new InvalidOperationException("This configuration already exists for the selected Job Type.");
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.USERS_UPDATE,$"Leave configuration {entity.LeaveConfigurableValueId}, organisation {org}: {entity.Value} → {model.Value.Trim()}.");
        entity.LeaveConfigurableValueTypeId=model.LeaveConfigurableValueTypeId;entity.JobTypeId=model.JobTypeId;entity.Value=model.Value.Trim();entity.IsValid=model.IsValid;
        if(entity.LeaveConfigurableValueId==0)db.LeaveConfigurableValues.Add(entity);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task DeleteAsync(int id)
    {
        var org=await RequireAsync();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity=await db.LeaveConfigurableValues.SingleOrDefaultAsync(x=>x.LeaveConfigurableValueId==id&&x.OrganisationId==org)??throw new UnauthorizedAccessException("Configuration not available in this organisation.");
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.USERS_UPDATE,$"Delete leave configuration {id}, organisation {org}.");db.LeaveConfigurableValues.Remove(entity);await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
