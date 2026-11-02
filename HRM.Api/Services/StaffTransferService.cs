using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Authorization;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.StaffTransfer;
using HRM.DTOs.UserContext;
using HRM.Enums;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class StaffTransferService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService logs,
    IRequestService requests,
    IStaffTransferActivationService activation,
    IWorkflowEngine? workflow = null)
    : global::HRM.Services.Contracts.StaffTransferServiceContracts,
      global::HRM.Services.Contracts.StaffTransferServiceContracts.IOperations
{
    private static DateTime TodayMaldives => DateTime.UtcNow.AddHours(5).Date;

    private async Task<(UserContextDto Actor, int Org, bool IsHrOrAdmin)> ScopeAsync(int? selectedOrgId = null)
    {
        var actor = await access.RequireContextAsync();
        var currentOrg = await access.GetCurrentOrganisationIdAsync();
        var isSuper = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var org = isSuper && selectedOrgId is > 0 ? selectedOrgId.Value : currentOrg;
        if (org <= 0 || (!isSuper && !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Select an organisation you can access.");

        var isHrOrAdmin = isSuper || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager ||
                          actor.IsOrganisationAdmin || await access.HasPermissionAsync("Admin") ||
                          await access.HasPermissionAsync(Permissions.StaffTransferCreate) ||
                          await access.HasPermissionAsync(Permissions.StaffTransferApprove);
        return (actor, org, isHrOrAdmin);
    }

    private static string FullName(Individual i) =>
        string.Join(" ", new[] { i.FirstNameEnglish, i.MiddleNameEnglish, i.LastNameEnglish }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));

    private static string TransferTypeLabel(int transferTypeId) => (StaffTransferType)transferTypeId switch
    {
        StaffTransferType.InternalDepartmental => "Internal Departmental Transfer",
        StaffTransferType.CrossStructureReassignment => "Cross-Structure Reassignment",
        StaffTransferType.SameJobPositionTransfer => "Position Transfer (Same Job)",
        StaffTransferType.NewAppointmentTransfer => "New Employment Appointment Transfer",
        StaffTransferType.CrossOrganisationTransfer => "Cross-Organisation Transfer",
        _ => "Internal Transfer"
    };

    public async Task<StaffTransferDashboardData> LoadDashboardAsync(int? organisationId = null, string? statusFilter = null)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();

        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == org)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? "Organisation";

        var canInitiate = isHrOrAdmin || await IsDepartmentAuthorizedOfficerAsync(db, actor, org, null);
        var canApproveReceiving = isHrOrAdmin || await access.HasPermissionAsync(Permissions.StaffTransferApprove);
        var canActivate = isHrOrAdmin || await access.HasPermissionAsync(Permissions.StaffTransferActivate);
        var canProvision = isHrOrAdmin || await access.HasPermissionAsync(Permissions.StaffTransferProvisionAccess);

        var baseQuery = db.StaffTransfers.AsNoTracking()
            .Where(t => t.FromOrganisationId == org || t.ToOrganisationId == org || t.IndividualId == actor.IndividualId);

        if (!isHrOrAdmin && !canInitiate && !canApproveReceiving)
        {
            baseQuery = baseQuery.Where(t =>
                t.IndividualId == actor.IndividualId ||
                t.CurrentSupervisorIndividualId == actor.IndividualId ||
                t.CurrentHodIndividualId == actor.IndividualId ||
                t.RequestedByUserId == actor.UserId);
        }

        var allStatuses = await baseQuery
            .Select(t => new
            {
                t.StaffTransferId,
                t.Status,
                t.RequestedDate,
                DoorPending = t.DoorAccessChanges.Any(d =>
                    d.ProvisioningState != StaffTransferDoorAccessStates.StateProvisioned &&
                    d.ProvisioningState != StaffTransferDoorAccessStates.StateVerified &&
                    d.ProvisioningState != StaffTransferDoorAccessStates.StateRevoked)
            })
            .ToListAsync();

        var overdueCutoff = DateTime.UtcNow.AddDays(-3);
        var data = new StaffTransferDashboardData
        {
            OrganisationId = org,
            OrganisationName = orgName,
            CanInitiate = canInitiate,
            CanApproveReceiving = canApproveReceiving,
            CanActivate = canActivate,
            CanProvisionSecurity = canProvision,
            DraftCount = allStatuses.Count(x => x.Status is StaffTransferStatuses.Draft or StaffTransferStatuses.CorrectionRequested),
            PendingConfirmationCount = allStatuses.Count(x => x.Status is StaffTransferStatuses.Submitted or StaffTransferStatuses.PendingStaffConfirmation),
            PendingApprovalCount = allStatuses.Count(x => x.Status == StaffTransferStatuses.PendingReceivingApproval),
            ApprovedScheduledCount = allStatuses.Count(x => x.Status is StaffTransferStatuses.Approved or StaffTransferStatuses.Scheduled or StaffTransferStatuses.ActivationInProgress),
            ActiveCount = allStatuses.Count(x => x.Status == StaffTransferStatuses.Active),
            FailedActivationCount = allStatuses.Count(x => x.Status == StaffTransferStatuses.ActivationFailed),
            OverdueConfirmationCount = allStatuses.Count(x =>
                x.Status is StaffTransferStatuses.Submitted or StaffTransferStatuses.PendingStaffConfirmation &&
                x.RequestedDate <= overdueCutoff),
            PendingDoorProvisioningCount = allStatuses.Count(x =>
                x.Status is StaffTransferStatuses.Approved or StaffTransferStatuses.Scheduled or StaffTransferStatuses.Active &&
                x.DoorPending)
        };

        var filteredQuery = baseQuery;
        if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
            filteredQuery = filteredQuery.Where(t => t.Status == statusFilter);

        var rows = await filteredQuery
            .OrderByDescending(t => t.UpdatedDate)
            .ThenByDescending(t => t.StaffTransferId)
            .Take(200)
            .Select(t => new
            {
                t.StaffTransferId,
                t.RequestId,
                ReferenceNumber = t.Request != null ? t.Request.ReferenceNumber : null,
                t.IndividualId,
                t.EmployeeNumberSnapshot,
                t.FullNameSnapshot,
                t.CurrentJobId,
                ExistingPositionTitle = t.JobDetail != null ? t.JobDetail.ExistingPositionTitleSnapshot : "",
                t.FromOrganisationId,
                FromOrgName = t.FromOrganisation.OrganisationName,
                t.FromOrganisationStructureId,
                FromDeptName = t.FromOrganisationStructure.Name,
                t.ToOrganisationId,
                ToOrgName = t.ToOrganisation.OrganisationName,
                t.ToOrganisationStructureId,
                ToDeptName = t.ToOrganisationStructure.Name,
                DestinationPositionTitle = t.JobDetail != null ? t.JobDetail.DestinationPositionTitleSnapshot : null,
                t.TransferTypeId,
                AppointmentMode = t.JobDetail != null ? t.JobDetail.TransferAppointmentMode : TransferAppointmentModes.OrganisationalReassignment,
                t.RequestedDate,
                t.EffectiveDate,
                t.ReportingDate,
                t.Status,
                t.CurrentVersion,
                t.ConfirmedVersion,
                HasSupComment = t.Comments.Any(c => c.ParticipantRole == StaffTransferParticipantRoles.CurrentSupervisor),
                HasHodComment = t.Comments.Any(c => c.ParticipantRole == StaffTransferParticipantRoles.CurrentHOD),
                DoorTotal = t.DoorAccessChanges.Count,
                DoorDone = t.DoorAccessChanges.Count(d =>
                    d.ProvisioningState == StaffTransferDoorAccessStates.StateProvisioned ||
                    d.ProvisioningState == StaffTransferDoorAccessStates.StateVerified ||
                    d.ProvisioningState == StaffTransferDoorAccessStates.StateRevoked),
                t.FinalApprovedAt,
                t.ActivatedAt
            })
            .ToListAsync();

        data.Items = rows.Select(r => new StaffTransferRowDto(
            r.StaffTransferId,
            r.RequestId,
            r.ReferenceNumber ?? $"STR-{r.StaffTransferId:D4}",
            r.IndividualId,
            r.EmployeeNumberSnapshot,
            r.FullNameSnapshot,
            r.CurrentJobId,
            r.ExistingPositionTitle,
            r.FromOrganisationId,
            r.FromOrgName,
            r.FromOrganisationStructureId,
            r.FromDeptName,
            r.ToOrganisationId,
            r.ToOrgName,
            r.ToOrganisationStructureId,
            r.ToDeptName,
            r.DestinationPositionTitle,
            r.TransferTypeId,
            TransferTypeLabel(r.TransferTypeId),
            r.AppointmentMode,
            r.RequestedDate,
            r.EffectiveDate,
            r.ReportingDate,
            r.Status,
            r.CurrentVersion,
            r.ConfirmedVersion,
            r.HasSupComment,
            r.HasHodComment,
            r.DoorTotal == 0 ? "None" : $"{r.DoorDone}/{r.DoorTotal} completed",
            r.FinalApprovedAt,
            r.ActivatedAt)).ToList();

        return data;
    }

    public async Task<StaffTransferFormLookupDto> LoadFormLookupsAsync(
        int? organisationId = null,
        int? individualId = null,
        int? destinationOrganisationId = null)
    {
        var (actor, org, _) = await ScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var today = TodayMaldives;
        var destOrg = destinationOrganisationId is > 0 ? destinationOrganisationId.Value : org;

        var lookup = new StaffTransferFormLookupDto
        {
            CurrentOrganisationId = org,
            CurrentOrganisationName = await db.Organisations.AsNoTracking()
                .Where(o => o.BusinessEntityID == org)
                .Select(o => o.OrganisationName)
                .FirstOrDefaultAsync() ?? ""
        };

        var activeJobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == org && j.JobStateId == SharedConfig.JobStates.APPROVED &&
                        (!j.TerminatedDate.HasValue || j.TerminatedDate.Value > today) &&
                        (!individualId.HasValue || j.IndividualID == individualId.Value))
            .OrderBy(j => j.Individual.FirstNameEnglish)
            .Select(j => new
            {
                j.IndividualID,
                j.JobId,
                j.Individual.FirstNameEnglish,
                j.Individual.MiddleNameEnglish,
                j.Individual.LastNameEnglish,
                EmployeeNo = j.Staff.EmployeeNumber,
                DeptName = j.OrganisationStructure.Name
            })
            .Take(300)
            .ToListAsync();

        lookup.Employees = activeJobs
            .GroupBy(x => x.IndividualID)
            .Select(g =>
            {
                var first = g.First();
                var fullName = string.Join(" ", new[] { first.FirstNameEnglish, first.MiddleNameEnglish, first.LastNameEnglish }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                return new StaffTransferLookupOption(
                    first.IndividualID,
                    $"{fullName} ({first.EmployeeNo})",
                    $"Job #{first.JobId} · {first.DeptName}");
            })
            .ToList();

        lookup.Organisations = await db.Organisations.AsNoTracking()
            .Where(o => actor.IsSuperAdministrator || o.BusinessEntityID == org || o.BusinessEntityID == destOrg)
            .OrderBy(o => o.OrganisationName)
            .Select(o => new StaffTransferLookupOption(o.BusinessEntityID, o.OrganisationName, null))
            .ToListAsync();

        lookup.Departments = await db.OrganisationStructures.AsNoTracking()
            .Where(s => (s.OrganisationBusinessEntityID == org || s.OrganisationBusinessEntityID == destOrg) &&
                        s.OrganisationStructureStateId == 3)
            .OrderBy(s => s.Name)
            .Select(s => new StaffTransferDepartmentOption(
                s.OrganisationStructureId,
                s.OrganisationBusinessEntityID,
                s.Name,
                s.OrganisationStructureTypeId))
            .ToListAsync();

        lookup.JobTypes = await db.JobTypes.AsNoTracking()
            .Where(jt => jt.IsActive != false)
            .OrderBy(jt => jt.TypeName)
            .Select(jt => new StaffTransferLookupOption(jt.JobTypeId, jt.TypeName, null))
            .ToListAsync();

        var vacantIds = (await PositionVacancy.Vacant(db, today).Select(p => p.PositionId).ToListAsync()).ToHashSet();
        var rawPositions = await db.Positions.AsNoTracking()
            .Where(p => p.IsActive != false && (p.IsGlobal || p.OrganisationId == destOrg || p.OrganisationId == org))
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.PositionId,
                p.OrganisationId,
                p.Name,
                p.PositionFrameworkId,
                p.PositionMatrixId
            })
            .ToListAsync();

        lookup.Positions = rawPositions.Select(p => new StaffTransferPositionOption(
            p.PositionId,
            p.OrganisationId,
            p.Name,
            p.PositionFrameworkId,
            p.PositionMatrixId,
            vacantIds.Contains(p.PositionId),
            null,
            null)).ToList();

        lookup.PositionFrameworks = await db.PositionFrameworks.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.FrameworkId)
            .Select(f => new StaffTransferLookupOption(f.FrameworkId, $"{f.Code} — {f.Name}", null))
            .ToListAsync();

        var matrices = await (from m in db.PositionMatrices.AsNoTracking()
                              join jm in db.FrameworkJobMatrices.AsNoTracking() on m.JobMatrixId equals jm.JobMatrixId
                              join jt in db.FrameworkJobTitles.AsNoTracking() on m.JobTitleId equals jt.JobTitleId
                              where (m.OrganisationId == destOrg || m.OrganisationId == org) && m.IsActive && jm.IsActive && jt.IsActive
                              select new
                              {
                                  m.PositionMatrixId,
                                  jm.FrameworkId,
                                  m.JobMatrixId,
                                  Label = jt.Name + " · " + jm.MatrixCode,
                                  jm.GradeCode,
                                  m.ApprovedSlots
                              }).ToListAsync();

        lookup.PositionMatrices = matrices.Select(m => new StaffTransferMatrixOption(
            m.PositionMatrixId,
            m.FrameworkId,
            m.JobMatrixId,
            m.Label,
            m.GradeCode,
            m.ApprovedSlots,
            m.ApprovedSlots)).ToList();

        var matrixIds = matrices.Select(m => m.JobMatrixId).Distinct().ToList();
        var matrixSalById = await db.FrameworkJobMatrices.AsNoTracking()
            .Where(jm => matrixIds.Contains(jm.JobMatrixId))
            .ToDictionaryAsync(jm => jm.JobMatrixId, jm => jm.BasicSalary);

        var rawSteps = await db.FrameworkJobMatrixSteps.AsNoTracking()
            .Where(s => matrixIds.Contains(s.JobMatrixId) && s.IsActive)
            .OrderBy(s => s.JobMatrixId).ThenBy(s => s.StepNumber)
            .ToListAsync();

        lookup.MatrixSteps = rawSteps.Select(s => new StaffTransferStepOption(
            s.JobMatrixStepId,
            s.JobMatrixId,
            s.StepNumber,
            s.BasicSalaryOverride ?? matrixSalById.GetValueOrDefault(s.JobMatrixId),
            s.JobAllowance)).ToList();

        lookup.PlanningProviders = await db.PlanningProviders.AsNoTracking()
            .Where(p => p.IsActive && (p.OrganisationBusinessEntityID == destOrg || p.OrganisationBusinessEntityID == org))
            .OrderBy(p => p.Name)
            .Select(p => new StaffTransferLookupOption(p.PlanningProviderId, p.Name, p.Code))
            .ToListAsync();

        lookup.WorkTemplates = await db.WorkTemplates.AsNoTracking()
            .Where(w => w.IsActive && (w.IsGlobal || w.OrganisationBusinessEntityId == destOrg || w.OrganisationBusinessEntityId == org))
            .OrderBy(w => w.Name)
            .Select(w => new StaffTransferLookupOption(w.WorkTemplateId, w.Name, null))
            .ToListAsync();

        lookup.AttendanceDevices = await db.AttendanceDevices.AsNoTracking()
            .OrderBy(d => d.Location)
            .Take(100)
            .Select(d => new StaffTransferLookupOption(d.AttendanceDeviceId, d.Location ?? $"Device #{d.AttendanceDeviceId} ({d.Ipaddress})", null))
            .ToListAsync();

        lookup.Supervisors = lookup.Employees;

        lookup.Roles = await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Take(200)
            .Select(r => new StaffTransferLookupOption(r.RoleID, r.Name, r.RoleKey))
            .ToListAsync();

        lookup.UserGroups = await db.UserGroups.AsNoTracking()
            .OrderBy(g => g.GroupName)
            .Take(100)
            .Select(g => new StaffTransferLookupOption(g.UserGroupId, g.GroupName, null))
            .ToListAsync();

        lookup.SecurityOfficers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.LocalUserStateId == 1 &&
                        (u.UserOrganisation!.BusinessEntityID == destOrg || u.UserOrganisation.BusinessEntityID == org))
            .OrderBy(u => u.FullName ?? u.Username)
            .Take(100)
            .Select(u => new StaffTransferLookupOption(u.UserId, u.FullName ?? u.Username, u.Email))
            .ToListAsync();

        return lookup;
    }

    public async Task<StaffTransferDetailDto> LoadDetailAsync(
        int? staffTransferId = null,
        int? individualId = null,
        int? jobId = null)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        if (staffTransferId is > 0)
            return await LoadExistingTransferDetailAsync(db, staffTransferId.Value, actor, org, isHrOrAdmin);

        var targetIndividualId = individualId ?? actor.IndividualId;
        return await BuildNewDraftFromDatabaseAsync(db, targetIndividualId, jobId, actor, org, isHrOrAdmin);
    }

    private async Task<StaffTransferDetailDto> LoadExistingTransferDetailAsync(
        HrmTeContext db,
        int staffTransferId,
        UserContextDto actor,
        int org,
        bool isHrOrAdmin)
    {
        var t = await db.StaffTransfers.AsNoTracking()
            .Include(x => x.Request)
            .Include(x => x.FromOrganisation)
            .Include(x => x.FromOrganisationStructure)
            .Include(x => x.ToOrganisation)
            .Include(x => x.ToOrganisationStructure)
            .Include(x => x.RequestedByUser)
            .Include(x => x.FinalApprovedByUser)
            .Include(x => x.CurrentSupervisorIndividual)
            .Include(x => x.CurrentHodIndividual)
            .Include(x => x.JobDetail).ThenInclude(j => j!.DestinationPosition)
            .Include(x => x.WorkDetail).ThenInclude(w => w!.ProposedWorkTemplate)
            .Include(x => x.WorkDetail).ThenInclude(w => w!.CurrentWorkTemplate)
            .Include(x => x.WorkDetail).ThenInclude(w => w!.ProposedPlanningProvider)
            .Include(x => x.WorkDetail).ThenInclude(w => w!.CurrentPlanningProvider)
            .Include(x => x.ReportingDetail).ThenInclude(r => r!.ReportingToIndividual)
            .Include(x => x.AccessChanges).ThenInclude(a => a.Role)
            .Include(x => x.AccessChanges).ThenInclude(a => a.UserGroup)
            .Include(x => x.DoorAccessChanges).ThenInclude(d => d.SecurityOfficerUser)
            .Include(x => x.Comments).ThenInclude(c => c.AuthorUser)
            .Include(x => x.Confirmations).ThenInclude(c => c.ConfirmedByUser)
            .Include(x => x.ActivationLogs)
            .Include(x => x.Documents)
            .SingleOrDefaultAsync(x => x.StaffTransferId == staffTransferId)
            ?? throw new InvalidOperationException("Staff transfer request was not found.");

        var isEmployee = actor.IndividualId == t.IndividualId;
        var isSupervisor = t.CurrentSupervisorIndividualId == actor.IndividualId;
        var isHod = t.CurrentHodIndividualId == actor.IndividualId;
        var isSendingOfficer = isHrOrAdmin || t.RequestedByUserId == actor.UserId ||
                               await IsDepartmentAuthorizedOfficerAsync(db, actor, t.FromOrganisationId, t.FromOrganisationStructureId);
        var isReceivingApprover = isHrOrAdmin ||
                                  await IsReceivingDepartmentApproverAsync(db, actor, t.ToOrganisationId, t.ToOrganisationStructureId);

        if (!isEmployee && !isSupervisor && !isHod && !isSendingOfficer && !isReceivingApprover)
            throw new UnauthorizedAccessException("You do not have permission to view this staff transfer.");

        var canSeeProtectedFields = isHrOrAdmin || isSendingOfficer || isReceivingApprover || isEmployee;
        var canSeeSalary = isHrOrAdmin || await access.HasPermissionAsync(Permissions.StaffTransferApprove);
        var canExportRestricted = isHrOrAdmin || await access.HasPermissionAsync(Permissions.StaffTransferExportRestrictedPdf);

        var job = t.JobDetail ?? new StaffTransferJobDetail();
        var work = t.WorkDetail ?? new StaffTransferWorkDetail();
        var rep = t.ReportingDetail ?? new StaffTransferReportingDetail();

        var dto = new StaffTransferDetailDto
        {
            StaffTransferId = t.StaffTransferId,
            RequestId = t.RequestId,
            ReferenceNumber = t.Request?.ReferenceNumber ?? $"STR-{t.StaffTransferId:D4}",
            Status = t.Status,
            IsOpen = t.IsOpen,
            CurrentVersion = t.CurrentVersion,
            ConfirmedVersion = t.ConfirmedVersion,
            RequestedByUserId = t.RequestedByUserId,
            RequestedByName = t.RequestedByUser.FullName ?? t.RequestedByUser.Username,
            RequestedDate = t.RequestedDate,
            FinalApprovedByUserId = t.FinalApprovedByUserId,
            FinalApprovedByName = t.FinalApprovedByUser?.FullName ?? t.FinalApprovedByUser?.Username,
            FinalApprovedAt = t.FinalApprovedAt,
            FinalDecisionRemarks = t.FinalDecisionRemarks,
            ActivatedAt = t.ActivatedAt,

            CanEdit = isSendingOfficer && t.Status is StaffTransferStatuses.Draft or StaffTransferStatuses.CorrectionRequested or StaffTransferStatuses.Submitted or StaffTransferStatuses.PendingStaffConfirmation,
            CanSubmit = isSendingOfficer && t.Status is StaffTransferStatuses.Draft or StaffTransferStatuses.CorrectionRequested,
            CanCommentAsSupervisorOrHod = (isSupervisor || isHod || isSendingOfficer || isReceivingApprover) && t.IsOpen,
            CanConfirmAsEmployee = isEmployee && t.Status is StaffTransferStatuses.Submitted or StaffTransferStatuses.PendingStaffConfirmation,
            CanApproveAsReceiving = isReceivingApprover && t.RequestedByUserId != actor.UserId &&
                                    t.Status == StaffTransferStatuses.PendingReceivingApproval &&
                                    t.ConfirmedVersion == t.CurrentVersion,
            CanActivate = (isHrOrAdmin || await access.HasPermissionAsync(Permissions.StaffTransferActivate)) &&
                          t.Status is StaffTransferStatuses.Approved or StaffTransferStatuses.Scheduled or StaffTransferStatuses.ActivationFailed &&
                          t.EffectiveDate.Date <= TodayMaldives,
            CanExportRestrictedPdf = canExportRestricted,

            Staff = new StaffTransferStaffTabDto
            {
                IndividualId = t.IndividualId,
                FullName = t.FullNameSnapshot,
                EmployeeNumber = t.EmployeeNumberSnapshot,
                IdCardNumber = canSeeProtectedFields ? t.ProtectedIdCardSnapshot : "••••••••",
                CurrentOrganisationId = t.FromOrganisationId,
                CurrentOrganisationName = t.FromOrganisation.OrganisationName,
                CurrentOrganisationStructureId = t.FromOrganisationStructureId,
                CurrentDepartmentName = t.FromOrganisationStructure.Name,
                CurrentEmploymentStatus = t.CurrentEmploymentStatusSnapshot,
                DateJoined = t.DateJoinedSnapshot,
                OfficialEmail = t.OfficialEmailSnapshot,
                OfficialPhone = t.OfficialPhoneSnapshot,
                CurrentSupervisorIndividualId = t.CurrentSupervisorIndividualId,
                CurrentSupervisorName = t.CurrentSupervisorIndividual != null ? FullName(t.CurrentSupervisorIndividual) : null,
                CurrentHodIndividualId = t.CurrentHodIndividualId,
                CurrentHodName = t.CurrentHodIndividual != null ? FullName(t.CurrentHodIndividual) : null,
                EmploymentCategory = t.EmploymentCategorySnapshot,
                CurrentJobId = t.CurrentJobId,
                CurrentJobPositionId = job.ExistingJobPositionId,
                CurrentPositionId = job.ExistingPositionId,
                CurrentPositionTitle = job.ExistingPositionTitleSnapshot
            },
            Job = new StaffTransferJobTabDto
            {
                ExistingJobId = job.ExistingJobId > 0 ? job.ExistingJobId : t.CurrentJobId,
                ExistingJobPositionId = job.ExistingJobPositionId,
                ExistingPositionId = job.ExistingPositionId,
                ExistingPositionTitle = job.ExistingPositionTitleSnapshot,
                ExistingPositionFrameworkId = job.ExistingPositionFrameworkId,
                ExistingPositionMatrixId = job.ExistingPositionMatrixId,
                ExistingJobMatrixId = job.ExistingJobMatrixId,
                ExistingJobMatrixStepId = job.ExistingJobMatrixStepId,
                ExistingGradeLevel = job.ExistingGradeLevelSnapshot,
                ExistingBasicSalary = canSeeSalary ? job.ExistingBasicSalarySnapshot : null,
                ExistingAllowance = canSeeSalary ? job.ExistingAllowanceSnapshot : null,
                DestinationOrganisationId = t.ToOrganisationId,
                DestinationOrganisationStructureId = t.ToOrganisationStructureId,
                DestinationJobTypeId = job.DestinationJobTypeId,
                DestinationPositionId = job.DestinationPositionId,
                DestinationPositionTitle = job.DestinationPositionTitleSnapshot ?? job.DestinationPosition?.Name,
                DestinationPositionFrameworkId = job.DestinationPositionFrameworkId,
                DestinationPositionMatrixId = job.DestinationPositionMatrixId,
                DestinationJobMatrixId = job.DestinationJobMatrixId,
                DestinationJobMatrixStepId = job.DestinationJobMatrixStepId,
                DestinationGradeLevel = job.DestinationGradeLevelSnapshot,
                TransferTypeId = t.TransferTypeId,
                TransferAppointmentMode = job.TransferAppointmentMode,
                HasSalaryBenefitChange = job.HasSalaryBenefitChange,
                ProposedBasicSalary = canSeeSalary ? job.ProposedBasicSalary : null,
                ProposedAllowance = canSeeSalary ? job.ProposedAllowance : null,
                SalaryBenefitChangeNotes = job.SalaryBenefitChangeNotes,
                NewReportingSupervisorIndividualId = job.NewReportingSupervisorIndividualId,
                EffectiveDate = t.EffectiveDate,
                TransferReason = t.TransferReason
            },
            Work = new StaffTransferWorkTabDto
            {
                CurrentLocationId = work.CurrentLocationId,
                CurrentLocationName = work.CurrentLocationNameSnapshot,
                ProposedLocationId = work.ProposedLocationId,
                ProposedLocationName = work.ProposedLocationName,
                CurrentPlanningProviderId = work.CurrentPlanningProviderId,
                CurrentPlanningProviderName = work.CurrentPlanningProvider?.Name,
                ProposedPlanningProviderId = work.ProposedPlanningProviderId,
                ProposedPlanningProviderName = work.ProposedPlanningProvider?.Name,
                CurrentWorkTemplateId = work.CurrentWorkTemplateId,
                CurrentWorkTemplateName = work.CurrentWorkTemplate?.Name,
                ProposedWorkTemplateId = work.ProposedWorkTemplateId,
                ProposedWorkTemplateName = work.ProposedWorkTemplate?.Name,
                CurrentScheduleMode = work.CurrentScheduleMode,
                ProposedScheduleMode = work.ProposedScheduleMode,
                WorkingDaysMask = work.WorkingDaysMask,
                StandardStartTime = work.StandardStartTime,
                StandardEndTime = work.StandardEndTime,
                WeeklyWorkingHours = work.WeeklyWorkingHours,
                RequiredAttendanceMethod = work.RequiredAttendanceMethod,
                AttendanceDeviceId = work.AttendanceDeviceId,
                AttendanceDeviceLocationNotes = work.AttendanceDeviceLocationNotes,
                IsOvertimeEligible = work.IsOvertimeEligible,
                WorkArrangementMode = work.WorkArrangementMode,
                AttendanceVerificationSupervisorIndividualId = work.AttendanceVerificationSupervisorIndividualId,
                ScheduleEffectiveDate = work.ScheduleEffectiveDate == default ? t.EffectiveDate : work.ScheduleEffectiveDate,
                ReconcileFutureWorkPlans = work.ReconcileFutureWorkPlans,
                WorkNotes = work.WorkNotes
            },
            AccessChanges = t.AccessChanges
                .OrderBy(a => a.StaffTransferAccessChangeId)
                .Select(a => new StaffTransferAccessChangeItemDto
                {
                    StaffTransferAccessChangeId = a.StaffTransferAccessChangeId,
                    AccessAreaCode = a.AccessAreaCode,
                    AccessAreaName = a.AccessAreaName,
                    CurrentScopeSummary = a.CurrentScopeSummary,
                    ProposedScopeSummary = a.ProposedScopeSummary,
                    TransitionAction = a.TransitionAction,
                    RoleId = a.RoleId,
                    RoleName = a.Role?.Name,
                    UserGroupId = a.UserGroupId,
                    UserGroupName = a.UserGroup?.GroupName,
                    RemainsActiveInOldDepartmentWarning = a.RemainsActiveInOldDepartmentWarning,
                    RequiresSeparateSecurityAuth = a.RequiresSeparateSecurityAuth,
                    ProvisioningStatus = a.ProvisioningStatus,
                    AppliedAtUtc = a.AppliedAtUtc,
                    Remarks = a.Remarks
                }).ToList(),
            DoorAccessChanges = t.DoorAccessChanges
                .OrderBy(d => d.StaffTransferDoorAccessId)
                .Select(d => new StaffTransferDoorAccessItemDto
                {
                    StaffTransferDoorAccessId = d.StaffTransferDoorAccessId,
                    BuildingName = d.BuildingName,
                    FloorName = d.FloorName,
                    DepartmentOfficeArea = d.DepartmentOfficeArea,
                    RestrictedRoomName = d.RestrictedRoomName,
                    IncludesServerRoom = d.IncludesServerRoom,
                    StaffEntranceName = d.StaffEntranceName,
                    AttendanceDeviceId = d.AttendanceDeviceId,
                    AccessCardReference = d.AccessCardReference,
                    ExistingAccessGroups = d.ExistingAccessGroups,
                    ProposedAccessGroups = d.ProposedAccessGroups,
                    TransitionAction = d.TransitionAction,
                    AccessStartDate = d.AccessStartDate,
                    PreviousAccessExpiryDate = d.PreviousAccessExpiryDate,
                    SecurityOfficerUserId = d.SecurityOfficerUserId,
                    SecurityOfficerName = d.SecurityOfficerUser?.FullName ?? d.SecurityOfficerUser?.Username,
                    ConnectorReference = d.ConnectorReference,
                    ProvisioningState = d.ProvisioningState,
                    ProvisionedAtUtc = d.ProvisionedAtUtc,
                    ProvisioningNotes = d.ProvisioningNotes
                }).ToList(),
            Reporting = new StaffTransferReportingTabDto
            {
                ApprovedTransferEffectiveDate = rep.ApprovedTransferEffectiveDate == default ? t.EffectiveDate : rep.ApprovedTransferEffectiveDate,
                ReportingDate = rep.ReportingDate == default ? t.ReportingDate : rep.ReportingDate,
                ReportingTime = rep.ReportingTime,
                ReportingLocation = rep.ReportingLocation,
                ReportingToIndividualId = rep.ReportingToIndividualId,
                ReportingToName = rep.ReportingToNameSnapshot ?? (rep.ReportingToIndividual != null ? FullName(rep.ReportingToIndividual) : null),
                HandoverRequired = rep.HandoverRequired,
                HandoverDeadline = rep.HandoverDeadline,
                OutstandingAssignmentsSummary = rep.OutstandingAssignmentsSummary,
                AssetsToReturnSummary = rep.AssetsToReturnSummary,
                AssetsToReceiveSummary = rep.AssetsToReceiveSummary,
                OutstandingApprovalsRoutingRule = rep.OutstandingApprovalsRoutingRule,
                OutstandingApprovalsSummary = rep.OutstandingApprovalsSummary,
                SpecialInstructions = rep.SpecialInstructions
            },
            Comments = t.Comments
                .Where(c => !isEmployee || c.IsVisibleToEmployee)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new StaffTransferCommentDto(
                    c.StaffTransferCommentId,
                    c.AuthorUserId,
                    c.AuthorUser.FullName ?? c.AuthorUser.Username,
                    c.ParticipantRole,
                    c.CommentKind,
                    c.RequestVersion,
                    c.CommentText,
                    c.IsVisibleToEmployee,
                    c.CreatedAtUtc)).ToList(),
            Confirmations = t.Confirmations
                .OrderByDescending(c => c.ConfirmedAtUtc)
                .Select(c => new StaffTransferConfirmationDto(
                    c.StaffTransferConfirmationId,
                    c.ConfirmedVersion,
                    c.ResponseKind,
                    c.ConfirmedByUserId,
                    c.ConfirmedByUser.FullName ?? c.ConfirmedByUser.Username,
                    c.ConfirmationStatement,
                    c.EmployeeRemarks,
                    c.IsInvalidatedByMaterialEdit,
                    c.InvalidatedAtUtc,
                    c.ConfirmedAtUtc)).ToList(),
            ActivationLogs = t.ActivationLogs
                .OrderByDescending(l => l.ExecutedAtUtc)
                .Select(l => new StaffTransferActivationLogDto(
                    l.StaffTransferActivationLogId,
                    l.ExecutionAttempt,
                    l.StepCode,
                    l.StepStatus,
                    l.IdempotencyKey,
                    l.DetailsJson,
                    l.ErrorMessage,
                    l.ExecutedAtUtc)).ToList(),
            Documents = t.Documents
                .Where(d => canExportRestricted || d.DocumentVariant == StaffTransferDocumentVariants.EmployeeNotice)
                .OrderByDescending(d => d.GeneratedAtUtc)
                .Select(d => new StaffTransferDocumentSummaryDto(
                    d.StaffTransferDocumentId,
                    d.DocumentNumber,
                    d.DocumentVariant,
                    d.RequestVersion,
                    d.ConfirmedVersion,
                    d.Sha256Hash,
                    d.GeneratedAtUtc)).ToList()
        };

        return dto;
    }

    private async Task<StaffTransferDetailDto> BuildNewDraftFromDatabaseAsync(
        HrmTeContext db,
        int individualId,
        int? jobId,
        UserContextDto actor,
        int org,
        bool isHrOrAdmin)
    {
        var today = TodayMaldives;
        var job = await db.Jobs.AsNoTracking()
            .Include(j => j.Individual)
            .Include(j => j.Staff)
            .Include(j => j.Organisation)
            .Include(j => j.OrganisationStructure)
            .Include(j => j.JobType)
            .Where(j => j.IndividualID == individualId && j.OrganisationID == org &&
                        j.JobStateId == SharedConfig.JobStates.APPROVED &&
                        (!j.TerminatedDate.HasValue || j.TerminatedDate.Value > today) &&
                        (!jobId.HasValue || j.JobId == jobId.Value))
            .OrderByDescending(j => j.JoinedDate)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No active approved employment record was found for the selected employee.");

        var activeJobPosition = await db.JobPositions.AsNoTracking()
            .Include(jp => jp.Position)
            .Where(jp => jp.JobId == job.JobId && jp.JobPositionStateId == SharedConfig.JobPositionStates.APPROVED &&
                         (!jp.ToDate.HasValue || jp.ToDate.Value >= today))
            .OrderByDescending(jp => jp.FromDate)
            .FirstOrDefaultAsync();

        var stepHistory = await db.FrameworkJobStepHistories.AsNoTracking()
            .Where(s => s.JobId == job.JobId && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= today))
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync();

        var idCard = await db.Idcards.AsNoTracking()
            .Where(c => c.BusinessEntityId == individualId && c.IdcardNumber != null && c.IdcardNumber != "")
            .Select(c => c.IdcardNumber)
            .FirstOrDefaultAsync();

        var userProfile = await db.Users.AsNoTracking()
            .Where(u => u.BusinessEntityID == individualId && u.IsActive)
            .Select(u => new { u.Email, u.Mobile })
            .FirstOrDefaultAsync();

        var supervisorIndividualId = await db.OrganisationStructureSupervisors.AsNoTracking()
            .Where(s => s.OrganisationStructureId == job.OrganisationStructureId && s.IsActive && s.IsValid && s.SupervisorStaffId != individualId)
            .Select(s => (int?)s.SupervisorStaffId)
            .FirstOrDefaultAsync();

        var supervisorName = supervisorIndividualId.HasValue
            ? FullName(await db.Individuals.AsNoTracking().SingleAsync(i => i.BusinessEntityId == supervisorIndividualId.Value))
            : null;

        var hodIndividualId = await db.OrganisationStructureHeadIncharges.AsNoTracking()
            .Where(h => h.OrganisationStructureId == job.OrganisationStructureId && h.IsValid && h.IndividualId != individualId)
            .Select(h => (int?)h.IndividualId)
            .FirstOrDefaultAsync();

        var hodName = hodIndividualId.HasValue
            ? FullName(await db.Individuals.AsNoTracking().SingleAsync(i => i.BusinessEntityId == hodIndividualId.Value))
            : null;

        var currentTemplateAssignment = await db.JobWorkTemplateAssignments.AsNoTracking()
            .Where(a => a.JobID == job.JobId && a.IsActive && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value >= today))
            .OrderByDescending(a => a.EffectiveFrom)
            .FirstOrDefaultAsync();

        var currentProviderSetting = await db.OrganisationWorkPlanningSettings.AsNoTracking()
            .Include(s => s.PlanningProvider)
            .Where(s => s.OrganisationBusinessEntityId == org && s.IsActive && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= today))
            .FirstOrDefaultAsync();

        var issuedAssets = await db.ItemUnits.AsNoTracking()
            .Include(u => u.Item)
            .Where(u => u.OrganisationId == org && u.IssuedToIndividualId == individualId && u.Status == "Issued")
            .Select(u => $"{u.Item.Name} ({u.InventoryNumber})")
            .ToListAsync();

        var defaultEffective = today.AddDays(7);

        return new StaffTransferDetailDto
        {
            Status = StaffTransferStatuses.Draft,
            IsOpen = true,
            CurrentVersion = 1,
            RequestedByUserId = actor.UserId,
            RequestedByName = actor.FullName,
            RequestedDate = DateTime.UtcNow,
            CanEdit = true,
            CanSubmit = true,
            CanExportRestrictedPdf = isHrOrAdmin,
            Staff = new StaffTransferStaffTabDto
            {
                IndividualId = individualId,
                FullName = FullName(job.Individual),
                EmployeeNumber = job.Staff?.EmployeeNumber ?? $"EMP-{individualId}",
                IdCardNumber = idCard,
                HasPhoto = job.Individual.ImageData is { Length: > 0 },
                PhotoContentType = job.Individual.ImageContentType,
                CurrentOrganisationId = job.OrganisationID,
                CurrentOrganisationName = job.Organisation.OrganisationName,
                CurrentOrganisationStructureId = job.OrganisationStructureId,
                CurrentDepartmentName = job.OrganisationStructure.Name,
                CurrentEmploymentStatus = "Active",
                DateJoined = job.JoinedDate.Date,
                OfficialEmail = userProfile?.Email,
                OfficialPhone = userProfile?.Mobile,
                CurrentSupervisorIndividualId = supervisorIndividualId,
                CurrentSupervisorName = supervisorName,
                CurrentHodIndividualId = hodIndividualId,
                CurrentHodName = hodName,
                EmploymentCategory = job.JobType.TypeName,
                CurrentJobId = job.JobId,
                CurrentJobPositionId = activeJobPosition?.JobPositionId,
                CurrentPositionId = activeJobPosition?.PositionId,
                CurrentPositionTitle = activeJobPosition?.Position.Name ?? "Unassigned Position"
            },
            Job = new StaffTransferJobTabDto
            {
                ExistingJobId = job.JobId,
                ExistingJobPositionId = activeJobPosition?.JobPositionId,
                ExistingPositionId = activeJobPosition?.PositionId,
                ExistingPositionTitle = activeJobPosition?.Position.Name ?? "Unassigned Position",
                ExistingPositionFrameworkId = activeJobPosition?.Position.PositionFrameworkId,
                ExistingPositionMatrixId = activeJobPosition?.Position.PositionMatrixId ?? stepHistory?.PositionMatrixId,
                ExistingJobMatrixStepId = stepHistory?.JobMatrixStepId,
                ExistingBasicSalary = isHrOrAdmin ? (stepHistory?.BasicSalarySnapshot ?? job.BasicSalary) : null,
                ExistingAllowance = isHrOrAdmin ? stepHistory?.JobAllowanceSnapshot : null,
                DestinationOrganisationId = job.OrganisationID,
                DestinationOrganisationStructureId = 0,
                DestinationJobTypeId = job.JobTypeId,
                DestinationPositionId = activeJobPosition?.PositionId,
                DestinationPositionTitle = activeJobPosition?.Position.Name,
                DestinationPositionFrameworkId = activeJobPosition?.Position.PositionFrameworkId,
                DestinationPositionMatrixId = activeJobPosition?.Position.PositionMatrixId,
                DestinationJobMatrixStepId = stepHistory?.JobMatrixStepId,
                TransferTypeId = (int)StaffTransferType.InternalDepartmental,
                TransferAppointmentMode = TransferAppointmentModes.OrganisationalReassignment,
                EffectiveDate = defaultEffective,
                TransferReason = ""
            },
            Work = new StaffTransferWorkTabDto
            {
                CurrentLocationName = job.OrganisationStructure.Name,
                ProposedLocationName = "",
                CurrentPlanningProviderId = currentProviderSetting?.PlanningProviderId,
                CurrentPlanningProviderName = currentProviderSetting?.PlanningProvider?.Name,
                ProposedPlanningProviderId = currentProviderSetting?.PlanningProviderId,
                ProposedPlanningProviderName = currentProviderSetting?.PlanningProvider?.Name,
                CurrentWorkTemplateId = currentTemplateAssignment?.WorkTemplateID,
                ProposedWorkTemplateId = currentTemplateAssignment?.WorkTemplateID,
                CurrentScheduleMode = currentTemplateAssignment?.ScheduleMode ?? "Standard",
                ProposedScheduleMode = currentTemplateAssignment?.ScheduleMode ?? "Standard",
                WorkingDaysMask = currentTemplateAssignment?.WorkingDaysMask ?? 31,
                StandardStartTime = currentTemplateAssignment?.ScheduledStartTime ?? new TimeOnly(8, 0),
                StandardEndTime = new TimeOnly(14, 0),
                WeeklyWorkingHours = 35m,
                RequiredAttendanceMethod = "Biometric",
                WorkArrangementMode = "OnSite",
                ScheduleEffectiveDate = defaultEffective,
                ReconcileFutureWorkPlans = true
            },
            AccessChanges = BuildDefaultAccessComparisonRows(job.OrganisationStructure.Name),
            DoorAccessChanges =
            [
                new StaffTransferDoorAccessItemDto
                {
                    BuildingName = job.Organisation.OrganisationName,
                    DepartmentOfficeArea = job.OrganisationStructure.Name,
                    ExistingAccessGroups = $"{job.OrganisationStructure.Name} Staff Access",
                    ProposedAccessGroups = "Destination Department Staff Access",
                    TransitionAction = StaffTransferDoorAccessStates.ActionReplace,
                    AccessStartDate = defaultEffective,
                    PreviousAccessExpiryDate = defaultEffective.AddDays(-1),
                    ProvisioningState = StaffTransferDoorAccessStates.StatePending
                }
            ],
            Reporting = new StaffTransferReportingTabDto
            {
                ApprovedTransferEffectiveDate = defaultEffective,
                ReportingDate = defaultEffective,
                ReportingTime = new TimeOnly(8, 0),
                ReportingLocation = "",
                HandoverRequired = true,
                HandoverDeadline = defaultEffective.AddDays(-1),
                AssetsToReturnSummary = issuedAssets.Count > 0 ? string.Join(", ", issuedAssets) : "Departmental keys and assigned items if applicable",
                AssetsToReceiveSummary = "Destination department workstation and access credentials",
                OutstandingApprovalsRoutingRule = StaffTransferApprovalRoutingRules.RetainHistoricalReassignOpen
            }
        };
    }

    private static List<StaffTransferAccessChangeItemDto> BuildDefaultAccessComparisonRows(string sourceDeptName) =>
    [
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.OrganisationDataAccess,
            AccessAreaName = "Organisation Data Access",
            CurrentScopeSummary = $"{sourceDeptName} (Current Organisation)",
            ProposedScopeSummary = "Destination Department Scope",
            TransitionAction = StaffTransferAccessActions.Retain
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.DepartmentInformation,
            AccessAreaName = "Department-Specific Information",
            CurrentScopeSummary = $"{sourceDeptName} internal records",
            ProposedScopeSummary = "Receiving department records",
            TransitionAction = StaffTransferAccessActions.RevokeOldGrantNew,
            RemainsActiveInOldDepartmentWarning = true
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.EmployeeRecordVisibility,
            AccessAreaName = "Employee Record Visibility",
            CurrentScopeSummary = $"Self & {sourceDeptName} directory",
            ProposedScopeSummary = "Self & Receiving department directory",
            TransitionAction = StaffTransferAccessActions.ReviewAndAssign
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.HrApprovalPermissions,
            AccessAreaName = "HR Approval Permissions",
            CurrentScopeSummary = $"Scoped to {sourceDeptName}",
            ProposedScopeSummary = "Review for receiving department",
            TransitionAction = StaffTransferAccessActions.Revoke,
            RemainsActiveInOldDepartmentWarning = true
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.AttendanceAdministration,
            AccessAreaName = "Attendance Administration",
            CurrentScopeSummary = $"{sourceDeptName} attendance scope",
            ProposedScopeSummary = "Receiving department attendance scope",
            TransitionAction = StaffTransferAccessActions.ReviewAndAssign
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.LeaveManagement,
            AccessAreaName = "Leave Management",
            CurrentScopeSummary = $"{sourceDeptName} leave routing",
            ProposedScopeSummary = "Receiving department leave routing",
            TransitionAction = StaffTransferAccessActions.RevokeOldGrantNew
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.ReportsAndAnalytics,
            AccessAreaName = "Reports and Analytics",
            CurrentScopeSummary = $"{sourceDeptName} operational reports",
            ProposedScopeSummary = "Receiving department operational reports",
            TransitionAction = StaffTransferAccessActions.ReviewAndAssign
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.RequestProcessing,
            AccessAreaName = "Request Processing",
            CurrentScopeSummary = $"{sourceDeptName} request queues",
            ProposedScopeSummary = "Receiving department request queues",
            TransitionAction = StaffTransferAccessActions.RevokeOldGrantNew,
            RemainsActiveInOldDepartmentWarning = true
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.SupervisorHodPrivileges,
            AccessAreaName = "Supervisor / HOD Privileges",
            CurrentScopeSummary = $"Supervisory designation in {sourceDeptName}",
            ProposedScopeSummary = "Do not carry over old department supervisory scope",
            TransitionAction = StaffTransferAccessActions.Revoke,
            RemainsActiveInOldDepartmentWarning = true
        },
        new()
        {
            AccessAreaCode = StaffTransferAccessAreas.SensitiveStaffInformation,
            AccessAreaName = "Sensitive Staff Information",
            CurrentScopeSummary = "Restricted HR/Protected access",
            ProposedScopeSummary = "Requires explicit security authorisation if granted",
            TransitionAction = StaffTransferAccessActions.NoChange,
            RequiresSeparateSecurityAuth = true
        }
    ];

    public async Task<ServiceResult> SaveAsync(StaffTransferSaveInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var today = TodayMaldives;
            var sourceJob = await db.Jobs
                .Include(j => j.Individual)
                .Include(j => j.Staff)
                .Include(j => j.OrganisationStructure)
                .Include(j => j.JobType)
                .SingleOrDefaultAsync(j => j.JobId == input.CurrentJobId && j.IndividualID == input.IndividualId)
                ?? throw new InvalidOperationException("Selected employee job record was not found.");

            if (sourceJob.JobStateId != SharedConfig.JobStates.APPROVED ||
                (sourceJob.TerminatedDate.HasValue && sourceJob.TerminatedDate.Value <= today))
                throw new InvalidOperationException("The employee must have an active approved employment record.");

            if (!isHrOrAdmin && !await IsDepartmentAuthorizedOfficerAsync(db, actor, sourceJob.OrganisationID, sourceJob.OrganisationStructureId))
                throw new UnauthorizedAccessException("Only an authorized officer of the sending department or HR can initiate or edit this transfer.");

            StaffTransferValidator.ValidateSaveInput(input, sourceJob.OrganisationID, sourceJob.OrganisationStructureId);

            if (!await db.OrganisationStructures.AnyAsync(s =>
                s.OrganisationStructureId == input.Job.DestinationOrganisationStructureId &&
                s.OrganisationBusinessEntityID == input.Job.DestinationOrganisationId &&
                s.OrganisationStructureStateId == 3))
            {
                throw new InvalidOperationException("Select an approved destination department/structure.");
            }

            if (await db.StaffTransfers.AnyAsync(t =>
                t.CurrentJobId == sourceJob.JobId &&
                t.IsOpen &&
                t.StaffTransferId != input.StaffTransferId))
            {
                throw new InvalidOperationException("This employee already has an open transfer request for this job.");
            }

            var newFingerprint = StaffTransferValidator.ComputeMaterialFingerprint(input);
            StaffTransfer transfer;
            var isNew = input.StaffTransferId <= 0;

            if (isNew)
            {
                var staffTab = await BuildNewDraftFromDatabaseAsync(db, input.IndividualId, input.CurrentJobId, actor, sourceJob.OrganisationID, isHrOrAdmin);
                transfer = new StaffTransfer
                {
                    IndividualId = input.IndividualId,
                    CurrentJobId = sourceJob.JobId,
                    FromOrganisationId = sourceJob.OrganisationID,
                    FromOrganisationStructureId = sourceJob.OrganisationStructureId,
                    ToOrganisationId = input.Job.DestinationOrganisationId,
                    ToOrganisationStructureId = input.Job.DestinationOrganisationStructureId,
                    TransferTypeId = input.Job.TransferTypeId,
                    TransferReason = input.Job.TransferReason.Trim(),
                    RequestedByUserId = actor.UserId,
                    RequestedDate = DateTime.UtcNow,
                    EffectiveDate = input.Job.EffectiveDate.Date,
                    ReportingDate = input.Reporting.ReportingDate.Date,
                    Status = StaffTransferStatuses.Draft,
                    IsOpen = true,
                    CurrentVersion = 1,
                    MaterialStateFingerprint = newFingerprint,
                    FullNameSnapshot = staffTab.Staff.FullName,
                    EmployeeNumberSnapshot = staffTab.Staff.EmployeeNumber,
                    ProtectedIdCardSnapshot = staffTab.Staff.IdCardNumber,
                    CurrentEmploymentStatusSnapshot = staffTab.Staff.CurrentEmploymentStatus,
                    EmploymentCategorySnapshot = staffTab.Staff.EmploymentCategory,
                    DateJoinedSnapshot = staffTab.Staff.DateJoined,
                    OfficialEmailSnapshot = staffTab.Staff.OfficialEmail,
                    OfficialPhoneSnapshot = staffTab.Staff.OfficialPhone,
                    CurrentSupervisorIndividualId = staffTab.Staff.CurrentSupervisorIndividualId,
                    CurrentHodIndividualId = staffTab.Staff.CurrentHodIndividualId,
                    StaffSnapshotJson = JsonSerializer.Serialize(staffTab.Staff),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };
                db.StaffTransfers.Add(transfer);
                await db.SaveChangesAsync();
            }
            else
            {
                transfer = await db.StaffTransfers
                    .Include(t => t.JobDetail)
                    .Include(t => t.WorkDetail)
                    .Include(t => t.ReportingDetail)
                    .Include(t => t.AccessChanges)
                    .Include(t => t.DoorAccessChanges)
                    .Include(t => t.Confirmations)
                    .SingleOrDefaultAsync(t => t.StaffTransferId == input.StaffTransferId)
                    ?? throw new InvalidOperationException("Transfer record was not found.");

                if (transfer.Status is not (StaffTransferStatuses.Draft or StaffTransferStatuses.CorrectionRequested or StaffTransferStatuses.Submitted or StaffTransferStatuses.PendingStaffConfirmation))
                    throw new InvalidOperationException($"Transfer cannot be edited in '{transfer.Status}' status.");

                if (input.ExpectedVersion > 0 && transfer.CurrentVersion != input.ExpectedVersion)
                    throw new InvalidOperationException("This transfer was updated by another user. Refresh before saving.");

                var materialChanged = !string.Equals(transfer.MaterialStateFingerprint, newFingerprint, StringComparison.Ordinal);
                if (materialChanged && (transfer.ConfirmedVersion.HasValue || transfer.Status != StaffTransferStatuses.Draft))
                {
                    transfer.CurrentVersion++;
                    if (transfer.ConfirmedVersion.HasValue)
                    {
                        transfer.ConfirmedVersion = null;
                        foreach (var conf in transfer.Confirmations.Where(c => !c.IsInvalidatedByMaterialEdit))
                        {
                            conf.IsInvalidatedByMaterialEdit = true;
                            conf.InvalidatedAtUtc = DateTime.UtcNow;
                        }
                        db.StaffTransferComments.Add(new StaffTransferComment
                        {
                            StaffTransferId = transfer.StaffTransferId,
                            AuthorUserId = actor.UserId,
                            AuthorIndividualId = actor.IndividualId,
                            ParticipantRole = StaffTransferParticipantRoles.SendingOfficer,
                            CommentKind = StaffTransferCommentKinds.SystemAudit,
                            RequestVersion = transfer.CurrentVersion,
                            CommentText = $"Material transfer details were updated to Version {transfer.CurrentVersion}; previous employee confirmation was invalidated and renewed confirmation is required.",
                            IsVisibleToEmployee = true,
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                transfer.ToOrganisationId = input.Job.DestinationOrganisationId;
                transfer.ToOrganisationStructureId = input.Job.DestinationOrganisationStructureId;
                transfer.TransferTypeId = input.Job.TransferTypeId;
                transfer.TransferReason = input.Job.TransferReason.Trim();
                transfer.EffectiveDate = input.Job.EffectiveDate.Date;
                transfer.ReportingDate = input.Reporting.ReportingDate.Date;
                transfer.MaterialStateFingerprint = newFingerprint;
                transfer.UpdatedDate = DateTime.UtcNow;
            }

            // Upsert Tab 2 Job Detail
            var destPositionTitle = input.Job.DestinationPositionId.HasValue
                ? await db.Positions.Where(p => p.PositionId == input.Job.DestinationPositionId.Value).Select(p => p.Name).FirstOrDefaultAsync()
                : input.Job.DestinationPositionTitle;

            var jobDetail = transfer.JobDetail ?? new StaffTransferJobDetail { StaffTransferId = transfer.StaffTransferId };
            jobDetail.ExistingJobId = sourceJob.JobId;
            jobDetail.ExistingJobPositionId = input.Job.ExistingJobPositionId;
            jobDetail.ExistingPositionId = input.Job.ExistingPositionId;
            jobDetail.ExistingPositionTitleSnapshot = string.IsNullOrWhiteSpace(input.Job.ExistingPositionTitle) ? "Unassigned Position" : input.Job.ExistingPositionTitle;
            jobDetail.ExistingPositionFrameworkId = input.Job.ExistingPositionFrameworkId;
            jobDetail.ExistingPositionMatrixId = input.Job.ExistingPositionMatrixId;
            jobDetail.ExistingJobMatrixId = input.Job.ExistingJobMatrixId;
            jobDetail.ExistingJobMatrixStepId = input.Job.ExistingJobMatrixStepId;
            jobDetail.ExistingGradeLevelSnapshot = input.Job.ExistingGradeLevel;
            jobDetail.ExistingBasicSalarySnapshot = input.Job.ExistingBasicSalary ?? sourceJob.BasicSalary;
            jobDetail.ExistingAllowanceSnapshot = input.Job.ExistingAllowance;
            jobDetail.DestinationOrganisationId = input.Job.DestinationOrganisationId;
            jobDetail.DestinationOrganisationStructureId = input.Job.DestinationOrganisationStructureId;
            jobDetail.DestinationJobTypeId = input.Job.DestinationJobTypeId ?? sourceJob.JobTypeId;
            jobDetail.DestinationPositionId = input.Job.DestinationPositionId;
            jobDetail.DestinationPositionTitleSnapshot = destPositionTitle;
            jobDetail.DestinationPositionFrameworkId = input.Job.DestinationPositionFrameworkId;
            jobDetail.DestinationPositionMatrixId = input.Job.DestinationPositionMatrixId;
            jobDetail.DestinationJobMatrixId = input.Job.DestinationJobMatrixId;
            jobDetail.DestinationJobMatrixStepId = input.Job.DestinationJobMatrixStepId;
            jobDetail.DestinationGradeLevelSnapshot = input.Job.DestinationGradeLevel;
            jobDetail.TransferAppointmentMode = input.Job.TransferAppointmentMode;
            jobDetail.HasSalaryBenefitChange = input.Job.HasSalaryBenefitChange;
            jobDetail.ProposedBasicSalary = input.Job.ProposedBasicSalary;
            jobDetail.ProposedAllowance = input.Job.ProposedAllowance;
            jobDetail.SalaryBenefitChangeNotes = input.Job.SalaryBenefitChangeNotes;
            jobDetail.NewReportingSupervisorIndividualId = input.Job.NewReportingSupervisorIndividualId;
            if (transfer.JobDetail is null) db.StaffTransferJobDetails.Add(jobDetail);

            // Upsert Tab 3 Work Detail
            var workDetail = transfer.WorkDetail ?? new StaffTransferWorkDetail { StaffTransferId = transfer.StaffTransferId };
            workDetail.CurrentLocationId = input.Work.CurrentLocationId;
            workDetail.CurrentLocationNameSnapshot = input.Work.CurrentLocationName;
            workDetail.ProposedLocationId = input.Work.ProposedLocationId;
            workDetail.ProposedLocationName = input.Work.ProposedLocationName.Trim();
            workDetail.CurrentPlanningProviderId = input.Work.CurrentPlanningProviderId;
            workDetail.ProposedPlanningProviderId = input.Work.ProposedPlanningProviderId;
            workDetail.CurrentWorkTemplateId = input.Work.CurrentWorkTemplateId;
            workDetail.ProposedWorkTemplateId = input.Work.ProposedWorkTemplateId;
            workDetail.CurrentScheduleMode = input.Work.CurrentScheduleMode;
            workDetail.ProposedScheduleMode = string.IsNullOrWhiteSpace(input.Work.ProposedScheduleMode) ? "Standard" : input.Work.ProposedScheduleMode;
            workDetail.WorkingDaysMask = input.Work.WorkingDaysMask is > 0 and <= 127 ? input.Work.WorkingDaysMask : 31;
            workDetail.StandardStartTime = input.Work.StandardStartTime;
            workDetail.StandardEndTime = input.Work.StandardEndTime;
            workDetail.WeeklyWorkingHours = input.Work.WeeklyWorkingHours > 0 ? input.Work.WeeklyWorkingHours : 35m;
            workDetail.RequiredAttendanceMethod = string.IsNullOrWhiteSpace(input.Work.RequiredAttendanceMethod) ? "Biometric" : input.Work.RequiredAttendanceMethod;
            workDetail.AttendanceDeviceId = input.Work.AttendanceDeviceId;
            workDetail.AttendanceDeviceLocationNotes = input.Work.AttendanceDeviceLocationNotes;
            workDetail.IsOvertimeEligible = input.Work.IsOvertimeEligible;
            workDetail.WorkArrangementMode = string.IsNullOrWhiteSpace(input.Work.WorkArrangementMode) ? "OnSite" : input.Work.WorkArrangementMode;
            workDetail.AttendanceVerificationSupervisorIndividualId = input.Work.AttendanceVerificationSupervisorIndividualId;
            workDetail.ScheduleEffectiveDate = (input.Work.ScheduleEffectiveDate == default ? input.Job.EffectiveDate : input.Work.ScheduleEffectiveDate).Date;
            workDetail.ReconcileFutureWorkPlans = input.Work.ReconcileFutureWorkPlans;
            workDetail.WorkNotes = input.Work.WorkNotes;
            if (transfer.WorkDetail is null) db.StaffTransferWorkDetails.Add(workDetail);

            // Replace Tab 4 Access Changes
            if (transfer.AccessChanges.Count > 0)
                db.StaffTransferAccessChanges.RemoveRange(transfer.AccessChanges);
            var accessRows = input.AccessChanges.Count > 0
                ? input.AccessChanges
                : BuildDefaultAccessComparisonRows(sourceJob.OrganisationStructure.Name);
            foreach (var item in accessRows)
            {
                db.StaffTransferAccessChanges.Add(new StaffTransferAccessChange
                {
                    StaffTransferId = transfer.StaffTransferId,
                    AccessAreaCode = item.AccessAreaCode,
                    AccessAreaName = item.AccessAreaName,
                    CurrentScopeSummary = item.CurrentScopeSummary,
                    ProposedScopeSummary = item.ProposedScopeSummary,
                    TransitionAction = StaffTransferAccessActions.All.Contains(item.TransitionAction) ? item.TransitionAction : StaffTransferAccessActions.ReviewAndAssign,
                    RoleId = item.RoleId,
                    UserGroupId = item.UserGroupId,
                    RemainsActiveInOldDepartmentWarning = item.RemainsActiveInOldDepartmentWarning,
                    RequiresSeparateSecurityAuth = item.RequiresSeparateSecurityAuth,
                    ProvisioningStatus = "Pending",
                    Remarks = item.Remarks
                });
            }

            // Replace Tab 5 Door Access Items
            if (transfer.DoorAccessChanges.Count > 0)
                db.StaffTransferDoorAccesses.RemoveRange(transfer.DoorAccessChanges);
            foreach (var door in input.DoorAccessChanges)
            {
                if (string.IsNullOrWhiteSpace(door.BuildingName)) continue;
                db.StaffTransferDoorAccesses.Add(new StaffTransferDoorAccess
                {
                    StaffTransferId = transfer.StaffTransferId,
                    BuildingName = door.BuildingName.Trim(),
                    FloorName = door.FloorName,
                    DepartmentOfficeArea = door.DepartmentOfficeArea,
                    RestrictedRoomName = door.RestrictedRoomName,
                    IncludesServerRoom = door.IncludesServerRoom,
                    StaffEntranceName = door.StaffEntranceName,
                    AttendanceDeviceId = door.AttendanceDeviceId,
                    AccessCardReference = door.AccessCardReference,
                    ExistingAccessGroups = door.ExistingAccessGroups,
                    ProposedAccessGroups = door.ProposedAccessGroups,
                    TransitionAction = string.IsNullOrWhiteSpace(door.TransitionAction) ? StaffTransferDoorAccessStates.ActionReplace : door.TransitionAction,
                    AccessStartDate = (door.AccessStartDate == default ? input.Job.EffectiveDate : door.AccessStartDate).Date,
                    PreviousAccessExpiryDate = door.PreviousAccessExpiryDate?.Date,
                    SecurityOfficerUserId = door.SecurityOfficerUserId,
                    ConnectorReference = door.ConnectorReference,
                    ProvisioningState = StaffTransferDoorAccessStates.StatePending,
                    ProvisioningNotes = door.ProvisioningNotes
                });
            }

            // Upsert Tab 6 Reporting Detail
            var repDetail = transfer.ReportingDetail ?? new StaffTransferReportingDetail { StaffTransferId = transfer.StaffTransferId };
            repDetail.ApprovedTransferEffectiveDate = input.Job.EffectiveDate.Date;
            repDetail.ReportingDate = input.Reporting.ReportingDate.Date;
            repDetail.ReportingTime = input.Reporting.ReportingTime;
            repDetail.ReportingLocation = input.Reporting.ReportingLocation.Trim();
            repDetail.ReportingToIndividualId = input.Reporting.ReportingToIndividualId;
            repDetail.ReportingToNameSnapshot = input.Reporting.ReportingToName;
            repDetail.HandoverRequired = input.Reporting.HandoverRequired;
            repDetail.HandoverDeadline = input.Reporting.HandoverDeadline?.Date;
            repDetail.OutstandingAssignmentsSummary = input.Reporting.OutstandingAssignmentsSummary;
            repDetail.AssetsToReturnSummary = input.Reporting.AssetsToReturnSummary;
            repDetail.AssetsToReceiveSummary = input.Reporting.AssetsToReceiveSummary;
            repDetail.OutstandingApprovalsRoutingRule = StaffTransferApprovalRoutingRules.All.Contains(input.Reporting.OutstandingApprovalsRoutingRule)
                ? input.Reporting.OutstandingApprovalsRoutingRule
                : StaffTransferApprovalRoutingRules.RetainHistoricalReassignOpen;
            repDetail.OutstandingApprovalsSummary = input.Reporting.OutstandingApprovalsSummary;
            repDetail.SpecialInstructions = input.Reporting.SpecialInstructions;
            if (transfer.ReportingDetail is null) db.StaffTransferReportingDetails.Add(repDetail);

            var audit = await logs.CreateAsync(
                db,
                isNew ? SharedConfig.OperationLogActionTypes.REQUESTS_CREATE : SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,
                $"{(input.SubmitImmediately ? "Submitted" : "Saved")} staff transfer #{transfer.StaffTransferId} (v{transfer.CurrentVersion}) for staff {transfer.IndividualId}.");
            transfer.OperationLogId = audit.OperationLogId;

            if (input.SubmitImmediately)
            {
                if (!transfer.RequestId.HasValue)
                {
                    var reqType = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == RequestTypeName)
                        ?? throw new InvalidOperationException("Run the Internal Staff Transfer database migration to register the request type.");

                    var req = await requests.CreateAsync(db, new CreateRequestDto
                    {
                        RequestTypeId = reqType.RequestTypeId,
                        ApplicantBusinessEntityId = transfer.IndividualId,
                        OrganisationBusinessEntityId = transfer.FromOrganisationId,
                        EffectiveDate = transfer.EffectiveDate,
                        OperationLogId = audit.OperationLogId
                    });
                    req.RequestStateId = SharedConfig.RequestStates.PENDING_STAFF_COMPLETION;
                    transfer.RequestId = req.RequestId;
                    transfer.Request = req;
                    await db.SaveChangesAsync();

                    if (workflow is not null)
                    {
                        try { await workflow.StartAsync(db, req.RequestId, transfer.CurrentJobId); }
                        catch (WorkflowConfigurationException) { /* Uses transfer stage state when tenant custom flow is not configured */ }
                    }
                }

                transfer.Status = StaffTransferStatuses.PendingStaffConfirmation;

                await NotifyIndividualAsync(
                    db,
                    transfer.IndividualId,
                    transfer.FromOrganisationId,
                    "StaffTransferConfirmationRequired",
                    $"Transfer request #{transfer.StaffTransferId} (v{transfer.CurrentVersion}) requires your review and confirmation.",
                    $"{OnlineConfirmationUrl}?id={transfer.StaffTransferId}");

                if (transfer.CurrentSupervisorIndividualId.HasValue)
                {
                    await NotifyIndividualAsync(
                        db,
                        transfer.CurrentSupervisorIndividualId.Value,
                        transfer.FromOrganisationId,
                        "StaffTransferAdvisoryComment",
                        $"Optional supervisor comment invited for Staff Transfer #{transfer.StaffTransferId} ({transfer.FullNameSnapshot}).",
                        $"{PageUrl}?id={transfer.StaffTransferId}");
                }

                if (transfer.CurrentHodIndividualId.HasValue)
                {
                    await NotifyIndividualAsync(
                        db,
                        transfer.CurrentHodIndividualId.Value,
                        transfer.FromOrganisationId,
                        "StaffTransferAdvisoryComment",
                        $"Optional HOD comment invited for Staff Transfer #{transfer.StaffTransferId} ({transfer.FullNameSnapshot}).",
                        $"{PageUrl}?id={transfer.StaffTransferId}");
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return ServiceResult.Ok(input.SubmitImmediately
                ? $"Transfer #{transfer.StaffTransferId} (v{transfer.CurrentVersion}) submitted and sent for employee confirmation."
                : $"Transfer #{transfer.StaffTransferId} (v{transfer.CurrentVersion}) saved as draft.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            await tx.RollbackAsync();
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> AddCommentAsync(
        int staffTransferId,
        string commentKind,
        string commentText,
        bool isVisibleToEmployee = true)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        if (string.IsNullOrWhiteSpace(commentText) || commentText.Trim().Length > 2000)
            return ServiceResult.Fail("Enter a comment of up to 2,000 characters.");

        await using var db = await factory.CreateDbContextAsync();
        var transfer = await db.StaffTransfers.SingleOrDefaultAsync(t => t.StaffTransferId == staffTransferId);
        if (transfer is null) return ServiceResult.Fail("Staff transfer was not found.");

        var role = actor.IndividualId == transfer.IndividualId
            ? StaffTransferParticipantRoles.Employee
            : actor.IndividualId == transfer.CurrentSupervisorIndividualId
                ? StaffTransferParticipantRoles.CurrentSupervisor
                : actor.IndividualId == transfer.CurrentHodIndividualId
                    ? StaffTransferParticipantRoles.CurrentHOD
                    : actor.UserId == transfer.RequestedByUserId
                        ? StaffTransferParticipantRoles.SendingOfficer
                        : isHrOrAdmin
                            ? StaffTransferParticipantRoles.HR
                            : StaffTransferParticipantRoles.ReceivingApprover;

        var kind = StaffTransferCommentKinds.All.Contains(commentKind)
            ? commentKind
            : StaffTransferCommentKinds.AdvisoryComment;

        db.StaffTransferComments.Add(new StaffTransferComment
        {
            StaffTransferId = transfer.StaffTransferId,
            AuthorUserId = actor.UserId,
            AuthorIndividualId = actor.IndividualId,
            ParticipantRole = role,
            CommentKind = kind,
            RequestVersion = transfer.CurrentVersion,
            CommentText = commentText.Trim(),
            IsVisibleToEmployee = isVisibleToEmployee,
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Comment recorded.");
    }

    public async Task<ServiceResult> SubmitEmployeeConfirmationAsync(StaffTransferEmployeeConfirmationInput input)
    {
        var (actor, _, _) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var transfer = await db.StaffTransfers
                .Include(t => t.Request)
                .SingleOrDefaultAsync(t => t.StaffTransferId == input.StaffTransferId)
                ?? throw new InvalidOperationException("Staff transfer request was not found.");

            if (actor.IndividualId != transfer.IndividualId)
                throw new UnauthorizedAccessException("Only the transferring employee can submit the staff confirmation.");

            StaffTransferValidator.ValidateEmployeeConfirmation(input, transfer.CurrentVersion, transfer.Status);

            var detailSnapshot = await LoadExistingTransferDetailAsync(db, transfer.StaffTransferId, actor, transfer.FromOrganisationId, true);
            var snapshotJson = JsonSerializer.Serialize(detailSnapshot);

            db.StaffTransferConfirmations.Add(new StaffTransferConfirmation
            {
                StaffTransferId = transfer.StaffTransferId,
                ConfirmedVersion = transfer.CurrentVersion,
                ResponseKind = input.ResponseKind,
                ConfirmedByUserId = actor.UserId,
                ConfirmedByIndividualId = actor.IndividualId,
                ConfirmationStatement = StaffTransferValidator.DefaultConfirmationStatement,
                EmployeeRemarks = input.EmployeeRemarks?.Trim(),
                ReviewedSnapshotJson = snapshotJson,
                IsInvalidatedByMaterialEdit = false,
                ConfirmedAtUtc = DateTime.UtcNow
            });

            if (input.ResponseKind == StaffTransferConfirmationResponses.Confirmed)
            {
                transfer.ConfirmedVersion = transfer.CurrentVersion;
                transfer.Status = StaffTransferStatuses.PendingReceivingApproval;
                if (transfer.Request is not null)
                {
                    transfer.Request.RequestStateId = SharedConfig.RequestStates.PENDING_APPROVAL;
                    transfer.Request.LastStateChangedByUserId = actor.UserId;
                    transfer.Request.LastStateChangedDate = DateTime.UtcNow;
                }
            }
            else if (input.ResponseKind == StaffTransferConfirmationResponses.CorrectionRequested)
            {
                transfer.ConfirmedVersion = null;
                transfer.Status = StaffTransferStatuses.CorrectionRequested;
                db.StaffTransferComments.Add(new StaffTransferComment
                {
                    StaffTransferId = transfer.StaffTransferId,
                    AuthorUserId = actor.UserId,
                    AuthorIndividualId = actor.IndividualId,
                    ParticipantRole = StaffTransferParticipantRoles.Employee,
                    CommentKind = StaffTransferCommentKinds.CorrectionRequest,
                    RequestVersion = transfer.CurrentVersion,
                    CommentText = input.EmployeeRemarks!.Trim(),
                    IsVisibleToEmployee = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                db.StaffTransferComments.Add(new StaffTransferComment
                {
                    StaffTransferId = transfer.StaffTransferId,
                    AuthorUserId = actor.UserId,
                    AuthorIndividualId = actor.IndividualId,
                    ParticipantRole = StaffTransferParticipantRoles.Employee,
                    CommentKind = StaffTransferCommentKinds.Clarification,
                    RequestVersion = transfer.CurrentVersion,
                    CommentText = input.EmployeeRemarks!.Trim(),
                    IsVisibleToEmployee = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            transfer.UpdatedDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return ServiceResult.Ok(input.ResponseKind == StaffTransferConfirmationResponses.Confirmed
                ? "Transfer information confirmed and routed to the receiving department for final approval."
                : input.ResponseKind == StaffTransferConfirmationResponses.CorrectionRequested
                    ? "Correction request submitted to the sending department."
                    : "Clarification submitted.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            await tx.RollbackAsync();
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> SubmitReceivingDecisionAsync(StaffTransferDecisionInput input)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var transfer = await db.StaffTransfers
                .Include(t => t.Request)
                .SingleOrDefaultAsync(t => t.StaffTransferId == input.StaffTransferId)
                ?? throw new InvalidOperationException("Staff transfer request was not found.");

            var isReceivingApprover = isHrOrAdmin ||
                                      await IsReceivingDepartmentApproverAsync(db, actor, transfer.ToOrganisationId, transfer.ToOrganisationStructureId);
            if (!isReceivingApprover)
                throw new UnauthorizedAccessException("Only an authorized approver for the receiving department can record the final decision.");

            StaffTransferValidator.ValidateReceivingDecision(
                input,
                transfer.Status,
                transfer.CurrentVersion,
                transfer.ConfirmedVersion,
                transfer.RequestedByUserId,
                actor.UserId,
                allowSelfApprovalOverride: actor.IsSuperAdministrator);

            var audit = await logs.CreateAsync(
                db,
                SharedConfig.OperationLogActionTypes.REQUEST_STATE_CHANGE,
                $"Receiving department decision '{input.Decision}' on Staff Transfer #{transfer.StaffTransferId} (v{transfer.CurrentVersion}) by user {actor.UserId}.");
            transfer.OperationLogId = audit.OperationLogId;
            transfer.UpdatedDate = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(input.Remarks))
            {
                db.StaffTransferComments.Add(new StaffTransferComment
                {
                    StaffTransferId = transfer.StaffTransferId,
                    AuthorUserId = actor.UserId,
                    AuthorIndividualId = actor.IndividualId,
                    ParticipantRole = StaffTransferParticipantRoles.ReceivingApprover,
                    CommentKind = input.Decision == "ReturnForCorrection"
                        ? StaffTransferCommentKinds.CorrectionRequest
                        : StaffTransferCommentKinds.ApprovalRemark,
                    RequestVersion = transfer.CurrentVersion,
                    CommentText = input.Remarks.Trim(),
                    IsVisibleToEmployee = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (input.Decision == "ReturnForCorrection")
            {
                transfer.Status = StaffTransferStatuses.CorrectionRequested;
            }
            else if (input.Decision == "Reject")
            {
                transfer.Status = StaffTransferStatuses.Rejected;
                transfer.IsOpen = false;
                transfer.FinalApprovedByUserId = actor.UserId;
                transfer.FinalApprovedAt = DateTime.UtcNow;
                transfer.FinalDecisionRemarks = input.Remarks?.Trim();
                if (transfer.Request is not null)
                {
                    transfer.Request.RequestStateId = SharedConfig.RequestStates.REJECTED;
                    transfer.Request.LastStateChangedByUserId = actor.UserId;
                    transfer.Request.LastStateChangedDate = DateTime.UtcNow;
                    transfer.Request.StateChangeRemarks = input.Remarks?.Trim();
                }

                await NotifyIndividualAsync(
                    db,
                    transfer.IndividualId,
                    transfer.FromOrganisationId,
                    "StaffTransferRejected",
                    $"Staff Transfer #{transfer.StaffTransferId} was rejected by the receiving department.",
                    $"{OnlineConfirmationUrl}?id={transfer.StaffTransferId}");
            }
            else
            {
                transfer.FinalApprovedByUserId = actor.UserId;
                transfer.FinalApprovedAt = DateTime.UtcNow;
                transfer.FinalDecisionRemarks = input.Remarks?.Trim();
                transfer.Status = transfer.EffectiveDate.Date <= TodayMaldives
                    ? StaffTransferStatuses.Approved
                    : StaffTransferStatuses.Scheduled;

                if (transfer.Request is not null)
                {
                    transfer.Request.RequestStateId = SharedConfig.RequestStates.APPROVED;
                    transfer.Request.LastStateChangedByUserId = actor.UserId;
                    transfer.Request.LastStateChangedDate = DateTime.UtcNow;
                    transfer.Request.StateChangeRemarks = input.Remarks?.Trim();
                }

                await db.SaveChangesAsync();

                // Generate both EmployeeNotice and RestrictedAdminRecord immutable document snapshots
                await CreateApprovedDocumentSnapshotsAsync(db, transfer.StaffTransferId, actor);

                await NotifyIndividualAsync(
                    db,
                    transfer.IndividualId,
                    transfer.ToOrganisationId,
                    "StaffTransferApproved",
                    $"Your transfer (#{transfer.StaffTransferId}) has been approved by the receiving department effective {transfer.EffectiveDate:dd MMM yyyy}.",
                    $"{OnlineConfirmationUrl}?id={transfer.StaffTransferId}");
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            if (input.Decision == "Approve" && transfer.EffectiveDate.Date <= TodayMaldives)
            {
                var actResult = await activation.ActivateAsync(transfer.StaffTransferId);
                return ServiceResult.Ok(actResult.Succeeded
                    ? $"Transfer approved and activated effective {transfer.EffectiveDate:dd MMM yyyy}."
                    : $"Transfer approved; activation queued ({actResult.Message}).");
            }

            return ServiceResult.Ok(input.Decision switch
            {
                "Approve" => $"Transfer approved and scheduled for activation on {transfer.EffectiveDate:dd MMM yyyy}.",
                "Reject" => "Transfer request rejected.",
                _ => "Transfer returned to sending department for correction."
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            await tx.RollbackAsync();
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<TransferActivationResult> ActivateTransferAsync(int staffTransferId)
    {
        var (_, _, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.StaffTransferActivate))
            throw new UnauthorizedAccessException("You do not have permission to activate transfers.");

        return await activation.ActivateAsync(staffTransferId);
    }

    public async Task<ServiceResult> UpdateDoorAccessProvisioningAsync(
        int staffTransferDoorAccessId,
        string provisioningState,
        string? notes)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var item = await db.StaffTransferDoorAccesses
            .Include(d => d.StaffTransfer)
            .SingleOrDefaultAsync(d => d.StaffTransferDoorAccessId == staffTransferDoorAccessId);
        if (item is null) return ServiceResult.Fail("Door access provisioning item was not found.");

        if (!isHrOrAdmin && item.SecurityOfficerUserId != actor.UserId &&
            !await access.HasPermissionAsync(Permissions.StaffTransferProvisionAccess))
            return ServiceResult.Fail("Only an authorized security administrator can update door access provisioning.");

        item.ProvisioningState = provisioningState;
        item.ProvisioningNotes = notes?.Trim();
        if (provisioningState is StaffTransferDoorAccessStates.StateProvisioned or StaffTransferDoorAccessStates.StateVerified or StaffTransferDoorAccessStates.StateRevoked)
            item.ProvisionedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Door access provisioning updated to '{provisioningState}'.");
    }

    public async Task<StaffTransferPrintableDocumentDto> GetPrintableDocumentAsync(
        int staffTransferId,
        string variant = StaffTransferDocumentVariants.EmployeeNotice)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var normalizedVariant = variant == StaffTransferDocumentVariants.RestrictedAdminRecord
            ? StaffTransferDocumentVariants.RestrictedAdminRecord
            : StaffTransferDocumentVariants.EmployeeNotice;

        if (normalizedVariant == StaffTransferDocumentVariants.RestrictedAdminRecord &&
            !isHrOrAdmin && !await access.HasPermissionAsync(Permissions.StaffTransferExportRestrictedPdf))
        {
            throw new UnauthorizedAccessException("You do not have permission to export the restricted administrative transfer record.");
        }

        var savedDoc = await db.StaffTransferDocuments.AsNoTracking()
            .Where(d => d.StaffTransferId == staffTransferId && d.DocumentVariant == normalizedVariant)
            .OrderByDescending(d => d.RequestVersion)
            .ThenByDescending(d => d.GeneratedAtUtc)
            .FirstOrDefaultAsync();

        if (savedDoc is not null)
        {
            var deserialized = JsonSerializer.Deserialize<StaffTransferPrintableDocumentDto>(savedDoc.SnapshotJson);
            if (deserialized is not null)
                return deserialized;
        }

        // Build print-preview from current versioned state (e.g., before employee confirmation)
        var detail = await LoadExistingTransferDetailAsync(db, staffTransferId, actor, org, isHrOrAdmin);
        return BuildPrintableDtoFromDetail(detail, normalizedVariant, $"STR-DOC-{staffTransferId:D4}-V{detail.CurrentVersion}");
    }

    private async Task CreateApprovedDocumentSnapshotsAsync(HrmTeContext db, int staffTransferId, UserContextDto actor)
    {
        var detail = await LoadExistingTransferDetailAsync(db, staffTransferId, actor, 0, true);
        foreach (var variant in new[] { StaffTransferDocumentVariants.EmployeeNotice, StaffTransferDocumentVariants.RestrictedAdminRecord })
        {
            var docNo = $"STR-{staffTransferId:D4}-V{detail.CurrentVersion}-{(variant == StaffTransferDocumentVariants.RestrictedAdminRecord ? "ADM" : "EMP")}";
            if (await db.StaffTransferDocuments.AnyAsync(d => d.DocumentNumber == docNo))
                continue;

            var printable = BuildPrintableDtoFromDetail(detail, variant, docNo);
            var json = JsonSerializer.Serialize(printable);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            printable.Sha256Hash = hash;
            json = JsonSerializer.Serialize(printable);

            db.StaffTransferDocuments.Add(new StaffTransferDocument
            {
                StaffTransferId = staffTransferId,
                DocumentNumber = docNo,
                DocumentVariant = variant,
                RequestVersion = detail.CurrentVersion,
                ConfirmedVersion = detail.ConfirmedVersion,
                SnapshotJson = json,
                Sha256Hash = hash,
                ContentType = "application/pdf",
                GeneratedByUserId = actor.UserId,
                GeneratedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static StaffTransferPrintableDocumentDto BuildPrintableDtoFromDetail(
        StaffTransferDetailDto detail,
        string variant,
        string documentNumber)
    {
        var isRestricted = variant == StaffTransferDocumentVariants.RestrictedAdminRecord;
        var latestConfirmation = detail.Confirmations.FirstOrDefault(c => !c.IsInvalidatedByMaterialEdit);

        var staffCopy = new StaffTransferStaffTabDto
        {
            IndividualId = detail.Staff.IndividualId,
            FullName = detail.Staff.FullName,
            EmployeeNumber = detail.Staff.EmployeeNumber,
            IdCardNumber = isRestricted ? detail.Staff.IdCardNumber : null,
            CurrentOrganisationId = detail.Staff.CurrentOrganisationId,
            CurrentOrganisationName = detail.Staff.CurrentOrganisationName,
            CurrentOrganisationStructureId = detail.Staff.CurrentOrganisationStructureId,
            CurrentDepartmentName = detail.Staff.CurrentDepartmentName,
            CurrentEmploymentStatus = detail.Staff.CurrentEmploymentStatus,
            DateJoined = detail.Staff.DateJoined,
            OfficialEmail = detail.Staff.OfficialEmail,
            OfficialPhone = detail.Staff.OfficialPhone,
            CurrentSupervisorName = detail.Staff.CurrentSupervisorName,
            CurrentHodName = detail.Staff.CurrentHodName,
            EmploymentCategory = detail.Staff.EmploymentCategory,
            CurrentJobId = detail.Staff.CurrentJobId,
            CurrentPositionTitle = detail.Staff.CurrentPositionTitle
        };

        var jobCopy = new StaffTransferJobTabDto
        {
            ExistingJobId = detail.Job.ExistingJobId,
            ExistingPositionTitle = detail.Job.ExistingPositionTitle,
            ExistingGradeLevel = detail.Job.ExistingGradeLevel,
            ExistingBasicSalary = isRestricted ? detail.Job.ExistingBasicSalary : null,
            ExistingAllowance = isRestricted ? detail.Job.ExistingAllowance : null,
            DestinationOrganisationId = detail.Job.DestinationOrganisationId,
            DestinationOrganisationStructureId = detail.Job.DestinationOrganisationStructureId,
            DestinationPositionTitle = detail.Job.DestinationPositionTitle,
            DestinationGradeLevel = detail.Job.DestinationGradeLevel,
            TransferTypeId = detail.Job.TransferTypeId,
            TransferAppointmentMode = detail.Job.TransferAppointmentMode,
            HasSalaryBenefitChange = detail.Job.HasSalaryBenefitChange,
            ProposedBasicSalary = isRestricted ? detail.Job.ProposedBasicSalary : null,
            ProposedAllowance = isRestricted ? detail.Job.ProposedAllowance : null,
            SalaryBenefitChangeNotes = isRestricted ? detail.Job.SalaryBenefitChangeNotes : null,
            EffectiveDate = detail.Job.EffectiveDate,
            TransferReason = detail.Job.TransferReason
        };

        var doc = new StaffTransferPrintableDocumentDto
        {
            StaffTransferId = detail.StaffTransferId,
            DocumentNumber = documentNumber,
            DocumentVariant = variant,
            RequestReferenceNumber = detail.ReferenceNumber,
            IssueDateUtc = detail.FinalApprovedAt ?? DateTime.UtcNow,
            RequestVersion = detail.CurrentVersion,
            ConfirmedVersion = detail.ConfirmedVersion,
            Status = detail.Status,
            OrganisationBrandingName = detail.Staff.CurrentOrganisationName,
            Staff = staffCopy,
            Job = jobCopy,
            Work = detail.Work,
            AccessChanges = detail.AccessChanges,
            DoorAccessChanges = isRestricted
                ? detail.DoorAccessChanges
                : detail.DoorAccessChanges.Select(d => new StaffTransferDoorAccessItemDto
                {
                    BuildingName = d.BuildingName,
                    FloorName = d.FloorName,
                    DepartmentOfficeArea = d.DepartmentOfficeArea,
                    TransitionAction = d.TransitionAction,
                    AccessStartDate = d.AccessStartDate,
                    ProvisioningState = d.ProvisioningState
                }).ToList(),
            Reporting = detail.Reporting,
            ConfirmationStatement = latestConfirmation?.ConfirmationStatement ?? StaffTransferValidator.DefaultConfirmationStatement,
            ConfirmedByName = latestConfirmation?.ConfirmedByName,
            ConfirmedAtUtc = latestConfirmation?.ConfirmedAtUtc,
            EmployeeRemarks = latestConfirmation?.EmployeeRemarks,
            SupervisorComments = detail.Comments.Where(c => c.ParticipantRole == StaffTransferParticipantRoles.CurrentSupervisor).ToList(),
            HodComments = detail.Comments.Where(c => c.ParticipantRole == StaffTransferParticipantRoles.CurrentHOD).ToList(),
            FinalApprovedByName = detail.FinalApprovedByName,
            FinalApprovedAtUtc = detail.FinalApprovedAt,
            FinalDecisionRemarks = detail.FinalDecisionRemarks
        };

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(doc)));
        doc.Sha256Hash = Convert.ToHexString(hashBytes);
        return doc;
    }

    private static async Task<bool> IsDepartmentAuthorizedOfficerAsync(
        HrmTeContext db,
        UserContextDto actor,
        int organisationId,
        int? structureId)
    {
        if (actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin)
            return true;

        return await db.OrganisationStructureSupervisors.AnyAsync(s =>
                   s.SupervisorStaffId == actor.IndividualId && s.IsActive && s.IsValid &&
                   (!structureId.HasValue || s.OrganisationStructureId == structureId.Value) &&
                   s.OrganisationStructure.OrganisationBusinessEntityID == organisationId) ||
               await db.OrganisationStructureHeadIncharges.AnyAsync(h =>
                   h.IndividualId == actor.IndividualId && h.IsValid &&
                   (!structureId.HasValue || h.OrganisationStructureId == structureId.Value) &&
                   h.OrganisationStructure.OrganisationBusinessEntityID == organisationId);
    }

    private async Task<bool> IsReceivingDepartmentApproverAsync(
        HrmTeContext db,
        UserContextDto actor,
        int toOrganisationId,
        int toStructureId)
    {
        if (actor.IsSuperAdministrator || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin ||
            await access.HasPermissionAsync(Permissions.StaffTransferApprove))
            return true;

        return await db.OrganisationStructureHeadIncharges.AnyAsync(h =>
                   h.IndividualId == actor.IndividualId && h.IsValid &&
                   h.OrganisationStructureId == toStructureId &&
                   h.OrganisationStructure.OrganisationBusinessEntityID == toOrganisationId) ||
               await db.OrganisationStructureSupervisors.AnyAsync(s =>
                   s.SupervisorStaffId == actor.IndividualId && s.IsActive && s.IsValid &&
                   s.OrganisationStructureId == toStructureId &&
                   s.OrganisationStructure.OrganisationBusinessEntityID == toOrganisationId);
    }

    private static async Task NotifyIndividualAsync(
        HrmTeContext db,
        int individualId,
        int organisationId,
        string kind,
        string message,
        string url)
    {
        var userId = await db.Users
            .Where(u => u.BusinessEntityID == individualId && u.IsActive)
            .Select(u => (int?)u.UserId)
            .FirstOrDefaultAsync();

        if (!userId.HasValue) return;

        db.StaffNotifications.Add(new StaffNotification
        {
            UserId = userId.Value,
            IndividualId = individualId,
            OrganisationId = organisationId,
            Kind = kind,
            Message = message,
            Url = url,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
