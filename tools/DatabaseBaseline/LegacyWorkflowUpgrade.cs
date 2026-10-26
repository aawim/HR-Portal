using HRM.Models;
using HRM.Models.Workflow;
using Microsoft.EntityFrameworkCore;
using System.Data;

// Copy routing for future submissions; never mutate a definition used by an in-flight request.
internal static class LegacyWorkflowUpgrade
{
    public static async Task RunAsync(HrmTeContext db)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var sources = await db.WfWorkflowDefinitions.Include(x => x.WorkflowJobTypes)
            .Include(x => x.Stages).ThenInclude(x => x.Approvers)
            .Where(x => x.IsActive && x.LeaveTypeId == null && x.LeaveRequestKind == null).ToListAsync();
        var organisations = await db.Organisations.Select(x => x.BusinessEntityID).ToListAsync();
        var jobTypes = await db.JobTypes.Where(x => x.IsActive != false).Select(x => x.JobTypeId).ToListAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
        var added = 0;
        foreach (var org in organisations)
        foreach (var job in jobTypes)
        foreach (var type in sources.Select(x => x.RequestTypeId).Distinct())
        {
            if (await db.WfWorkflowDefinitions.AnyAsync(x => x.IsActive && x.OrganisationBusinessEntityId == org &&
                x.RequestTypeId == type && x.LeaveTypeId == null && x.LeaveRequestKind == 0 &&
                x.WorkflowJobTypes.Any(j => j.IsActive && j.JobTypeId == job))) continue;
            var candidates = sources.Where(x => x.RequestTypeId == type &&
                (x.OrganisationBusinessEntityId == org || x.OrganisationBusinessEntityId == null) &&
                (!x.WorkflowJobTypes.Any() || x.WorkflowJobTypes.Any(j => j.IsActive && j.JobTypeId == job)) &&
                (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= date) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date))
                .Select(x => new { Flow = x, Priority = (x.OrganisationBusinessEntityId == org ? 2 : 0) + (x.WorkflowJobTypes.Any() ? 1 : 0) })
                .OrderByDescending(x => x.Priority).ThenByDescending(x => x.Flow.VersionNumber).ToList();
            if (candidates.Count == 0) continue;
            var selected = candidates[0];
            if (candidates.Count(x => x.Priority == selected.Priority && x.Flow.VersionNumber == selected.Flow.VersionNumber) != 1)
                throw new InvalidOperationException($"Ambiguous legacy flow for organisation {org}, Job Type {job}, Request Type {type}; upgrade rolled back.");
            var source = selected.Flow;
            if (!source.Stages.Any(x => x.IsActive)) continue;
            var version = (await db.WfWorkflowDefinitions.Where(x => x.OrganisationBusinessEntityId == org && x.RequestTypeId == type)
                .Select(x => (int?)x.VersionNumber).MaxAsync() ?? 0) + 1;
            var clone = new WfWorkflowDefinition { OrganisationBusinessEntityId = org, RequestTypeId = type,
                LeaveRequestKind = 0, Name = $"Imported legacy flow {source.WorkflowDefinitionId} / Job Type {job}",
                VersionNumber = version, CreatedAt = DateTime.UtcNow, EffectiveFrom = source.EffectiveFrom, EffectiveTo = source.EffectiveTo,
                WorkflowJobTypes = [new() { JobTypeId = job }] };
            foreach (var stage in source.Stages.Where(x => x.IsActive).OrderBy(x => x.StageOrder))
                clone.Stages.Add(new() { StageOrder = stage.StageOrder, StageName = stage.StageName,
                    RequestStateId = stage.RequestStateId, ApprovalMode = stage.ApprovalMode, MinimumApprovals = stage.MinimumApprovals,
                    Approvers = stage.Approvers.Select(a => new WfStageApprover { ApproverType = a.ApproverType,
                        ApproverReferenceId = a.ApproverReferenceId, SequenceNumber = a.SequenceNumber, IsRequired = a.IsRequired }).ToList() });
            db.WfWorkflowDefinitions.Add(clone); await db.SaveChangesAsync(); added++;
        }
        if (added > 0) await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID) VALUES(18,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','',{"Copied " + added.ToString() + " legacy approval routes into canonical tenant/Job Type flows; existing instances and original definitions preserved."},1)");
        await tx.CommitAsync(); Console.WriteLine($"Canonical approval flows imported: {added}. Existing instances retained their original definitions.");
    }
}
