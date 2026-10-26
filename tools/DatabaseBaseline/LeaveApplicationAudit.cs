using HRM.Models;
using Microsoft.EntityFrameworkCore;

internal static class LeaveApplicationAudit
{
    public static async Task RunAsync(HrmTeContext db)
    {
        var types = await db.LeaveTypes.AsNoTracking().OrderBy(t=>t.LeaveTypeId).Select(t=>new {t.LeaveTypeId,t.Name,t.OrganisationId,t.IsGlobal,t.IsActive,t.RequestTypeId}).ToListAsync();
        var requestTypes = await db.RequestTypes.AsNoTracking().ToDictionaryAsync(r=>r.RequestTypeId,r=>r.TypeName);
        foreach(var t in types) Console.WriteLine($"TYPE {t.LeaveTypeId} | {t.Name} | org={t.OrganisationId} global={t.IsGlobal} active={t.IsActive} | request={t.RequestTypeId}: {requestTypes.GetValueOrDefault(t.RequestTypeId??0,"MISSING")}");
        foreach(var p in await db.LeavePolicies.AsNoTracking().ToListAsync()) Console.WriteLine($"POLICY {p.LeavePolicyId} | type={p.LeaveTypeId} | org={p.OrganisationId} | {p.Name} | request={p.RequestTypeId}: {requestTypes.GetValueOrDefault(p.RequestTypeId,"MISSING")} | active={p.IsActive} | handover={p.RequireWorkHandover} after={p.WorkHandoverAfterDays} days | from={p.EffectiveFrom:yyyy-MM-dd} to={p.EffectiveTo:yyyy-MM-dd}");
        foreach(var f in await db.WfWorkflowDefinitions.AsNoTracking().Include(x=>x.WorkflowJobTypes).Include(x=>x.Stages).Where(f=>f.IsActive).ToListAsync()) Console.WriteLine($"FLOW {f.WorkflowDefinitionId} | org={f.OrganisationBusinessEntityId} | request={f.RequestTypeId}: {requestTypes.GetValueOrDefault(f.RequestTypeId,"MISSING")} | typeScope={f.LeaveTypeId} kind={f.LeaveRequestKind} | jobs={string.Join(',',f.WorkflowJobTypes.Where(j=>j.IsActive).Select(j=>j.JobTypeId))} | stages={f.Stages.Count} | {f.Name}");
        var today = DateTime.UtcNow.AddHours(5).Date;
        foreach (var id in new[] { 1,66 })
        {
            var type = await HRM.Services.Leave.LeavePolicyResolver.ResolveAsync(db,id,3,today);
            if (type?.RequestTypeId is not > 0) throw new InvalidOperationException($"Annual type {id} has no valid route.");
            var flow = await HRM.Services.Leave.LeaveFlowConfigurationService.ResolveAsync(db,new HRM.DTOs.ApprovalFlow.WorkflowRequestContextDto {
                RequestTypeId = type.RequestTypeId.Value, OrganisationBusinessEntityId = 3, JobTypeId = 1,
                LeaveTypeId = id, LeaveRequestKind = 0, EffectiveDate = DateOnly.FromDateTime(today)
            });
            Console.WriteLine($"VERIFIED annual type={id} -> request={type.RequestTypeId} -> Contract flow={flow.WorkflowDefinitionId}, active stages={flow.Stages.Count(x=>x.IsActive)}");
        }
        foreach (var r in await db.LeaveTypeReasonTypes.AsNoTracking().Where(x=>x.LeaveTypeId==1 || x.LeaveTypeId==66).OrderBy(x=>x.LeaveTypeId).ThenBy(x=>x.LeaveTypeReasonTypeId).ToListAsync())
            Console.WriteLine($"REASON {r.LeaveTypeReasonTypeId} | type={r.LeaveTypeId} | {r.ReasonTypeName}");
        var recent=await db.LeaveRequests.AsNoTracking().Include(x=>x.Request).Include(x=>x.Leave).ThenInclude(x=>x.LeaveType).OrderByDescending(x=>x.RequestId).Take(8).ToListAsync();
        foreach(var r in recent)Console.WriteLine($"RECENT request={r.RequestId} | leaveType={r.Leave.LeaveTypeId}:{r.Leave.LeaveType.Name} | requestType={r.Request.RequestTypeId}:{requestTypes.GetValueOrDefault(r.Request.RequestTypeId)} | state={r.Request.RequestStateId}");
    }
}
