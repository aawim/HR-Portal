using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class JobTypeConfigurationService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
{
    private async Task<int> RequireOrganisationAsync()
    {
        var user = await access.RequireContextAsync();
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

    public async Task<JobTypeConfigurationData> LoadAsync(bool requestTypes = false)
    {
        var org = await RequireOrganisationAsync();
        await using var db = await factory.CreateDbContextAsync();
        return new()
        {
            JobTypes = requestTypes
                ? await db.RequestTypes.AsNoTracking().OrderBy(x => x.TypeName).Select(x => new JobTypeConfigurationDto
                    { Id = x.RequestTypeId, Name = x.TypeName, SequenceNumberTypeId = x.SequenceNumberTypeId, ViewUrl = x.ViewUrl }).ToListAsync()
                : await db.JobTypes.AsNoTracking().OrderBy(x => x.TypeName).Select(x => new JobTypeConfigurationDto
                { Id = x.JobTypeId, Name = x.TypeName, DhivehiName = x.TypeNameDhivehi, IsActive = x.IsActive != false }).ToListAsync(),
            Staff = await db.Users.AsNoTracking().Where(x => x.IsActive && x.UserOrganisation != null &&
                x.UserOrganisation.BusinessEntityID == org && x.BusinessEntity != null &&
                db.Jobs.Any(j => j.IndividualID == x.BusinessEntity.BusinessEntityId && j.OrganisationID == org && j.TerminatedDate == null))
                .OrderBy(x => x.FullName).Select(x => new ConfigurationOption(x.UserId, (x.FullName ?? x.Username) + " (" + x.Username + ")")).ToListAsync(),
            RequestTypes = await db.RequestTypes.AsNoTracking().Where(x => requestTypes || db.LeaveTypes.Any(l => l.RequestTypeId == x.RequestTypeId))
                .OrderBy(x => x.TypeName).Select(x => new ConfigurationOption(x.RequestTypeId, x.TypeName)).ToListAsync(),
            NumberingTypes = await db.SequenceNumberTypes.AsNoTracking().OrderBy(x => x.TypeName)
                .Select(x => new ConfigurationOption(x.SequenceNumberTypeId, x.TypeName ?? x.Prefix ?? "Numbering sequence")).ToListAsync(),
            States = await db.RequestStates.AsNoTracking().Where(x => x.IsProcessingState &&
                new[] { 2, 3, 9, 11, 12, 13, 14, 15, 16, 17 }.Contains(x.RequestStateId))
                .OrderBy(x => x.RequestStateId).Select(x => new ConfigurationOption(x.RequestStateId, x.StateName)).ToListAsync()
        };
    }

    public async Task<ServiceResult> SaveJobTypeAsync(JobTypeConfigurationDto model)
    {
        await RequireOrganisationAsync();
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

    public async Task<ServiceResult> SaveRequestTypeAsync(JobTypeConfigurationDto model)
    {
        await RequireOrganisationAsync();
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length > 500 || model.ViewUrl?.Length > 1000)
            return ServiceResult.Fail("Enter a name of up to 500 characters and a view URL of up to 1000 characters.");
        var viewUrl = string.IsNullOrWhiteSpace(model.ViewUrl) ? null : model.ViewUrl.Trim();
        if (viewUrl != null && (viewUrl.StartsWith("//") || viewUrl.Contains('\\') || viewUrl.Any(char.IsControl) ||
            !Uri.TryCreate(viewUrl, UriKind.RelativeOrAbsolute, out var parsedUrl) ||
            parsedUrl.IsAbsoluteUri && parsedUrl.Scheme != Uri.UriSchemeHttp && parsedUrl.Scheme != Uri.UriSchemeHttps))
            return ServiceResult.Fail("Enter an application path (for example Settings/Users/Account/), an HTTP/HTTPS URL, or leave View URL blank.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.RequestTypes.AnyAsync(x => x.RequestTypeId != model.Id && x.TypeName == model.Name.Trim()))
            return ServiceResult.Fail("A request type with this name already exists.");
        if (!model.SequenceNumberTypeId.HasValue || !await db.SequenceNumberTypes.AnyAsync(x => x.SequenceNumberTypeId == model.SequenceNumberTypeId))
            return ServiceResult.Fail("Select a request numbering sequence.");
        var entity = model.Id == 0 ? new RequestType() : await db.RequestTypes.SingleOrDefaultAsync(x => x.RequestTypeId == model.Id);
        if (entity == null) return ServiceResult.Fail("Request type was not found.");
        if (model.Id == 0) db.RequestTypes.Add(entity);
        entity.TypeName = model.Name.Trim(); entity.SequenceNumberTypeId = model.SequenceNumberTypeId;
        entity.ViewUrl = viewUrl;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.RequestTypeId;
        return ServiceResult.Ok("Request type saved.");
    }

    public async Task<ServiceResult> DeleteRequestTypeAsync(int id)
    {
        var org = await RequireOrganisationAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity = await db.RequestTypes.SingleOrDefaultAsync(x => x.RequestTypeId == id);
        if (entity == null) return ServiceResult.Fail("Request type was not found.");
        if (entity.IsSystemType) return ServiceResult.Fail("System request types cannot be deleted.");
        if (await db.Requests.AnyAsync(x => x.RequestTypeId == id) || await db.LeaveTypes.AnyAsync(x => x.RequestTypeId == id))
            return ServiceResult.Fail("This request type is used by requests or leave types and cannot be deleted.");
        var definitions = db.WfWorkflowDefinitions.Where(x => x.RequestTypeId == id);
        if (await definitions.AnyAsync(x => x.OrganisationBusinessEntityId != org || x.OrganisationBusinessEntityId == null || x.RequestInstances.Any()))
            return ServiceResult.Fail("This request type has workflow history or workflows outside your organisation and cannot be deleted.");
        var stages = db.WfWorkflowStages.Where(x => x.WorkflowDefinition.RequestTypeId == id);
        await db.WfStageApprovers.Where(x => stages.Select(s => s.WorkflowStageId).Contains(x.WorkflowStageId)).ExecuteDeleteAsync();
        await stages.ExecuteDeleteAsync();
        await db.WfWorkflowJobTypes.Where(x => x.WorkflowDefinition.RequestTypeId == id).ExecuteDeleteAsync();
        await definitions.ExecuteDeleteAsync();
        await db.RequestTypesAllowedRequestStateTransitions.Where(x => x.RequestTypeId == id).ExecuteDeleteAsync();
        await db.RequestTypesStatesRequiredRoles.Where(x => x.RequestTypeId == id).ExecuteDeleteAsync();
        await db.RequestTypesStatesRequiredRolesForProcessings.Where(x => x.RequestTypeId == id).ExecuteDeleteAsync();
        await db.RequestTypeSpecificDocumentTypes.Where(x => x.RequestTypeId == id).ExecuteDeleteAsync();
        await db.Set<Dictionary<string, object>>("RequestTypeSpecificCheckListItem")
            .Where(x => EF.Property<int>(x, "RequestTypeId") == id).ExecuteDeleteAsync();
        db.RequestTypes.Remove(entity);
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return ServiceResult.Ok("Request type and its unused workflow configuration deleted.");
    }

    public async Task<JobTypeWorkflowDto> GetWorkflowAsync(int jobTypeId, int requestTypeId, bool requestTypeDefault = false)
    {
        var org = await RequireOrganisationAsync();
        await using var db = await factory.CreateDbContextAsync();
        var definition = await db.WfWorkflowDefinitions.AsNoTracking().Include(x => x.Stages).ThenInclude(x => x.Approvers)
            .Where(x => x.LeaveRequestKind == null && x.IsActive && x.OrganisationBusinessEntityId == org && x.RequestTypeId == requestTypeId &&
                (requestTypeDefault ? !x.WorkflowJobTypes.Any() : x.WorkflowJobTypes.Any(m => m.JobTypeId == jobTypeId && m.IsActive)))
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

    public async Task<ServiceResult> SaveWorkflowAsync(JobTypeWorkflowDto model, bool requestTypeDefault = false)
    {
        var org = await RequireOrganisationAsync();
        var data = await LoadAsync(requestTypeDefault);
        if ((!requestTypeDefault && !data.JobTypes.Any(x => x.Id == model.JobTypeId && x.IsActive)) || !data.RequestTypes.Any(x => x.Id == model.RequestTypeId))
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
        var route = new[] { 2 }.Concat(model.Stages.Select(x => x.StateId)).Append(4).ToArray();
        for (var i = 1; i < route.Length; i++)
            if (route[i - 1] != route[i])
                try { await RequestTransitionPolicy.RequireAllowedAsync(db, model.RequestTypeId, route[i - 1], route[i]); }
                catch (InvalidOperationException ex) { return ServiceResult.Fail(ex.Message); }
        var previous = await db.WfWorkflowDefinitions.Include(x => x.WorkflowJobTypes)
            .Where(x => x.LeaveRequestKind == null && x.OrganisationBusinessEntityId == org && x.RequestTypeId == model.RequestTypeId &&
                (requestTypeDefault ? !x.WorkflowJobTypes.Any() : x.WorkflowJobTypes.Any(m => m.JobTypeId == model.JobTypeId && m.IsActive))).ToListAsync();
        var currentVersion = previous.Where(x => x.IsActive).Select(x => x.VersionNumber).DefaultIfEmpty().Max();
        if (currentVersion != model.Version) return ServiceResult.Fail("This workflow was changed by another administrator. Reload it before saving.");
        foreach (var old in previous)
        {
            if (requestTypeDefault) old.IsActive = false;
            foreach (var mapping in old.WorkflowJobTypes.Where(x => x.JobTypeId == model.JobTypeId)) mapping.IsActive = false;
        }
        var version = (await db.WfWorkflowDefinitions.Where(x => x.OrganisationBusinessEntityId == org && x.RequestTypeId == model.RequestTypeId)
            .Select(x => (int?)x.VersionNumber).MaxAsync() ?? 0) + 1;
        db.WfWorkflowDefinitions.Add(new()
        {
            Name = requestTypeDefault ? $"Request type {model.RequestTypeId} approvals" : $"Job type {model.JobTypeId} leave approvals", RequestTypeId = model.RequestTypeId,
            OrganisationBusinessEntityId = org, VersionNumber = version, IsActive = true, CreatedAt = DateTime.UtcNow,
            WorkflowJobTypes = requestTypeDefault ? new List<WfWorkflowJobType>() : new List<WfWorkflowJobType> { new() { JobTypeId = model.JobTypeId, IsActive = true } },
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
