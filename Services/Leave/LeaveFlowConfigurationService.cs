using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Helpers;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public sealed class LeaveFlowConfigurationService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
{
    private async Task<int> RequireOrganisationAsync()
    {
        var user = await access.RequireContextAsync();
        if (!user.IsSuperAdministrator && !user.IsHRStaff && !await access.HasPermissionAsync("Super.Admin.Manage") && !await access.HasPermissionAsync("Admin"))
            throw new UnauthorizedAccessException("Only HR, administrators or Super Administrators can configure request approval flows.");
        var org = 0;
        try { org = await access.GetCurrentOrganisationIdAsync(); }
        catch (InvalidOperationException) { /* Some staff contexts contain only an active job. */ }
        if (org <= 0 && user.ActiveJob != null && user.IndividualId > 0)
        {
            await using var db = await factory.CreateDbContextAsync();
            org = await db.Jobs.AsNoTracking()
                .Where(x => x.JobId == user.ActiveJob.JobId && x.IndividualID == user.IndividualId)
                .Select(x => x.OrganisationID).SingleOrDefaultAsync();
        }
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("Select an organisation you can manage.");
        return org;
    }

    public async Task<LeaveFlowSettingsData> LoadAsync()
    {
        var org = await RequireOrganisationAsync();
        await using var db = await factory.CreateDbContextAsync();
        var organisationName = await db.Organisations.AsNoTracking().Where(x => x.BusinessEntityID == org)
            .Select(x => x.OrganisationName).SingleOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("The selected organisation is unavailable. Select an organisation you can manage.");
        return new()
        {
            Organisation = organisationName,
            JobTypes = await db.JobTypes.AsNoTracking().Where(x => x.IsActive != false).OrderBy(x => x.TypeName).Select(x => new LeaveFlowOption(x.JobTypeId, x.TypeName)).ToListAsync(),
            RequestTypes = await db.RequestTypes.AsNoTracking().Where(x => x.RequestTypeId != LeaveChangeRequestTypes.Legacy).OrderBy(x => x.TypeName)
                .Select(x => new LeaveFlowOption(x.RequestTypeId, x.TypeName)).ToListAsync(),
            UserGroups = await db.UserGroups.AsNoTracking().Where(x => x.IsGlobal || x.UserOrganisation.BusinessEntityID == org)
                .OrderBy(x => x.GroupName).Select(x => new LeaveFlowOption(x.UserGroupId, x.GroupName)).ToListAsync(),
            States = await db.RequestStates.AsNoTracking().Where(x => x.IsProcessingState && !x.IsFinalState && x.RequestStateId != 1)
                .OrderBy(x => x.RequestStateId).Select(x => new LeaveFlowOption(x.RequestStateId, x.StateName)).ToListAsync()
        };
    }

    public async Task<LeaveFlowConfigurationDto> GetAsync(int jobType, int requestType)
    {
        var org = await RequireOrganisationAsync();
        await using var db = await factory.CreateDbContextAsync();
        var definition = await db.WfWorkflowDefinitions.AsNoTracking().Include(x => x.Stages).ThenInclude(x => x.Approvers)
            .Where(x => x.OrganisationBusinessEntityId == org && x.LeaveTypeId == null && x.RequestTypeId == requestType && x.LeaveRequestKind == 0 &&
                x.IsActive && x.WorkflowJobTypes.Any(j => j.JobTypeId == jobType && j.IsActive))
            .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync();
        var imported = false;
        if (definition == null)
        {
            var sourceType = LeaveChangeRequestTypes.IsChange(requestType) ? LeaveChangeRequestTypes.Legacy : requestType;
            var sourceKind = requestType == LeaveChangeRequestTypes.EarlyReturn ? 2 : requestType == LeaveChangeRequestTypes.Extension ? 1 : 0;
            var legacy = await db.WfWorkflowDefinitions.AsNoTracking().Include(x => x.Stages).ThenInclude(x => x.Approvers)
                .Where(x => x.IsActive && x.OrganisationBusinessEntityId == org && x.RequestTypeId == sourceType &&
                    (x.LeaveTypeId != null || sourceType != requestType) && x.LeaveRequestKind == sourceKind && x.WorkflowJobTypes.Any(j => j.JobTypeId == jobType && j.IsActive))
                .ToListAsync();
            if (legacy.Count == 1) { definition = legacy[0]; imported = true; }
        }
        var dutyType = await db.RequestTypes.AnyAsync(x => x.RequestTypeId == requestType && x.ViewUrl == "/online/duty-roster");
        var supervisorGroup = dutyType ? await db.UserGroups.Where(x => x.GroupName == "Supervisor (Team Leader)" &&
            (x.IsGlobal || x.UserOrganisation.BusinessEntityID == org)).OrderBy(x => x.IsGlobal).Select(x => x.UserGroupId).FirstOrDefaultAsync() : 0;
        return new()
        {
            JobTypeId = jobType, RequestTypeId = requestType, Version = imported ? 0 : definition?.VersionNumber ?? 0,
            ImportedFromLegacy = imported,
            SupervisorFirst = requestType != LeaveChangeRequestTypes.EarlyReturn && (definition == null || definition.Stages.Any(x => x.RequestStateId == 2)),
            HrUserGroupId = definition?.Stages.SelectMany(x => x.Approvers).FirstOrDefault(x => x.ApproverType.ToUpper() == "USERGROUP")?.ApproverReferenceId ?? 0,
            Stages = definition?.Stages.Where(x => x.IsActive).OrderBy(x => x.StageOrder).Select(x => new LeaveFlowStageDto
            {
                Name = x.StageName, StateId = x.RequestStateId,
                UserGroupId = x.Approvers.FirstOrDefault(a => a.ApproverType.ToUpper() == "USERGROUP" || a.ApproverType.ToUpper() == "SUPERVISORGROUP")?.ApproverReferenceId ?? 0,
                ApplicantSupervisorOnly = x.Approvers.Any(a => a.ApproverType.ToUpper() == "SUPERVISOR" || a.ApproverType.ToUpper() == "SUPERVISORGROUP")
            }).ToList() ?? (dutyType ? new List<LeaveFlowStageDto> { new() { Name = "Supervisor approval", StateId = 3, UserGroupId = supervisorGroup, ApplicantSupervisorOnly = true } }
                : requestType == LeaveChangeRequestTypes.EarlyReturn ? new List<LeaveFlowStageDto> { new() { Name = "HR approval", StateId = 3 } }
                : new List<LeaveFlowStageDto> { new() { Name = "Supervisor verification", StateId = 2, ApplicantSupervisorOnly = true }, new() { Name = "HR approval", StateId = 3 } })
        };
    }

    public async Task<ServiceResult> SaveAsync(LeaveFlowConfigurationDto model)
    {
        var org = await RequireOrganisationAsync();
        if (model.RequestTypeId == LeaveChangeRequestTypes.Legacy)
            return ServiceResult.Fail("Select the Early Return Request or Leave Extension Request type instead of the legacy Leave Change Request.");
        var stages = model.Stages ?? (model.SupervisorFirst
            ? new List<LeaveFlowStageDto> { new() { Name = "Supervisor verification", StateId = 2, UserGroupId = model.SupervisorUserGroupId, ApplicantSupervisorOnly = true }, new() { Name = "HR approval", StateId = 3, UserGroupId = model.HrUserGroupId } }
            : new List<LeaveFlowStageDto> { new() { Name = "HR approval", StateId = 3, UserGroupId = model.HrUserGroupId } });
        if (stages.Count is < 1 or > 20) return ServiceResult.Fail("Configure 1–20 approval stages before enabling submissions. Level 0 has no approval flow.");
        if (stages.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 150 || x.UserGroupId <= 0) ||
            stages.Select(x => x.StateId).Distinct().Count() != stages.Count)
            return ServiceResult.Fail("Each stage needs a name, User Group and a distinct processing status.");
        if (model.RequestTypeId == LeaveChangeRequestTypes.Extension && (!stages[0].ApplicantSupervisorOnly || stages.Count < 2 || stages[^1].ApplicantSupervisorOnly) ||
            model.RequestTypeId == LeaveChangeRequestTypes.EarlyReturn && (stages.Count != 1 || stages[0].ApplicantSupervisorOnly))
            return ServiceResult.Fail("Extensions require the applicant's supervisor followed by HR. Early returns require one HR stage.");
        await using var db = await factory.CreateDbContextAsync();
        var groupIds = stages.Select(x => x.UserGroupId).Distinct().ToList();
        if (!await db.JobTypes.AnyAsync(x => x.JobTypeId == model.JobTypeId && x.IsActive != false) ||
            await db.UserGroups.CountAsync(x => groupIds.Contains(x.UserGroupId) && (x.IsGlobal || x.UserOrganisation.BusinessEntityID == org)) != groupIds.Count)
            return ServiceResult.Fail("Choose an active Job Type and valid User Groups for this organisation at every stage.");
        var stateIds = stages.Select(x => x.StateId).ToList();
        if (await db.RequestStates.CountAsync(x => stateIds.Contains(x.RequestStateId) && x.IsProcessingState && !x.IsFinalState && x.RequestStateId != 1) != stages.Count)
            return ServiceResult.Fail("Every stage must use a non-final processing status, excluding Incomplete.");
        var requestType = model.RequestTypeId;
        var dutyType = await db.RequestTypes.AnyAsync(x => x.RequestTypeId == requestType && x.ViewUrl == "/online/duty-roster");
        if (dutyType && stages.Any(x => x.StateId == 10)) return ServiceResult.Fail("Pending staff completion is reserved for duty acceptance. Select a separate state for each approval stage.");
        if (requestType <= 0 || !await db.RequestTypes.AnyAsync(x => x.RequestTypeId == requestType))
            return ServiceResult.Fail("Select a valid Request Type.");
        if (await db.RequestStates.CountAsync(x => new[] { 1, 2, 3 }.Contains(x.RequestStateId) && x.IsProcessingState && !x.IsFinalState) != 3 ||
            await db.RequestStates.CountAsync(x => new[] { 4, 5, 6 }.Contains(x.RequestStateId) && x.IsFinalState) != 3)
            return ServiceResult.Fail("Configure Incomplete, Pending Verification and Pending Approval as processing states, and Approved, Cancelled and Rejected as final states first.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var previous = await db.WfWorkflowDefinitions.Include(x => x.WorkflowJobTypes)
            .Where(x => x.OrganisationBusinessEntityId == org && x.LeaveTypeId == null && x.RequestTypeId == requestType && x.LeaveRequestKind == 0 &&
                x.WorkflowJobTypes.Any(j => j.JobTypeId == model.JobTypeId)).ToListAsync();
        if (previous.Where(x => x.IsActive).Select(x => x.VersionNumber).DefaultIfEmpty().Max() != model.Version)
            return ServiceResult.Fail("This flow was changed by another administrator. Reload it before saving.");
        foreach (var old in previous) old.IsActive = false;
        var version = (await db.WfWorkflowDefinitions.Where(x => x.OrganisationBusinessEntityId == org && x.RequestTypeId == requestType)
            .Select(x => (int?)x.VersionNumber).MaxAsync() ?? 0) + 1;
        var definition = new WfWorkflowDefinition
        {
            OrganisationBusinessEntityId = org, RequestTypeId = requestType, LeaveTypeId = null, LeaveRequestKind = 0,
            Name = $"Request Type {requestType} / Job Type {model.JobTypeId}", VersionNumber = version, CreatedAt = DateTime.UtcNow,
            WorkflowJobTypes = [new() { JobTypeId = model.JobTypeId }]
        };
        foreach (var (stage, index) in stages.Select((s, i) => (s, i))) definition.Stages.Add(new WfWorkflowStage
        {
            StageOrder = index + 1, StageName = stage.Name.Trim(), RequestStateId = stage.StateId, ApprovalMode = "Any",
            Approvers = [new() { ApproverType = stage.ApplicantSupervisorOnly ? "SupervisorGroup" : "UserGroup", ApproverReferenceId = stage.UserGroupId }]
        });
        db.WfWorkflowDefinitions.Add(definition);
        var route = stages.Select(x => x.StateId).Append(4).ToArray();
        var pairs = route.Zip(route.Skip(1), (from, to) => (from, to))
            .Concat(dutyType ? new[] { (1, 10), (10, route[0]), (10, 5), (10, 6) } : new[] { (1, route[0]), (2, route[0]) })
            .Where(p => p.Item1 != p.Item2).Distinct();
        foreach (var pair in pairs)
        {
            try { await RequestTransitionPolicy.RequireAllowedAsync(db, requestType, pair.Item1, pair.Item2); }
            catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        model.Version = version;
        model.ImportedFromLegacy = false;
        return ServiceResult.Ok("Approval flow saved. It applies to new submissions; existing requests keep their workflow version.");
    }

    public static async Task<WfWorkflowDefinition> ResolveAsync(HrmTeContext db, WorkflowRequestContextDto context, CancellationToken ct = default)
    {
        var matches = await db.WfWorkflowDefinitions.AsNoTracking().Include(x => x.Stages)
            .Where(x => x.IsActive && x.RequestTypeId == context.RequestTypeId && x.OrganisationBusinessEntityId == context.OrganisationBusinessEntityId &&
                x.LeaveTypeId == null && x.LeaveRequestKind == 0 &&
                (x.EffectiveFrom == null || x.EffectiveFrom <= context.EffectiveDate) &&
                (x.EffectiveTo == null || x.EffectiveTo >= context.EffectiveDate) &&
                x.WorkflowJobTypes.Any(j => j.IsActive && j.JobTypeId == context.JobTypeId))
            .OrderByDescending(x => x.VersionNumber).ToListAsync(ct);
        if (matches.Count != 1 || !matches[0].Stages.Any(x => x.IsActive))
            throw new InvalidOperationException("Configure one approval flow for this organisation, Job Type and Request Type under Settings > Leaves Management > Request Approval Flows before submitting.");
        return matches[0];
    }
}
