using System.Data;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class LeaveSetConfigurationService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService logs)
:global::HRM.Services.Contracts.LeaveSetConfigurationServiceContracts,global::HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations{
    private async Task<(int Org,bool Super)> Scope()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Select your current organisation.");
        if(!actor.IsSuperAdministrator&&!await access.HasPermissionAsync("Admin")&&!await access.HasPermissionAsync("Leave.Configuration.Manage"))throw new UnauthorizedAccessException("Leave configuration management permission is required.");
        return(org,actor.IsSuperAdministrator);
    }
    public async Task<LeaveSetConfigurationData> LoadAsync()
    {
        var a=await Scope();await using var db=await factory.CreateDbContextAsync();
        return new(await db.Organisations.AsNoTracking().Where(o=>o.BusinessEntityID==a.Org).ToListAsync(),await db.LeaveSets.AsNoTracking().Include(s=>s.Organisation).Include(s=>s.LeaveSetLeaveTypes.Where(l=>l.IsValid&&l.LeaveType.IsActive&&(l.LeaveType.OrganisationId==a.Org||l.LeaveType.IsGlobal))).Where(s=>s.OrganisationId==a.Org).OrderBy(s=>s.Name).ToListAsync(),a.Super);
    }
    public async Task SaveAsync(LeaveSet model)
    {
        var a=await Scope();if(model.OrganisationId!=a.Org)throw new UnauthorizedAccessException("Use your current organisation.");
        if(string.IsNullOrWhiteSpace(model.Name)||model.Name.Trim().Length>250)throw new InvalidOperationException("Enter a leave set name of up to 250 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var row=model.LeaveSetId==0?new LeaveSet{OrganisationId=a.Org}:await db.LeaveSets.SingleOrDefaultAsync(s=>s.LeaveSetId==model.LeaveSetId&&s.OrganisationId==a.Org)??throw new UnauthorizedAccessException("Leave set unavailable in this organisation.");
        if(!a.Super&&(model.IsGlobal||model.IsPublic||row.IsGlobal||row.IsPublic))throw new UnauthorizedAccessException("Only Super Administrators can manage shared leave sets.");
        if(model.LeaveSetId!=0&&row.OperationLogId!=model.OperationLogId)throw new InvalidOperationException("This leave set changed. Refresh before saving.");
        var name=model.Name.Trim();if(await db.LeaveSets.AnyAsync(s=>s.OrganisationId==a.Org&&s.LeaveSetId!=model.LeaveSetId&&s.Name.Trim().ToUpper()==name.ToUpper()))throw new InvalidOperationException("This leave set name already exists.");
        var audit=await logs.CreateAsync(db,model.LeaveSetId==0?HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_SET_CREATE:HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_SET_UDPATE,$"Leave set {model.LeaveSetId} in organisation {a.Org}: {row.Name} → {name}.");
        row.Name=name;row.IsGlobal=model.IsGlobal;row.IsPublic=model.IsPublic;row.OperationLogId=audit.OperationLogId;if(model.LeaveSetId==0)db.LeaveSets.Add(row);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task DeleteAsync(LeaveSet model)
    {
        var a=await Scope();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var row=await db.LeaveSets.SingleOrDefaultAsync(s=>s.LeaveSetId==model.LeaveSetId&&s.OrganisationId==a.Org)??throw new UnauthorizedAccessException("Leave set unavailable in this organisation.");
        if(!a.Super&&(row.IsGlobal||row.IsPublic))throw new UnauthorizedAccessException("Only Super Administrators can manage shared leave sets.");
        if(row.OperationLogId!=model.OperationLogId)throw new InvalidOperationException("This leave set changed. Refresh before deleting.");
        if(await db.LeaveSetLeaveTypes.AnyAsync(s=>s.LeaveSetId==row.LeaveSetId)||await db.GroupLeaveSets.AnyAsync(s=>s.LeaveSetId==row.LeaveSetId)||await db.JobTypeLeaveSets.AnyAsync(s=>s.LeaveSetId==row.LeaveSetId))throw new InvalidOperationException("This leave set has leave type, group or Job Type assignments and cannot be deleted.");
        await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_SET_UDPATE,$"Delete unused leave set {row.LeaveSetId} in organisation {a.Org}.");db.LeaveSets.Remove(row);await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
