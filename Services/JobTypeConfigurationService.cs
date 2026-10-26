using System.Data;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class JobTypeConfigurationService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
{
    private async Task<int> RequireOrganisationAsync()
    {
        var user = await access.RequireContextAsync();
        if(!user.IsSuperAdministrator && !user.IsHRStaff && !await access.HasPermissionAsync("Super.Admin.Manage") && !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Configuration.View"))
            throw new UnauthorizedAccessException("You do not have permission to view configuration.");
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
        var actor=await access.RequireContextAsync();
        return new()
        {
            CanManageCatalogues=actor.IsSuperAdministrator||await access.HasPermissionAsync("Super.Admin.Manage"),
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

    private async Task RequireCatalogueAsync()
    {
        var actor=await access.RequireContextAsync();
        if(!actor.IsSuperAdministrator&&!await access.HasPermissionAsync("Super.Admin.Manage"))throw new UnauthorizedAccessException("Only Super Admin can change shared Job Type and Request Type catalogues.");
    }

    public async Task<ServiceResult> SaveJobTypeAsync(JobTypeConfigurationDto model)
    {
        await RequireOrganisationAsync();await RequireCatalogueAsync();
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length > 50 || model.DhivehiName?.Length > 50)
            return ServiceResult.Fail("Enter job type names of 50 characters or fewer.");
        await using var db = await factory.CreateDbContextAsync();
        if (await db.JobTypes.AnyAsync(x => x.JobTypeId != model.Id && x.TypeName == model.Name.Trim()))
            return ServiceResult.Fail("A job type with this name already exists.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity = model.Id == 0 ? new JobType() : await db.JobTypes.SingleOrDefaultAsync(x => x.JobTypeId == model.Id);
        if (entity == null) return ServiceResult.Fail("Job type was not found.");
        if (model.Id == 0) db.JobTypes.Add(entity);
        var audit=await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.USERS_UPDATE,$"Shared Job Type {model.Id}: {entity.TypeName} → {model.Name.Trim()}.");
        entity.OperationLogId=audit.OperationLogId;
        entity.TypeName = model.Name.Trim(); entity.TypeNameDhivehi = model.DhivehiName?.Trim(); entity.IsActive = model.IsActive;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.JobTypeId;
        return ServiceResult.Ok("Job type saved.");
    }

    public async Task<ServiceResult> SaveRequestTypeAsync(JobTypeConfigurationDto model)
    {
        await RequireOrganisationAsync();await RequireCatalogueAsync();
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
        await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.USERS_UPDATE,$"Shared Request Type {model.Id}: {entity.TypeName} → {model.Name.Trim()}.");
        entity.TypeName = model.Name.Trim(); entity.SequenceNumberTypeId = model.SequenceNumberTypeId;
        entity.ViewUrl = viewUrl;
        await db.SaveChangesAsync(); await tx.CommitAsync(); model.Id = entity.RequestTypeId;
        return ServiceResult.Ok("Request type saved.");
    }

    public async Task<ServiceResult> DeleteRequestTypeAsync(int id)
    {
        var org = await RequireOrganisationAsync();await RequireCatalogueAsync();
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
        await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.USERS_UPDATE,$"Delete unused shared Request Type {entity.RequestTypeId} ({entity.TypeName}).");
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

    public Task<ServiceResult> SaveWorkflowAsync(JobTypeWorkflowDto model, bool requestTypeDefault = false)
        => Task.FromResult(ServiceResult.Fail("Configure approval flows under Settings > Configurations > Request Approval Flows."));
}
