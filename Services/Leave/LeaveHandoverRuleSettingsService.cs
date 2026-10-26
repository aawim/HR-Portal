using System.Data;
using System.Text.Json;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Leave;

public sealed class LeaveHandoverRuleSettingsService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService logs)
{
    private async Task<int> Scope()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Select an organisation you can access.");
        if(!actor.IsSuperAdministrator&&!await access.HasPermissionAsync("Admin")&&!await access.HasPermissionAsync("Leave.Configuration.Manage"))throw new UnauthorizedAccessException("Leave configuration management permission is required.");
        return org;
    }
    public static HandoverRuleEdit Edit(LeaveHandoverRule x)=>new(){Id=x.Id,OrganisationId=x.OrganisationId,Version=x.Version,Name=x.Name,Scope=x.Scope,OrganisationStructureId=x.OrganisationStructureId,TeamId=x.TeamId,JobTypeId=x.JobTypeId,AllLeaveTypes=x.AllLeaveTypes,
        LeaveTypeIds=x.LeaveTypes.Where(t=>t.IsActive).Select(t=>t.LeaveTypeId).ToList(),Mode=x.Mode,AfterDays=x.AfterDays,Priority=x.Priority,EffectiveFrom=x.EffectiveFrom,EffectiveTo=x.EffectiveTo,IsActive=x.IsActive};
    public async Task<HandoverRuleSettingsData> LoadAsync()
    {
        var org=await Scope();await using var db=await factory.CreateDbContextAsync();
        return new(org,await db.Organisations.Where(x=>x.BusinessEntityID==org).Select(x=>x.OrganisationName).SingleAsync(),
            (await db.LeaveHandoverRules.AsNoTracking().Include(x=>x.LeaveTypes).Where(x=>x.OrganisationId==org).OrderBy(x=>x.Scope).ThenBy(x=>x.Name).ToListAsync()).Select(Edit).ToList(),
            await db.LeaveTypes.AsNoTracking().Where(x=>x.IsActive&&(x.IsGlobal||x.OrganisationId==org)).OrderBy(x=>x.Name).Select(x=>new HandoverOption(x.LeaveTypeId,x.Name+" · "+x.Code)).ToListAsync(),
            await db.JobTypes.AsNoTracking().Where(x=>x.IsActive==true).OrderBy(x=>x.TypeName).Select(x=>new HandoverOption(x.JobTypeId,x.TypeName)).ToListAsync(),
            await db.OrganisationStructures.AsNoTracking().Where(x=>x.OrganisationBusinessEntityID==org&&x.OrganisationStructureStateId==3).OrderBy(x=>x.Name).Select(x=>new HandoverOption(x.OrganisationStructureId,x.Name)).ToListAsync(),
            await db.Teams.AsNoTracking().Where(x=>x.OrganisationId==org&&x.IsValid==true).OrderBy(x=>x.Name).Select(x=>new HandoverOption(x.TeamId,x.Name)).ToListAsync());
    }
    public async Task<List<HandoverOption>> StaffAsync(string search)
    {
        var org=await Scope();await using var db=await factory.CreateDbContextAsync();var day=DateTime.UtcNow.AddHours(5).Date;var term=search.Trim();
        return await db.Jobs.AsNoTracking().Where(x=>x.OrganisationID==org&&x.JobStateId==4&&x.JoinedDate<=day&&x.TerminatedDate==null&&
            (term==""||(x.Individual.FirstNameEnglish+" "+(x.Individual.MiddleNameEnglish??"")+" "+(x.Individual.LastNameEnglish??"")).Contains(term)))
            .OrderBy(x=>x.Individual.FirstNameEnglish).Take(50).Select(x=>new HandoverOption(x.JobId,(x.Individual.FirstNameEnglish+" "+(x.Individual.LastNameEnglish??"")).Trim())).ToListAsync();
    }
    public async Task<EffectiveHandoverRule> PreviewAsync(int jobId,int typeId,DateTime date)
    {
        var org=await Scope();await using var db=await factory.CreateDbContextAsync();
        if(date==default)throw new InvalidOperationException("Select a leave start date.");
        if(!await db.Jobs.AnyAsync(x=>x.JobId==jobId&&x.OrganisationID==org)||!await db.LeaveTypes.AnyAsync(x=>x.LeaveTypeId==typeId&&x.IsActive&&(x.IsGlobal||x.OrganisationId==org)))throw new UnauthorizedAccessException("Choose staff and a leave type in this organisation.");
        return await LeaveHandoverRuleResolver.ResolveAsync(db,jobId,typeId,date);
    }
    public async Task SaveAsync(HandoverRuleEdit m)
    {
        var org=await Scope();
        if(m.OrganisationId!=org)throw new UnauthorizedAccessException("The organisation context changed. Refresh before saving this rule.");
        if(string.IsNullOrWhiteSpace(m.Name)||m.Name.Trim().Length>200||m.Scope is not ("Organisation" or "Structure" or "Team")||m.Mode is not ("Required" or "Exempt" or "Inherit")||m.AfterDays is <0 or >3650||m.Priority is <0 or >1000||m.EffectiveFrom==default||m.EffectiveTo<m.EffectiveFrom)
            throw new InvalidOperationException("Enter a name, valid scope, dates and a threshold from 0 to 3650 days.");
        if(m.Id==0&&m.Mode=="Inherit")throw new InvalidOperationException("No rule is needed to inherit defaults. Edit an existing override to select Inherit default.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var row=m.Id==0?new LeaveHandoverRule{OrganisationId=org}:await db.LeaveHandoverRules.Include(x=>x.LeaveTypes).SingleOrDefaultAsync(x=>x.Id==m.Id&&x.OrganisationId==org)??throw new UnauthorizedAccessException("Rule unavailable in this organisation.");
        if(m.Id!=0&&row.Version!=m.Version)throw new InvalidOperationException("This rule changed. Refresh before saving.");
        var structure=m.Scope=="Structure"?m.OrganisationStructureId:null;var team=m.Scope=="Team"?m.TeamId:null;
        if(m.Scope=="Structure"&&(structure==null||!await db.OrganisationStructures.AnyAsync(x=>x.OrganisationStructureId==structure&&x.OrganisationBusinessEntityID==org&&(!m.IsActive||x.OrganisationStructureStateId==3))))throw new InvalidOperationException("Select an approved department or structure in this organisation.");
        if(m.Scope=="Team"&&(team==null||!await db.Teams.AnyAsync(x=>x.TeamId==team&&x.OrganisationId==org&&(!m.IsActive||x.IsValid==true))))throw new InvalidOperationException("Select a team in this organisation.");
        if(m.JobTypeId.HasValue&&!await db.JobTypes.AnyAsync(x=>x.JobTypeId==m.JobTypeId&&x.IsActive==true))throw new InvalidOperationException("Select an active Job Type.");
        var types=m.AllLeaveTypes?new List<int>():m.LeaveTypeIds.Distinct().ToList();
        if(!m.AllLeaveTypes&&(types.Count==0||await db.LeaveTypes.CountAsync(x=>types.Contains(x.LeaveTypeId)&&(!m.IsActive||x.IsActive)&&(x.IsGlobal||x.OrganisationId==org))!=types.Count))throw new InvalidOperationException("Choose at least one available leave type or All leave types.");
        if(m.IsActive&&m.Mode!="Inherit")
        {
            var competing=await db.LeaveHandoverRules.Include(x=>x.LeaveTypes).Where(x=>x.OrganisationId==org&&x.Id!=m.Id&&x.IsActive&&x.Mode!="Inherit"&&x.Scope==m.Scope&&x.OrganisationStructureId==structure&&x.TeamId==team&&x.AllLeaveTypes==m.AllLeaveTypes&&x.JobTypeId==m.JobTypeId&&x.Priority==m.Priority&&
                (x.EffectiveTo==null||x.EffectiveTo>=m.EffectiveFrom)&&(m.EffectiveTo==null||x.EffectiveFrom<=m.EffectiveTo)).ToListAsync();
            if(competing.Any(x=>m.AllLeaveTypes||x.LeaveTypes.Any(t=>t.IsActive&&types.Contains(t.LeaveTypeId))))throw new InvalidOperationException("An overlapping rule has the same scope, filters and priority. Edit it or choose a different priority or period.");
        }
        m.OrganisationStructureId=structure;m.TeamId=team;m.LeaveTypeIds=types;
        var before=JsonSerializer.Serialize(Edit(row));
        var audit=await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_TYPE_UDPATE,$"Handover rule in tenant {org}. Before: {before}. After: {JsonSerializer.Serialize(m)}");
        row.Name=m.Name.Trim();row.Scope=m.Scope;row.OrganisationStructureId=structure;row.TeamId=team;row.JobTypeId=m.JobTypeId;row.AllLeaveTypes=m.AllLeaveTypes;row.Mode=m.Mode;row.AfterDays=m.AfterDays;row.Priority=m.Priority;row.EffectiveFrom=m.EffectiveFrom.Date;row.EffectiveTo=m.EffectiveTo?.Date;row.IsActive=m.IsActive;row.OperationLogId=audit.OperationLogId;
        if(m.Id==0)db.LeaveHandoverRules.Add(row);else row.Version++;
        foreach(var link in row.LeaveTypes)link.IsActive=types.Contains(link.LeaveTypeId);
        foreach(var id in types.Where(id=>!row.LeaveTypes.Any(x=>x.LeaveTypeId==id)))row.LeaveTypes.Add(new(){LeaveTypeId=id});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
