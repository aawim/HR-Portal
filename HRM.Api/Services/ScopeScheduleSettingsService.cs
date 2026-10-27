using System.Data;
using HRM.Models;
using HRM.Helpers;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Services;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;




public sealed partial class ScopeScheduleSettingsService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService audit)
:global::HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts,global::HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations{
    private static DateTime Today=>DateTime.UtcNow.AddHours(5).Date;
    private async Task<int> OrganisationAsync(int? requested)
    {
        var actor=await access.RequireContextAsync();
        var current=await access.GetCurrentOrganisationIdAsync();
        var org=requested??current;
        if(org<=0 || !actor.IsSuperAdministrator && (org!=current || !await access.HasOrganisationAccessAsync(org)) ||
            !actor.IsSuperAdministrator && !await access.HasPermissionAsync("Schedule.Attach"))
            throw new UnauthorizedAccessException("Schedule.Attach permission and access to the selected organisation are required. Only Super Admin can manage another organisation's schedules here.");
        await using var db=await factory.CreateDbContextAsync();
        if(!await db.Organisations.AnyAsync(o=>o.BusinessEntityID==org)) throw new UnauthorizedAccessException("The selected organisation is unavailable.");
        return org;
    }
    public async Task<ScopeScheduleSettingsData> LoadAsync(int? organisationId=null)
    {
        var org=await OrganisationAsync(organisationId);
        await using var db=await factory.CreateDbContextAsync();
        return new(await db.OrganisationStructures.Where(s=>s.OrganisationBusinessEntityID==org && s.OrganisationStructureStateId==3)
            .OrderBy(s=>s.Name).Select(s=>new ScopeScheduleStructureOption(s.OrganisationStructureId,s.OrganisationStructureType.TypeName+" · "+s.Name,s.OrganisationStructureTypeId,s.OrganisationStructureType.TypeName)).ToListAsync(),
            await db.Groups.Where(g=>g.BusinessEntityOrganisationId==org).OrderBy(g=>g.Name).Select(g=>new ScopeScheduleOption(g.GroupId,g.Name,true)).ToListAsync(),
            await db.WorkTemplates.Where(t=>(t.IsActive || db.ScopeScheduleAssignments.Any(a=>a.OrganisationId==org && a.WorkTemplateId==t.WorkTemplateId)) && (t.IsGlobal || t.OrganisationBusinessEntityId==org)).OrderBy(t=>t.Name).Select(t=>new ScopeScheduleOption(t.WorkTemplateId,t.Name,t.IsActive)).ToListAsync(),
            await db.ScopeScheduleAssignments.AsNoTracking().Where(a=>a.OrganisationId==org).OrderByDescending(a=>a.EffectiveFrom).ToListAsync());
    }
    public async Task SaveAsync(ScopeScheduleAssignment model,int? organisationId=null)
    {
        var org=await OrganisationAsync(organisationId);
        if(model.GroupId.HasValue && model.OrganisationStructureId.HasValue || model.EffectiveFrom.Date<Today || model.EffectiveFrom.Year>Today.Year+5 ||
            model.EffectiveTo.HasValue && model.EffectiveTo.Value.Date<model.EffectiveFrom.Date || model.SchedulePurpose is not ("Official" or "Duty") ||
            model.ScheduleMode is not ("Standard" or "Weekly" or "Rotation") || model.WorkingDaysMask is <1 or >127 || model.Priority is <0 or >9999)
            throw new InvalidOperationException("Choose a valid scope, today or a future effective period, and working pattern.");
        await using var db=await factory.CreateDbContextAsync();
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(model.OrganisationStructureId.HasValue && !await db.OrganisationStructures.AnyAsync(s=>s.OrganisationStructureId==model.OrganisationStructureId && s.OrganisationBusinessEntityID==org && s.OrganisationStructureStateId==3))
            throw new InvalidOperationException("Select an approved structure in this organisation.");
        if(model.GroupId.HasValue && !await db.Groups.AnyAsync(g=>g.GroupId==model.GroupId && g.BusinessEntityOrganisationId==org)) throw new InvalidOperationException("Select a group in this organisation.");
        var rotation=StaffScheduleCalendar.Parse(model.RotationTemplateIds);
        if(model.ScheduleMode=="Rotation" && (!model.RotationAnchorDate.HasValue || rotation.Count is <1 or >90 || rotation.Any(id=>id<0) || !rotation.Any(id=>id>0)))
            throw new InvalidOperationException("Choose a rotation anchor and 1–90 cycle days, including at least one template. Use 0 for rest days.");
        var ids=(model.ScheduleMode=="Rotation"?rotation.Where(id=>id>0):new[]{model.WorkTemplateId}).Distinct().ToList();
        if(model.ScheduleMode=="Rotation") model.WorkTemplateId=ids.First();
        var available=await db.WorkTemplates.Where(t=>ids.Contains(t.WorkTemplateId) && t.IsActive && (t.IsGlobal || t.OrganisationBusinessEntityId==org) &&
            t.DefaultStartTime.HasValue && t.DefaultEndTime.HasValue && t.WorkTemplateSegments.Any(s=>s.IsActive) &&
            (!t.EffectiveFrom.HasValue || t.EffectiveFrom<=model.EffectiveFrom) && (!t.EffectiveTo.HasValue || model.EffectiveTo.HasValue && t.EffectiveTo>=model.EffectiveTo)).Select(t=>t.WorkTemplateId).ToListAsync();
        if(available.Count!=ids.Count) throw new InvalidOperationException("Every template needs active hours and segments available for the full selected period.");
        var entity=model.Id==0?new ScopeScheduleAssignment{OrganisationId=org}:await db.ScopeScheduleAssignments.SingleOrDefaultAsync(a=>a.Id==model.Id && a.OrganisationId==org)
            ??throw new UnauthorizedAccessException("Schedule assignment not found in this organisation.");
        if(entity.Id!=0 && entity.IsActive && !model.IsActive) throw new InvalidOperationException("Use Disable to stop an enabled assignment while retaining its history and checking dependent duties.");
        if(entity.Id!=0 && (entity.Version!=model.Version || entity.EffectiveTo.HasValue && entity.EffectiveTo.Value.Date<Today)) throw new InvalidOperationException("This assignment changed or has ended. Reload or add a new assignment.");
        if(entity.Id!=0 && (entity.GroupId!=model.GroupId || entity.OrganisationStructureId!=model.OrganisationStructureId || entity.SchedulePurpose!=model.SchedulePurpose))
            throw new InvalidOperationException("Add a new assignment to change its scope or purpose.");
        if(await db.ScopeScheduleAssignments.AnyAsync(a=>a.Id!=model.Id && a.OrganisationId==org && a.IsActive && model.IsActive && a.GroupId==model.GroupId && a.OrganisationStructureId==model.OrganisationStructureId && a.SchedulePurpose==model.SchedulePurpose &&
            (model.SchedulePurpose=="Official" || a.WorkTemplateId==model.WorkTemplateId) && (!model.EffectiveTo.HasValue || a.EffectiveFrom<=model.EffectiveTo) && (!a.EffectiveTo.HasValue || a.EffectiveTo>=model.EffectiveFrom)))
            throw new InvalidOperationException("An assignment already covers these dates for this scope and purpose. End it first or choose another duty template.");
        if(model.IsActive && model.GroupId.HasValue && model.SchedulePurpose=="Official" && await db.ScopeScheduleAssignments.AnyAsync(a=>a.Id!=model.Id && a.OrganisationId==org && a.IsActive && a.GroupId.HasValue && a.SchedulePurpose=="Official" && a.Priority==model.Priority &&
            (!model.EffectiveTo.HasValue || a.EffectiveFrom<=model.EffectiveTo) && (!a.EffectiveTo.HasValue || a.EffectiveTo>=model.EffectiveFrom)))
            throw new InvalidOperationException("Choose a different group priority for overlapping official periods. Higher priority wins when staff belong to multiple groups.");
        var operation=await audit.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
            $"Scoped schedule tenant {org}, structure {model.OrganisationStructureId}, group {model.GroupId}, {model.SchedulePurpose}, template {model.WorkTemplateId}, {model.ScheduleMode}, {model.EffectiveFrom:yyyy-MM-dd}–{model.EffectiveTo:yyyy-MM-dd}, priority {model.Priority}, enabled {model.IsActive}.");
        if(entity.Id==0) db.ScopeScheduleAssignments.Add(entity);
        else
        {
            if(entity.IsActive && entity.EffectiveFrom.Date<model.EffectiveFrom.Date)
            {
                var history=new ScopeScheduleAssignment();db.Entry(history).CurrentValues.SetValues(entity);history.Id=0;
                history.EffectiveTo=model.EffectiveFrom.Date.AddDays(-1);history.Version=1;db.ScopeScheduleAssignments.Add(history);
            }
            entity.Version++;
        }
        entity.OrganisationStructureId=model.OrganisationStructureId;entity.GroupId=model.GroupId;entity.WorkTemplateId=model.WorkTemplateId;
        entity.SchedulePurpose=model.SchedulePurpose;entity.ScheduleMode=model.ScheduleMode;entity.WorkingDaysMask=model.ScheduleMode=="Standard"?31:model.WorkingDaysMask;
        entity.RotationAnchorDate=model.ScheduleMode=="Rotation"?model.RotationAnchorDate?.Date:null;entity.RotationTemplateIds=model.ScheduleMode=="Rotation"?System.Text.Json.JsonSerializer.Serialize(rotation):null;
        entity.Priority=model.Priority;entity.EffectiveFrom=model.EffectiveFrom.Date;entity.EffectiveTo=model.EffectiveTo?.Date;entity.OperationLogId=operation.OperationLogId;entity.IsActive=model.IsActive;
        await db.SaveChangesAsync();
        if(entity.IsActive) await ValidateDutiesAsync(db,entity,entity.EffectiveFrom,entity.EffectiveTo);
        if(entity.IsActive) await RefreshAsync(db,entity,entity.EffectiveFrom,entity.EffectiveTo,$"Your organisation's schedule configuration changed from {entity.EffectiveFrom:dd MMM yyyy}. Check Today's Work Plan for your effective hours.");
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task SetEnabledAsync(int id,int version,bool enabled,int? organisationId=null)
    {
        var org=await OrganisationAsync(organisationId);
        if(enabled)
        {
            await using var read=await factory.CreateDbContextAsync();
            var row=await read.ScopeScheduleAssignments.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id && a.OrganisationId==org)
                ??throw new UnauthorizedAccessException("Schedule assignment not found in this organisation.");
            if(row.Version!=version)throw new InvalidOperationException("This assignment changed. Reload before enabling it.");
            if(row.IsActive)return;
            if(row.EffectiveTo.HasValue && row.EffectiveTo.Value.Date<Today)throw new InvalidOperationException("This period has ended. Add a new schedule assignment.");
            if(row.EffectiveFrom<Today)row.EffectiveFrom=Today;row.IsActive=true;
            await SaveAsync(row,organisationId);return;
        }
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity=await db.ScopeScheduleAssignments.SingleOrDefaultAsync(a=>a.Id==id && a.OrganisationId==org)
            ??throw new UnauthorizedAccessException("Schedule assignment not found in this organisation.");
        if(entity.Version!=version)throw new InvalidOperationException("This assignment changed. Reload before disabling it.");
        if(!entity.IsActive)return;
        if(entity.EffectiveTo.HasValue && entity.EffectiveTo.Value.Date<Today)throw new InvalidOperationException("This period has ended; its history cannot be disabled.");
        var operation=await audit.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,$"Disabled scoped schedule {id} in tenant {org}, effective {Today:yyyy-MM-dd}; previous dates retained.");
        if(entity.EffectiveFrom<Today)
        {
            var history=new ScopeScheduleAssignment();db.Entry(history).CurrentValues.SetValues(entity);history.Id=0;history.EffectiveTo=Today.AddDays(-1);history.Version=1;
            db.ScopeScheduleAssignments.Add(history);entity.EffectiveFrom=Today;
        }
        entity.IsActive=false;entity.Version++;entity.OperationLogId=operation.OperationLogId;
        await db.SaveChangesAsync();await ValidateDutiesAsync(db,entity,Today,entity.EffectiveTo);
        await RefreshAsync(db,entity,Today,entity.EffectiveTo,"A schedule assignment was disabled. Check your effective work plan.");
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task EndAsync(int id,int version,DateTime lastDay,int? organisationId=null)
    {
        var org=await OrganisationAsync(organisationId);
        if(lastDay.Date<Today.AddDays(-1)) throw new InvalidOperationException("The last day must be yesterday or later.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity=await db.ScopeScheduleAssignments.SingleOrDefaultAsync(a=>a.Id==id && a.OrganisationId==org && a.IsActive)??throw new InvalidOperationException("Active assignment not found.");
        if(entity.Version!=version) throw new InvalidOperationException("This assignment changed. Reload before ending it.");
        if(entity.EffectiveTo.HasValue && lastDay.Date>entity.EffectiveTo.Value.Date) throw new InvalidOperationException("This assignment already ends earlier.");
        if(lastDay.Date<entity.EffectiveFrom.Date) entity.IsActive=false; else entity.EffectiveTo=lastDay.Date;
        entity.Version++;var operation=await audit.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,$"Ended scoped schedule {id} in tenant {org}, last day {lastDay:yyyy-MM-dd}.");entity.OperationLogId=operation.OperationLogId;
        await db.SaveChangesAsync();await ValidateDutiesAsync(db,entity,Today,null);
        await RefreshAsync(db,entity,lastDay.Date.AddDays(1),null,"A schedule assignment ended. Check your effective work plan.");await db.SaveChangesAsync();await tx.CommitAsync();
    }
    private static async Task<List<int>> AffectedJobsAsync(HrmTeContext db,ScopeScheduleAssignment scope,DateTime from,DateTime? to)
    {
        var jobs=db.Jobs.AsNoTracking().Where(j=>j.OrganisationID==scope.OrganisationId && j.JobStateId==4 && (!j.TerminatedDate.HasValue || j.TerminatedDate>=from));
        if(scope.GroupId.HasValue) jobs=jobs.Where(j=>db.GroupStaffs.Any(m=>m.GroupId==scope.GroupId && m.StaffIndividualId==j.IndividualID && (!to.HasValue || m.EffectiveStartDate<=to) && (!m.EffectiveEndDate.HasValue || m.EffectiveEndDate>=from)));
        if(scope.OrganisationStructureId.HasValue)
        {
            var nodes=new HashSet<int>{scope.OrganisationStructureId.Value};
            var edges=await db.OrganisationStructureRelations.AsNoTracking().Where(r=>r.OrganisationStructure.OrganisationBusinessEntityID==scope.OrganisationId && r.ParentOrganisationStructure.OrganisationBusinessEntityID==scope.OrganisationId &&
                (!to.HasValue || r.ActiveFromDate<=to) && (!r.ActiveToDate.HasValue || r.ActiveToDate>=from)).Select(r=>new{r.OrganisationStructureId,r.ParentOrganisationStructureId}).ToListAsync();
            bool changed;do{changed=false;foreach(var edge in edges)if(nodes.Contains(edge.ParentOrganisationStructureId))changed|=nodes.Add(edge.OrganisationStructureId);}while(changed);
            jobs=jobs.Where(j=>nodes.Contains(j.OrganisationStructureId));
        }
        return await jobs.Select(j=>j.JobId).ToListAsync();
    }
    private static async Task ValidateDutiesAsync(HrmTeContext db,ScopeScheduleAssignment scope,DateTime from,DateTime? to)
    {
        var org=scope.OrganisationId;var affected=await AffectedJobsAsync(db,scope,from,to);
        var jobs=await db.Jobs.AsNoTracking().Where(j=>affected.Contains(j.JobId)).ToListAsync();
        foreach(var job in jobs)
        {
            var rows=await EffectiveStaffSchedules.LoadAsync(db,job,from,to??DateTime.MaxValue.Date);
            foreach(var duty in rows.Where(c=>c.Assignment.SchedulePurpose=="Duty").Select(c=>c.Assignment))
                if(!await OfficialSchedulePrerequisite.CoversAsync(db,job.JobId,org,duty.EffectiveFrom.Date<from.Date?from.Date:duty.EffectiveFrom.Date,to.HasValue&&(!duty.EffectiveTo.HasValue||duty.EffectiveTo>to)?to:duty.EffectiveTo)) throw new InvalidOperationException($"Job {job.JobId} has duty dates without default official hours. Configure default hours for the complete duty period first.");
            var days=await db.WorkAssignments.Where(a=>a.IsValid && a.SchedulePurpose=="Duty" && a.WorkPlan.IsValid && a.WorkPlan.JobId==job.JobId && a.WorkPlan.WorkDate>=from && (!to.HasValue || a.WorkPlan.WorkDate<=to)).Select(a=>a.WorkPlan.WorkDate).Distinct().ToListAsync();
            foreach(var day in days) if(!await OfficialSchedulePrerequisite.CoversAsync(db,job.JobId,org,day,day)) throw new InvalidOperationException($"Job {job.JobId} has dated duties without default official hours.");
        }
    }
    private static async Task RefreshAsync(HrmTeContext db,ScopeScheduleAssignment scope,DateTime from,DateTime? to,string message)
    {
        var org=scope.OrganisationId;var affected=await AffectedJobsAsync(db,scope,from,to);
        var plans=await db.WorkPlans.Include(p=>p.WorkPlanSegments).Include(p=>p.WorkAssignments).ThenInclude(a=>a.WorkAssignmentSegments).Include(p=>p.WorkAssignments).ThenInclude(a=>a.WorkAssignmentOwners)
            .Where(p=>p.OrganisationBusinessEntityId==org && affected.Contains(p.JobId) && p.IsValid && p.IsGenerated && !p.IsManual && !p.IsFinalized && p.WorkDate>=from && (!to.HasValue || p.WorkDate<=to) &&
                !db.AttendanceLogResolutions.Any(r=>r.WorkPlanId==p.WorkPlanId) && !db.AttendanceLogs.Any(l=>l.OrganisationId==org && l.IndividualId==p.IndividualId && l.Date.Date==p.WorkDate.Date)).ToListAsync();
        foreach(var plan in plans) {plan.IsValid=false;foreach(var s in plan.WorkPlanSegments)s.IsValid=false;foreach(var a in plan.WorkAssignments){a.IsValid=false;foreach(var s in a.WorkAssignmentSegments)s.IsValid=false;foreach(var owner in a.WorkAssignmentOwners)owner.IsValid=false;}}
        var users=await db.Users.Where(u=>u.IsActive && u.LocalUserStateId==1 && db.Jobs.Any(j=>affected.Contains(j.JobId) && j.IndividualID==u.BusinessEntityID && j.OrganisationID==org && j.JobStateId==4 && !j.TerminatedDate.HasValue)).Select(u=>new{u.UserId,u.BusinessEntityID}).ToListAsync();
        foreach(var user in users)db.StaffNotifications.Add(new(){UserId=user.UserId,IndividualId=user.BusinessEntityID,OrganisationId=org,Kind="Schedule assignment",Message=message,Url="/online",CreatedAtUtc=DateTime.UtcNow});
    }
}
