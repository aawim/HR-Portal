using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class JobTypeConfigurationService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
{
    private async Task<int> RequireAdminAsync()
    {
        var user = await access.RequireContextAsync();
        if (!user.IsSuperAdministrator && !user.IsHRManager && !user.IsHeadOfOrganisation)
            throw new UnauthorizedAccessException("Only administrators, HR managers and organisation heads can configure job types and workflows.");
        int org;
        try { org = await access.GetCurrentOrganisationIdAsync(); }
        catch (InvalidOperationException) when (user.ActiveJob != null)
        {
            await using var db = await factory.CreateDbContextAsync();
            org = await db.Jobs.AsNoTracking().Where(x => x.JobId == user.ActiveJob.JobId)
                .Select(x => x.OrganisationID).SingleAsync();
        }
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("Select an organisation you can manage.");
        return org;
    }

    public async Task<JobTypeConfigurationData> LoadAsync()
    {
        var org = await RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        return new()
        {
            JobTypes = await db.JobTypes.AsNoTracking().OrderBy(x => x.TypeName).Select(x => new JobTypeConfigurationDto
                { Id = x.JobTypeId, Name = x.TypeName, DhivehiName = x.TypeNameDhivehi, IsActive = x.IsActive != false }).ToListAsync(),
            Staff = await db.Users.AsNoTracking().Where(x => x.IsActive && x.UserOrganisation != null &&
                x.UserOrganisation.BusinessEntityID == org && x.BusinessEntity != null &&
                db.Jobs.Any(j => j.IndividualID == x.BusinessEntity.BusinessEntityId && j.OrganisationID == org && j.TerminatedDate == null))
                .OrderBy(x => x.FullName).Select(x => new ConfigurationOption(x.UserId, (x.FullName ?? x.Username) + " (" + x.Username + ")")).ToListAsync(),
            RequestTypes = await db.RequestTypes.AsNoTracking().Where(x => db.LeaveTypes.Any(l => l.RequestTypeId == x.RequestTypeId))
                .OrderBy(x => x.TypeName).Select(x => new ConfigurationOption(x.RequestTypeId, x.TypeName)).ToListAsync(),
            States = await db.RequestStates.AsNoTracking().Where(x => x.IsProcessingState &&
                new[] { 2, 3, 9, 11, 12, 13, 14, 15, 16, 17 }.Contains(x.RequestStateId))
                .OrderBy(x => x.RequestStateId).Select(x => new ConfigurationOption(x.RequestStateId, x.StateName)).ToListAsync()
        };
    }

    public async Task<ServiceResult> SaveJobTypeAsync(JobTypeConfigurationDto model)
    {
        await RequireAdminAsync();
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length > 50 || model.DhivehiName?.Length > 50)
            return ServiceResult.Fail("Enter job type names of 50 characters or fewer.");
        await using var db = await factory.CreateDbContextAsync();
        if (await db.JobTypes.AnyAsync(x => x.JobTypeId != model.Id && x.TypeName == model.Name.Trim()))
            return ServiceResult.Fail("A job type with this name already exists.");
        var entity = model.Id == 0 ? new JobType() : await db.JobTypes.SingleOrDefaultAsync(x => x.JobTypeId == model.Id);
        if (entity == null) return ServiceResult.Fail("Job type was not found.");
        if (model.Id == 0) db.JobTypes.Add(entity);
        entity.TypeName = model.Name.Trim(); entity.TypeNameDhivehi = model.DhivehiName?.Trim(); entity.IsActive = model.IsActive;
        await db.SaveChangesAsync(); model.Id = entity.JobTypeId;
        return ServiceResult.Ok("Job type saved.");
    }

    public async Task<JobTypeWorkflowDto> GetWorkflowAsync(int jobTypeId, int requestTypeId)
    {
        var org = await RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        var definition = await db.WfWorkflowDefinitions.AsNoTracking().Include(x => x.Stages).ThenInclude(x => x.Approvers)
            .Where(x => x.IsActive && x.OrganisationBusinessEntityId == org && x.RequestTypeId == requestTypeId &&
                x.WorkflowJobTypes.Any(m => m.JobTypeId == jobTypeId && m.IsActive))
            .OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync();
        return new()
        {
            JobTypeId = jobTypeId, RequestTypeId = requestTypeId, Version = definition?.VersionNumber ?? 0,
            Stages = definition?.Stages.Where(x => x.IsActive).OrderBy(x => x.StageOrder).Select(x => new JobTypeStageDto
            { Name = x.StageName, StateId = x.RequestStateId, Mode = x.ApprovalMode, MinimumApprovals = x.MinimumApprovals,
                StaffUserIds = x.Approvers.Where(a => a.ApproverType == "USER" && a.ApproverReferenceId.HasValue)
                    .OrderBy(a => a.SequenceNumber).Select(a => a.ApproverReferenceId!.Value).ToList() }).ToList() ?? new()
        };
    }

    public async Task<ServiceResult> SaveWorkflowAsync(JobTypeWorkflowDto model)
    {
        var org = await RequireAdminAsync();
        var data = await LoadAsync();
        if (!data.JobTypes.Any(x => x.Id == model.JobTypeId && x.IsActive) || !data.RequestTypes.Any(x => x.Id == model.RequestTypeId))
            return ServiceResult.Fail("Select an active job type and a leave request type.");
        if (model.Stages.Count == 0 || model.Stages.Count > 20 || model.Stages.Select(x => x.StateId).Distinct().Count() != model.Stages.Count)
            return ServiceResult.Fail("Add 1–20 stages, each with a different request status.");
        foreach (var stage in model.Stages)
        {
            if (string.IsNullOrWhiteSpace(stage.Name) || stage.Name.Length > 150 || !data.States.Any(x => x.Id == stage.StateId))
                return ServiceResult.Fail("Each stage needs a name and a processing status.");
            if (stage.StaffUserIds.Count == 0 || stage.StaffUserIds.Distinct().Count() != stage.StaffUserIds.Count ||
                stage.StaffUserIds.Any(id => !data.Staff.Any(x => x.Id == id)))
                return ServiceResult.Fail("Assign at least one active staff member from this organisation to every stage.");
            if (!new[] { "Any", "All", "Sequential", "Quorum" }.Contains(stage.Mode) ||
                stage.Mode == "Quorum" && (stage.MinimumApprovals is null or < 1 || stage.MinimumApprovals > stage.StaffUserIds.Count))
                return ServiceResult.Fail("Choose a valid approval mode and quorum.");
        }
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var transitions = await db.RequestTypesAllowedRequestStateTransitions.AsNoTracking()
            .Where(x => x.RequestTypeId == model.RequestTypeId).Select(x => new { x.FromRequestStateId, x.ToRequestStateId }).ToListAsync();
        var route = new[] { 2 }.Concat(model.Stages.Select(x => x.StateId)).Append(4).ToArray();
        for (var i = 1; i < route.Length; i++)
            if (route[i - 1] != route[i] && !transitions.Any(x => x.FromRequestStateId == route[i - 1] && x.ToRequestStateId == route[i]))
                db.RequestTypesAllowedRequestStateTransitions.Add(new()
                { RequestTypeId = model.RequestTypeId, FromRequestStateId = route[i - 1], ToRequestStateId = route[i] });
        var previous = await db.WfWorkflowDefinitions.Include(x => x.WorkflowJobTypes)
            .Where(x => x.OrganisationBusinessEntityId == org && x.RequestTypeId == model.RequestTypeId &&
                x.WorkflowJobTypes.Any(m => m.JobTypeId == model.JobTypeId && m.IsActive)).ToListAsync();
        var currentVersion = previous.Where(x => x.IsActive).Select(x => x.VersionNumber).DefaultIfEmpty().Max();
        if (currentVersion != model.Version) return ServiceResult.Fail("This workflow was changed by another administrator. Reload it before saving.");
        foreach (var old in previous)
            foreach (var mapping in old.WorkflowJobTypes.Where(x => x.JobTypeId == model.JobTypeId)) mapping.IsActive = false;
        var version = (await db.WfWorkflowDefinitions.Where(x => x.OrganisationBusinessEntityId == org && x.RequestTypeId == model.RequestTypeId)
            .Select(x => (int?)x.VersionNumber).MaxAsync() ?? 0) + 1;
        db.WfWorkflowDefinitions.Add(new()
        {
            Name = $"Job type {model.JobTypeId} leave approvals", RequestTypeId = model.RequestTypeId,
            OrganisationBusinessEntityId = org, VersionNumber = version, IsActive = true, CreatedAt = DateTime.UtcNow,
            WorkflowJobTypes = new List<WfWorkflowJobType> { new() { JobTypeId = model.JobTypeId, IsActive = true } },
            Stages = model.Stages.Select((stage, index) => new WfWorkflowStage
            {
                StageOrder = index + 1, StageName = stage.Name.Trim(), RequestStateId = stage.StateId,
                ApprovalMode = stage.Mode, MinimumApprovals = stage.Mode == "Quorum" ? stage.MinimumApprovals : null,
                Approvers = stage.StaffUserIds.Select((id, order) => new WfStageApprover
                { ApproverType = "USER", ApproverReferenceId = id, SequenceNumber = order + 1, IsRequired = true }).ToList()
            }).ToList()
        });
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Version = version;
        return ServiceResult.Ok("Workflow saved. New requests will use these stages and assigned staff.");
    }
}
