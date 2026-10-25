using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record OperationalGroupOption(int Id, string Name);
public sealed record OperationalGroupRow(int Id, string Name, int TypeId, string Type, int StructureId, string Structure, int AuditId);
public sealed record OperationalGroupsData(int OrganisationId, string Organisation, bool IsSuperAdmin, bool CanManage,
    List<OperationalGroupOption> Organisations, List<OperationalGroupOption> Types, List<OperationalGroupOption> Structures, List<OperationalGroupRow> Groups);
public sealed class GroupsManagementService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
{
    private async Task<(int Org, bool Super, bool Manage)> ScopeAsync(int? selected, bool write = false)
    {
        var actor = await access.RequireContextAsync();
        var super = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var current = await access.GetCurrentOrganisationIdAsync();
        var org = selected.GetValueOrDefault() > 0 ? selected!.Value : current;
        if (org <= 0 || !super && (org != current || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Use your current organisation to manage groups.");
        var manage = super || await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Organisation.Edit") || await access.HasPermissionAsync("UserOrganisations.Edit");
        if (write && !manage) throw new UnauthorizedAccessException("You do not have permission to manage groups.");
        if (!manage && !actor.IsHRStaff && !actor.IsHRHead && !actor.IsHRManager && !actor.IsOrganisationAdmin &&
            !await access.HasPermissionAsync("UserOrganisations.View") && !await access.HasPermissionAsync("UserOrganisations.Admin.View"))
            throw new UnauthorizedAccessException("You do not have permission to view organisation groups.");
        return (org, super, manage);
    }
    public async Task<OperationalGroupsData> LoadAsync(int? selected = null)
    {
        var scope = await ScopeAsync(selected);
        await using var db = await factory.CreateDbContextAsync();
        var name = await db.Organisations.Where(o=>o.BusinessEntityID==scope.Org).Select(o=>o.OrganisationName).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Organisation not found.");
        return new(scope.Org,name,scope.Super,scope.Manage,
            await db.Organisations.Where(o=>scope.Super || o.BusinessEntityID==scope.Org).OrderBy(o=>o.OrganisationName).Select(o=>new OperationalGroupOption(o.BusinessEntityID,o.OrganisationName)).ToListAsync(),
            await db.GroupTypes.Where(t=>t.OrganisationBusinessEntityId==scope.Org).OrderBy(t=>t.Name).Select(t=>new OperationalGroupOption(t.GroupTypeId,t.Name)).ToListAsync(),
            await db.OrganisationStructures.Where(s=>s.OrganisationBusinessEntityID==scope.Org && s.OrganisationStructureStateId==3).OrderBy(s=>s.Name).Select(s=>new OperationalGroupOption(s.OrganisationStructureId,s.OrganisationStructureType.TypeName+" · "+s.Name)).ToListAsync(),
            await db.Groups.Where(g=>g.BusinessEntityOrganisationId==scope.Org).OrderBy(g=>g.Name).Select(g=>new OperationalGroupRow(g.GroupId,g.Name,g.GroupTypeId,g.GroupType.Name,g.OrganisationStructureId,g.OrganisationStructure.Name,g.OperationLogId)).ToListAsync());
    }
    public async Task<int> RequireGroupAsync(int id, bool write = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        var org = await db.Groups.Where(g=>g.GroupId==id).Select(g=>(int?)g.BusinessEntityOrganisationId).SingleOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("Group not available.");
        await ScopeAsync(org,write); return org;
    }
    public async Task<bool> CanManageGroupAsync(int id)
    {
        var org=await RequireGroupAsync(id);
        return (await ScopeAsync(org)).Manage;
    }
    public async Task SaveGroupAsync(int org, int id, string name, int type, int structure, int expectedAudit)
    {
        await ScopeAsync(org,true); name=name.Trim();
        if(name.Length is <1 or >250)throw new InvalidOperationException("Enter a group name of up to 250 characters.");
        await using var db=await factory.CreateDbContextAsync(); await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.GroupTypes.AnyAsync(t=>t.GroupTypeId==type && t.OrganisationBusinessEntityId==org) ||
            !await db.OrganisationStructures.AnyAsync(s=>s.OrganisationStructureId==structure && s.OrganisationBusinessEntityID==org && s.OrganisationStructureStateId==3))
            throw new InvalidOperationException("Select a group type and an approved structure in this organisation.");
        if(await db.Groups.AnyAsync(g=>g.BusinessEntityOrganisationId==org && g.GroupId!=id && g.Name==name))throw new InvalidOperationException("A group with this name already exists.");
        var entity=id==0?new Group{BusinessEntityOrganisationId=org}:await db.Groups.SingleOrDefaultAsync(g=>g.GroupId==id && g.BusinessEntityOrganisationId==org)
            ?? throw new UnauthorizedAccessException("Group not available in this organisation.");
        if(id!=0 && entity.OperationLogId!=expectedAudit)throw new InvalidOperationException("This group changed. Refresh before saving.");
        var audit=await logs.CreateAsync(db,id==0?SharedConfig.OperationLogActionTypes.GROUPS_CREATE:SharedConfig.OperationLogActionTypes.GROUPS_UPDATE,
            $"Group {id}, organisation {org}: {entity.Name} → {name}; type {type}, approved structure {structure}.");
        entity.Name=name;entity.GroupTypeId=type;entity.OrganisationStructureId=structure;entity.OperationLogId=audit.OperationLogId;
        if(id==0)db.Groups.Add(entity);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task DeleteGroupAsync(int org, int id, int expectedAudit)
    {
        await ScopeAsync(org,true);await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity=await db.Groups.SingleOrDefaultAsync(g=>g.GroupId==id && g.BusinessEntityOrganisationId==org)??throw new UnauthorizedAccessException("Group not available.");
        if(entity.OperationLogId!=expectedAudit)throw new InvalidOperationException("This group changed. Refresh before deleting.");
        if(await db.GroupStaffs.AnyAsync(x=>x.GroupId==id)||await db.GroupSchedules.AnyAsync(x=>x.GroupId==id)||await db.GroupLeaveSets.AnyAsync(x=>x.GroupId==id)||await db.ScopeScheduleAssignments.AnyAsync(x=>x.GroupId==id))
            throw new InvalidOperationException("This group has staff, schedule or leave history and cannot be deleted.");
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.GROUPS_UPDATE,$"Delete unused group {id} ({entity.Name}), organisation {org}.");
        db.Groups.Remove(entity);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task SaveTypeAsync(int org, int id, string name, string? originalName)
    {
        await ScopeAsync(org,true);name=name.Trim();if(name.Length is <1 or >150)throw new InvalidOperationException("Enter a group type name of up to 150 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(await db.GroupTypes.AnyAsync(t=>t.OrganisationBusinessEntityId==org && t.GroupTypeId!=id && t.Name==name))throw new InvalidOperationException("This group type already exists.");
        var entity=id==0?new GroupType{OrganisationBusinessEntityId=org}:await db.GroupTypes.SingleOrDefaultAsync(t=>t.GroupTypeId==id && t.OrganisationBusinessEntityId==org)??throw new UnauthorizedAccessException("Group type not available.");
        if(id!=0 && entity.Name!=originalName)throw new InvalidOperationException("This group type changed. Refresh before saving.");
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.GROUPS_UPDATE,$"Group type {id}, organisation {org}: {entity.Name} → {name}.");
        entity.Name=name;if(id==0)db.GroupTypes.Add(entity);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task DeleteTypeAsync(int org,int id,string originalName)
    {
        await ScopeAsync(org,true);await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity=await db.GroupTypes.SingleOrDefaultAsync(t=>t.GroupTypeId==id && t.OrganisationBusinessEntityId==org)??throw new UnauthorizedAccessException("Group type not available.");
        if(entity.Name!=originalName)throw new InvalidOperationException("This group type changed. Refresh before deleting.");
        if(await db.Groups.AnyAsync(g=>g.GroupTypeId==id))throw new InvalidOperationException("This group type is used by groups and cannot be deleted.");
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.GROUPS_UPDATE,$"Delete unused group type {id} ({entity.Name}), organisation {org}.");
        db.GroupTypes.Remove(entity);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task AssignLeaveSetAsync(int groupId,int leaveSetId)
    {
        var org=await RequireGroupAsync(groupId,true);await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.LeaveSets.AnyAsync(l=>l.LeaveSetId==leaveSetId && l.OrganisationId==org))throw new InvalidOperationException("Select a leave set from this organisation.");
        if(await db.GroupLeaveSets.AnyAsync(l=>l.GroupId==groupId && l.LeaveSetId==leaveSetId && l.IsValid))throw new InvalidOperationException("This leave set is already assigned.");
        var audit=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.GROUP_LEAVE_SET_CREATE,$"Assign leave set {leaveSetId} to group {groupId}, organisation {org}.");
        db.GroupLeaveSets.Add(new(){GroupId=groupId,LeaveSetId=leaveSetId,IsValid=true,OperationLogId=audit.OperationLogId});await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
