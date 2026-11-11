using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HRM.Authorization;
using HRM.DTOs;
using HRM.DTOs.Separation;
using HRM.DTOs.UserContext;
using HRM.Enums;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class StaffSeparationService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access)
    : global::HRM.Services.Contracts.StaffSeparationServiceContracts,
      global::HRM.Services.Contracts.StaffSeparationServiceContracts.IOperations
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
                          await access.HasPermissionAsync(Permissions.StaffSeparationInitiate) ||
                          await access.HasPermissionAsync(Permissions.StaffSeparationClearanceReview) ||
                          await access.HasPermissionAsync(Permissions.StaffSeparationFinalApprove) ||
                          await access.HasPermissionAsync(Permissions.StaffSeparationExecute);

        return (actor, org, isHrOrAdmin);
    }

    private static string FullName(Individual? i) =>
        i == null ? "" : string.Join(" ", new[] { i.FirstNameEnglish, i.MiddleNameEnglish, i.LastNameEnglish }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));

    public async Task<StaffSeparationDashboardData> LoadDashboardAsync(int? organisationId = null, string? statusFilter = null, string? search = null)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();

        var query = db.StaffSeparations
            .AsNoTracking()
            .Include(s => s.SeparationType)
            .Include(s => s.Individual)
            .Include(s => s.ClearanceTasks)
            .Where(s => s.OrganisationId == org);

        if (!isHrOrAdmin)
        {
            query = query.Where(s => s.IndividualId == actor.IndividualId || s.SupervisorIndividualId == actor.IndividualId);
        }

        var totalCount = await query.CountAsync();
        var pendingHandoverCount = await query.CountAsync(s => s.Status == StaffSeparationStatuses.HandoverInProgress || s.Status == StaffSeparationStatuses.Submitted);
        var pendingClearanceCount = await query.CountAsync(s => s.Status == StaffSeparationStatuses.ClearanceInProgress);
        var pendingFinalReviewCount = await query.CountAsync(s => s.Status == StaffSeparationStatuses.FinalReviewPending || s.Status == StaffSeparationStatuses.Approved);
        var terminatedCount = await query.CountAsync(s => s.Status == StaffSeparationStatuses.Terminated);
        var unresolvedCount = await query.CountAsync(s => s.HasUnresolvedObligations);

        if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (statusFilter == "Unresolved")
                query = query.Where(s => s.HasUnresolvedObligations);
            else
                query = query.Where(s => s.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.FullNameSnapshot.ToLower().Contains(term) ||
                s.EmployeeNumberSnapshot.ToLower().Contains(term) ||
                s.DepartmentNameSnapshot.ToLower().Contains(term) ||
                s.PositionTitleSnapshot.ToLower().Contains(term));
        }

        var items = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(100)
            .Select(s => new StaffSeparationSummaryDto
            {
                StaffSeparationId = s.StaffSeparationId,
                IndividualId = s.IndividualId,
                JobId = s.JobId,
                FullName = s.FullNameSnapshot,
                EmployeeNumber = s.EmployeeNumberSnapshot,
                DepartmentName = s.DepartmentNameSnapshot,
                PositionTitle = s.PositionTitleSnapshot,
                SeparationTypeName = s.SeparationType.Name,
                ReasonCategory = s.ReasonCategory,
                RequestedNoticeDate = s.RequestedNoticeDate,
                LastWorkingDate = s.LastWorkingDate,
                EffectiveSeparationDate = s.EffectiveSeparationDate,
                Status = s.Status,
                HasUnresolvedObligations = s.HasUnresolvedObligations,
                EmployeeAcknowledgementStatus = s.EmployeeAcknowledgementStatus,
                TotalTasksCount = s.ClearanceTasks.Count,
                ClearedTasksCount = s.ClearanceTasks.Count(t => t.Status == StaffSeparationClearanceTaskStatuses.Cleared || t.Status == StaffSeparationClearanceTaskStatuses.Waived)
            })
            .ToListAsync();

        var types = await db.SeparationTypes
            .AsNoTracking()
            .Where(t => t.IsActive && (t.OrganisationId == null || t.OrganisationId == org))
            .OrderBy(t => t.SortOrder)
            .Select(t => new SeparationTypeDto
            {
                SeparationTypeId = t.SeparationTypeId,
                Code = t.Code,
                Name = t.Name,
                Category = t.Category,
                DefaultNoticeDays = t.DefaultNoticeDays,
                RequiresClearance = t.RequiresClearance,
                RequiresExitInterview = t.RequiresExitInterview,
                AllowsNoticeWaiver = t.AllowsNoticeWaiver,
                Description = t.Description,
                IsActive = t.IsActive
            })
            .ToListAsync();

        return new StaffSeparationDashboardData
        {
            TotalCount = totalCount,
            PendingHandoverCount = pendingHandoverCount,
            PendingClearanceCount = pendingClearanceCount,
            PendingFinalReviewCount = pendingFinalReviewCount,
            TerminatedCount = terminatedCount,
            UnresolvedObligationsCount = unresolvedCount,
            Separations = items,
            SeparationTypes = types
        };
    }

    public async Task<StaffSeparationFormLookupsDto> LoadFormLookupsAsync(int? organisationId = null)
    {
        var (_, org, _) = await ScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();

        var types = await db.SeparationTypes
            .AsNoTracking()
            .Where(t => t.IsActive && (t.OrganisationId == null || t.OrganisationId == org))
            .OrderBy(t => t.SortOrder)
            .Select(t => new SeparationTypeDto
            {
                SeparationTypeId = t.SeparationTypeId,
                Code = t.Code,
                Name = t.Name,
                Category = t.Category,
                DefaultNoticeDays = t.DefaultNoticeDays,
                RequiresClearance = t.RequiresClearance,
                RequiresExitInterview = t.RequiresExitInterview,
                AllowsNoticeWaiver = t.AllowsNoticeWaiver,
                Description = t.Description,
                IsActive = t.IsActive
            })
            .ToListAsync();

        var departments = await db.OrganisationStructures
            .AsNoTracking()
            .Where(s => s.OrganisationBusinessEntityID == org)
            .OrderBy(s => s.Name)
            .Select(s => new LookupItemDto
            {
                Id = s.OrganisationStructureId,
                Text = s.Name
            })
            .ToListAsync();

        var today = TodayMaldives;
        var activeJobs = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Individual)
            .Include(j => j.OrganisationStructure)
            .Where(j => j.OrganisationID == org && j.JobStateId == 4 && j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate > today))
            .OrderBy(j => j.Individual.FirstNameEnglish)
            .Select(j => new LookupItemDto
            {
                Id = j.IndividualID,
                Text = (j.Individual.FirstNameEnglish + " " + (j.Individual.LastNameEnglish ?? "")).Trim(),
                SecondaryText = j.OrganisationStructure != null ? j.OrganisationStructure.Name : ""
            })
            .ToListAsync();

        return new StaffSeparationFormLookupsDto
        {
            SeparationTypes = types,
            Departments = departments,
            ActiveStaff = activeJobs
        };
    }

    public async Task<StaffSeparationDetailDto> LoadDetailAsync(int? staffSeparationId = null, int? individualId = null, int? jobId = null)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var query = db.StaffSeparations
            .AsNoTracking()
            .Include(s => s.SeparationType)
            .Include(s => s.WorkHandover).ThenInclude(h => h!.Items)
            .Include(s => s.ClearanceTasks)
            .Include(s => s.AssetClearances)!.ThenInclude(a => a.AssetItem)
            .Include(s => s.FinanceItems)
            .Include(s => s.AccessRevocations)
            .Include(s => s.Documents)
            .Include(s => s.Events)
            .AsQueryable();

        if (staffSeparationId.HasValue && staffSeparationId.Value > 0)
        {
            query = query.Where(s => s.StaffSeparationId == staffSeparationId.Value);
        }
        else if (individualId.HasValue && individualId.Value > 0)
        {
            query = query.Where(s => s.IndividualId == individualId.Value && s.OrganisationId == org).OrderByDescending(s => s.CreatedAtUtc);
        }
        else if (jobId.HasValue && jobId.Value > 0)
        {
            query = query.Where(s => s.JobId == jobId.Value).OrderByDescending(s => s.CreatedAtUtc);
        }
        else
        {
            query = query.Where(s => s.IndividualId == actor.IndividualId && s.OrganisationId == org).OrderByDescending(s => s.CreatedAtUtc);
        }

        var sep = await query.FirstOrDefaultAsync();
        if (sep == null)
            throw new KeyNotFoundException("Staff separation record not found.");

        if (!isHrOrAdmin && sep.IndividualId != actor.IndividualId && sep.SupervisorIndividualId != actor.IndividualId)
            throw new UnauthorizedAccessException("You do not have permission to view this separation record.");

        var relevantIndividualIds = new HashSet<int> { sep.IndividualId };
        if (sep.SupervisorIndividualId.HasValue) relevantIndividualIds.Add(sep.SupervisorIndividualId.Value);
        if (sep.WorkHandover != null)
        {
            relevantIndividualIds.Add(sep.WorkHandover.SupervisorIndividualId);
            if (sep.WorkHandover.HodIndividualId.HasValue) relevantIndividualIds.Add(sep.WorkHandover.HodIndividualId.Value);
            foreach (var item in sep.WorkHandover.Items)
            {
                relevantIndividualIds.Add(item.RecipientIndividualId);
                if (item.VerifiedByIndividualId.HasValue) relevantIndividualIds.Add(item.VerifiedByIndividualId.Value);
            }
        }

        var allIndividuals = await db.Individuals
            .AsNoTracking()
            .Where(i => relevantIndividualIds.Contains(i.BusinessEntityId))
            .ToDictionaryAsync(i => i.BusinessEntityId, FullName);

        var workHandoverDto = sep.WorkHandover == null ? null : new WorkHandoverDetailDto
        {
            WorkHandoverId = sep.WorkHandover.WorkHandoverId,
            StaffSeparationId = sep.WorkHandover.StaffSeparationId,
            SupervisorIndividualId = sep.WorkHandover.SupervisorIndividualId,
            SupervisorName = allIndividuals.GetValueOrDefault(sep.WorkHandover.SupervisorIndividualId, sep.SupervisorNameSnapshot ?? "Supervisor"),
            HodIndividualId = sep.WorkHandover.HodIndividualId,
            HodName = sep.WorkHandover.HodIndividualId.HasValue ? allIndividuals.GetValueOrDefault(sep.WorkHandover.HodIndividualId.Value, "HOD") : null,
            SupervisorStatus = sep.WorkHandover.SupervisorStatus,
            SupervisorReviewedAtUtc = sep.WorkHandover.SupervisorReviewedAtUtc,
            SupervisorComments = sep.WorkHandover.SupervisorComments,
            HodStatus = sep.WorkHandover.HodStatus,
            HodReviewedAtUtc = sep.WorkHandover.HodReviewedAtUtc,
            HodComments = sep.WorkHandover.HodComments,
            AllItemsCompleted = sep.WorkHandover.AllItemsCompleted,
            Items = sep.WorkHandover.Items.OrderBy(i => i.TargetCompletionDate).Select(i => new WorkHandoverItemDto
            {
                WorkHandoverItemId = i.WorkHandoverItemId,
                WorkHandoverId = i.WorkHandoverId,
                StaffSeparationId = i.StaffSeparationId,
                ItemType = i.ItemType,
                ItemTitle = i.ItemTitle,
                Details = i.Details,
                RecipientIndividualId = i.RecipientIndividualId,
                RecipientName = allIndividuals.GetValueOrDefault(i.RecipientIndividualId, "Colleague"),
                TargetCompletionDate = i.TargetCompletionDate,
                Status = i.Status,
                HandoverNotes = i.HandoverNotes,
                VerifiedAtUtc = i.VerifiedAtUtc
            }).ToList()
        };

        var clearanceTasksDto = sep.ClearanceTasks.OrderBy(t => t.SequenceOrder).Select(t => new ClearanceTaskDto
        {
            ClearanceTaskId = t.ClearanceTaskId,
            StaffSeparationId = t.StaffSeparationId,
            Domain = t.Domain,
            TaskName = t.TaskName,
            TaskDescription = t.TaskDescription,
            AssignedOrganisationStructureId = t.AssignedOrganisationStructureId,
            Status = t.Status,
            IsMandatory = t.IsMandatory,
            SequenceOrder = t.SequenceOrder,
            ClearedByName = t.ClearedByUserId.HasValue ? $"User #{t.ClearedByUserId}" : null,
            ClearedAtUtc = t.ClearedAtUtc,
            Remarks = t.Remarks,
            WaiverReason = t.WaiverReason,
            OutstandingObligation = t.OutstandingObligation,
            OutstandingObligationAmount = t.OutstandingObligationAmount,
            OutstandingObligationDetails = t.OutstandingObligationDetails
        }).ToList();

        var assetClearancesDto = sep.AssetClearances.Select(a => new AssetClearanceDto
        {
            SeparationAssetClearanceId = a.SeparationAssetClearanceId,
            StaffSeparationId = a.StaffSeparationId,
            AssetCustodyId = a.AssetCustodyId,
            AssetItemId = a.AssetItemId,
            AssetCode = a.AssetItem?.AssetCode ?? "",
            AssetName = a.AssetItem?.Name ?? "",
            SerialNumber = a.AssetItem?.SerialNumber,
            CategoryName = "General Asset",
            DisposalAction = a.DisposalAction,
            AssessedDamageCost = a.AssessedDamageCost,
            DeductionFromFinalSettlement = a.DeductionFromFinalSettlement,
            Status = a.Status,
            VerifiedByName = a.VerifiedByUserId.HasValue ? $"Officer #{a.VerifiedByUserId}" : null,
            VerifiedAtUtc = a.VerifiedAtUtc,
            Remarks = a.Remarks
        }).ToList();

        var financeItemsDto = sep.FinanceItems.Select(f => new FinanceItemDto
        {
            SeparationFinanceItemId = f.SeparationFinanceItemId,
            StaffSeparationId = f.StaffSeparationId,
            Category = f.Category,
            Description = f.Description,
            IsCredit = f.IsCredit,
            Amount = f.Amount,
            IsCleared = f.IsCleared,
            PayrollBatchReference = f.PayrollBatchReference,
            Remarks = f.Remarks,
            VerifiedByName = f.VerifiedByUserId.HasValue ? $"Finance #{f.VerifiedByUserId}" : null,
            VerifiedAtUtc = f.VerifiedAtUtc
        }).ToList();

        var accessRevocationsDto = sep.AccessRevocations.Select(r => new AccessRevocationDto
        {
            SeparationAccessRevocationId = r.SeparationAccessRevocationId,
            StaffSeparationId = r.StaffSeparationId,
            SystemType = r.SystemType,
            SystemName = r.SystemName,
            AccountIdentifier = r.AccountIdentifier,
            DoorBuildingName = r.DoorBuildingName,
            DoorCardNumber = r.DoorCardNumber,
            RevocationAction = r.RevocationAction,
            ScheduledRevocationDate = r.ScheduledRevocationDate,
            Status = r.Status,
            RevokedAtUtc = r.RevokedAtUtc,
            RevokedByName = r.RevokedByUserId.HasValue ? $"Security #{r.RevokedByUserId}" : null,
            ExecutionNotes = r.ExecutionNotes
        }).ToList();

        var documentsDto = sep.Documents.OrderByDescending(d => d.GeneratedAtUtc).Select(d => new SeparationDocumentDto
        {
            SeparationDocumentId = d.SeparationDocumentId,
            StaffSeparationId = d.StaffSeparationId,
            DocumentNumber = d.DocumentNumber,
            DocumentVariant = d.DocumentVariant,
            Title = d.Title,
            ContentType = d.ContentType,
            Sha256Hash = d.Sha256Hash,
            Base64Data = d.PdfBytes != null ? Convert.ToBase64String(d.PdfBytes) : null,
            GeneratedAtUtc = d.GeneratedAtUtc,
            GeneratedByName = $"User #{d.GeneratedByUserId}"
        }).ToList();

        var eventsDto = sep.Events.OrderByDescending(e => e.OccurredAtUtc).Select(e => new SeparationEventDto
        {
            SeparationEventId = e.SeparationEventId,
            StaffSeparationId = e.StaffSeparationId,
            EventType = e.EventType,
            EventTitle = e.EventTitle,
            DetailsJson = e.DetailsJson,
            ActorName = e.ActorName,
            OccurredAtUtc = e.OccurredAtUtc
        }).ToList();

        var totalPayable = financeItemsDto.Where(f => f.IsCredit).Sum(f => f.Amount);
        var totalDeduction = financeItemsDto.Where(f => !f.IsCredit).Sum(f => f.Amount);

        return new StaffSeparationDetailDto
        {
            StaffSeparationId = sep.StaffSeparationId,
            RequestId = sep.RequestId,
            IndividualId = sep.IndividualId,
            JobId = sep.JobId,
            OrganisationId = sep.OrganisationId,
            OrganisationStructureId = sep.OrganisationStructureId,
            SeparationTypeId = sep.SeparationTypeId,
            SeparationTypeName = sep.SeparationType.Name,
            ReasonCategory = sep.ReasonCategory,
            ReasonDetails = sep.ReasonDetails,
            InitiationSource = sep.InitiationSource,
            RequestedNoticeDate = sep.RequestedNoticeDate,
            ContractualNoticeDays = sep.ContractualNoticeDays,
            NoticeWaiverRequested = sep.NoticeWaiverRequested,
            NoticeWaiverApproved = sep.NoticeWaiverApproved,
            NoticeWaiverDays = sep.NoticeWaiverDays,
            NoticeShortfallDays = sep.NoticeShortfallDays,
            NoticeShortfallPenaltyAmount = sep.NoticeShortfallPenaltyAmount,
            LastWorkingDate = sep.LastWorkingDate,
            EffectiveSeparationDate = sep.EffectiveSeparationDate,
            Status = sep.Status,
            EmployeeAcknowledgementStatus = sep.EmployeeAcknowledgementStatus,
            EmployeeAcknowledgedAtUtc = sep.EmployeeAcknowledgedAtUtc,
            EmployeeExitComments = sep.EmployeeExitComments,
            ExitInterviewCompleted = sep.ExitInterviewCompleted,
            ExitInterviewDate = sep.ExitInterviewDate,
            ExitInterviewNotes = sep.ExitInterviewNotes,
            FullNameSnapshot = sep.FullNameSnapshot,
            EmployeeNumberSnapshot = sep.EmployeeNumberSnapshot,
            ProtectedIdCardSnapshot = sep.ProtectedIdCardSnapshot,
            PositionTitleSnapshot = sep.PositionTitleSnapshot,
            OrganisationNameSnapshot = sep.OrganisationNameSnapshot,
            DepartmentNameSnapshot = sep.DepartmentNameSnapshot,
            DateJoinedSnapshot = sep.DateJoinedSnapshot,
            BasicSalarySnapshot = sep.BasicSalarySnapshot,
            SupervisorIndividualId = sep.SupervisorIndividualId,
            SupervisorNameSnapshot = sep.SupervisorNameSnapshot,
            OfficialEmailSnapshot = sep.OfficialEmailSnapshot,
            PersonalContactNumber = sep.PersonalContactNumber,
            PersonalEmail = sep.PersonalEmail,
            ForwardingAddress = sep.ForwardingAddress,
            HrAdminReviewerUserId = sep.HrAdminReviewerUserId,
            HrAdminReviewedAtUtc = sep.HrAdminReviewedAtUtc,
            HrReviewRemarks = sep.HrReviewRemarks,
            TerminationExecutedAtUtc = sep.TerminationExecutedAtUtc,
            HasUnresolvedObligations = sep.HasUnresolvedObligations,
            UnresolvedObligationsSummary = sep.UnresolvedObligationsSummary,
            CreatedAtUtc = sep.CreatedAtUtc,
            WorkHandover = workHandoverDto,
            ClearanceTasks = clearanceTasksDto,
            AssetClearances = assetClearancesDto,
            FinanceItems = financeItemsDto,
            AccessRevocations = accessRevocationsDto,
            Documents = documentsDto,
            Events = eventsDto,
            TotalPayableAmount = totalPayable,
            TotalDeductionAmount = totalDeduction
        };
    }

    public async Task<ServiceResult> InitiateSeparationAsync(StaffSeparationInitiateInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var individualId = input.IndividualId > 0 ? input.IndividualId : actor.IndividualId;
        if (!isHrOrAdmin && individualId != actor.IndividualId)
            return ServiceResult.Fail("You can only initiate a separation request for yourself.");

        var activeJob = await db.Jobs
            .Include(j => j.Individual)
            .Include(j => j.Organisation)
            .Include(j => j.OrganisationStructure)
            .Include(j => j.JobPositions)
            .FirstOrDefaultAsync(j => j.IndividualID == individualId && j.OrganisationID == org && j.JobStateId == 4);

        if (activeJob == null)
            return ServiceResult.Fail("No active employment found for this individual in the selected organisation.");

        // Check rule 1: Prevent conflicting active separation requests for the same job
        var hasActiveSeparation = await db.StaffSeparations.AnyAsync(s =>
            s.JobId == activeJob.JobId &&
            s.Status != StaffSeparationStatuses.Terminated &&
            s.Status != StaffSeparationStatuses.Cancelled &&
            s.Status != StaffSeparationStatuses.Rejected);

        if (hasActiveSeparation)
            return ServiceResult.Fail("An active separation request already exists for this employment.");

        var sepType = await db.SeparationTypes.FindAsync(input.SeparationTypeId);
        if (sepType == null || !sepType.IsActive)
            return ServiceResult.Fail("Selected separation type is invalid or inactive.");

        var requestedNoticeDate = input.RequestedNoticeDate == default ? TodayMaldives : input.RequestedNoticeDate.Date;
        var lastWorkingDate = input.LastWorkingDate == default ? requestedNoticeDate.AddDays(sepType.DefaultNoticeDays) : input.LastWorkingDate.Date;
        var effectiveDate = input.EffectiveSeparationDate == default ? lastWorkingDate : input.EffectiveSeparationDate.Date;

        if (effectiveDate < lastWorkingDate)
            return ServiceResult.Fail("Effective separation date cannot be before the last working day.");

        var daysNotice = (lastWorkingDate - requestedNoticeDate).Days;
        var shortfallDays = Math.Max(0, sepType.DefaultNoticeDays - daysNotice);
        if (input.NoticeWaiverRequested && input.NoticeWaiverDays > 0)
        {
            shortfallDays = Math.Max(0, shortfallDays - input.NoticeWaiverDays);
        }

        var dailyRate = (activeJob.BasicSalary ?? 0m) / 30m;
        var shortfallPenalty = shortfallDays * dailyRate;

        // Snapshot details
        var fullName = FullName(activeJob.Individual);
        var employeeNumber = activeJob.Individual.BusinessEntityId.ToString();
        var department = activeJob.OrganisationStructure?.Name ?? "Department";
        var positionTitle = "Employee";
        var activePosition = activeJob.JobPositions.FirstOrDefault(p => p.JobPositionStateId == 4 && (!p.ToDate.HasValue || p.ToDate > TodayMaldives));
        if (activePosition != null)
        {
            var pos = await db.Positions.FindAsync(activePosition.PositionId);
            if (pos != null) positionTitle = pos.Name;
        }

        var supervisorIndividualId = activeJob.OrganisationStructure?.OrganisationStructureSupervisors?
            .Select(s => (int?)s.SupervisorStaffId).FirstOrDefault();

        var separation = new StaffSeparation
        {
            IndividualId = activeJob.IndividualID,
            JobId = activeJob.JobId,
            OrganisationId = org,
            OrganisationStructureId = activeJob.OrganisationStructureId,
            SeparationTypeId = sepType.SeparationTypeId,
            ReasonCategory = sepType.Category,
            ReasonDetails = input.ReasonDetails,
            InitiationSource = isHrOrAdmin && activeJob.IndividualID != actor.IndividualId ? StaffSeparationInitiationSources.HRAdmin : StaffSeparationInitiationSources.EmployeeSelf,
            RequestedNoticeDate = requestedNoticeDate,
            ContractualNoticeDays = sepType.DefaultNoticeDays,
            NoticeWaiverRequested = input.NoticeWaiverRequested,
            NoticeWaiverDays = input.NoticeWaiverDays,
            NoticeShortfallDays = shortfallDays,
            NoticeShortfallPenaltyAmount = Math.Round(shortfallPenalty, 2),
            LastWorkingDate = lastWorkingDate,
            EffectiveSeparationDate = effectiveDate,
            Status = StaffSeparationStatuses.Submitted,
            FullNameSnapshot = fullName,
            EmployeeNumberSnapshot = employeeNumber,
            PositionTitleSnapshot = positionTitle,
            OrganisationNameSnapshot = activeJob.Organisation?.OrganisationName ?? "Organisation",
            DepartmentNameSnapshot = department,
            DateJoinedSnapshot = activeJob.JoinedDate,
            BasicSalarySnapshot = activeJob.BasicSalary,
            SupervisorIndividualId = supervisorIndividualId,
            SupervisorNameSnapshot = supervisorIndividualId.HasValue ? "Supervisor" : null,
            PersonalContactNumber = input.PersonalContactNumber,
            PersonalEmail = input.PersonalEmail,
            ForwardingAddress = input.ForwardingAddress,
            CreatedByUserId = actor.UserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.StaffSeparations.Add(separation);
        await db.SaveChangesAsync();

        // Initialize Work Handover header
        var handover = new SeparationWorkHandover
        {
            StaffSeparationId = separation.StaffSeparationId,
            SupervisorIndividualId = supervisorIndividualId ?? actor.IndividualId,
            SupervisorStatus = "Pending",
            HodStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.SeparationWorkHandovers.Add(handover);

        // Populate Asset Clearances from active AssetCustodies
        var activeCustodies = await db.AssetCustodies
            .Include(c => c.AssetItem)
            .Where(c => c.IndividualId == activeJob.IndividualID && c.Status == "Active")
            .ToListAsync();

        foreach (var custody in activeCustodies)
        {
            db.SeparationAssetClearances.Add(new SeparationAssetClearance
            {
                StaffSeparationId = separation.StaffSeparationId,
                AssetCustodyId = custody.AssetCustodyId,
                AssetItemId = custody.AssetItemId,
                DisposalAction = StaffSeparationAssetDisposalActions.PendingReturn,
                Status = "PendingReturn"
            });
        }

        // Populate initial IT and Access Revocations
        db.SeparationAccessRevocations.Add(new SeparationAccessRevocation
        {
            StaffSeparationId = separation.StaffSeparationId,
            SystemType = StaffSeparationAccessSystemTypes.HrmPortal,
            SystemName = "HRM Employee Self-Service Access",
            AccountIdentifier = actor.Username ?? $"USR-{activeJob.IndividualID}",
            RevocationAction = StaffSeparationAccessRevocationActions.RevokeAccess,
            ScheduledRevocationDate = effectiveDate,
            Status = StaffSeparationAccessRevocationStatuses.Pending
        });

        db.SeparationAccessRevocations.Add(new SeparationAccessRevocation
        {
            StaffSeparationId = separation.StaffSeparationId,
            SystemType = StaffSeparationAccessSystemTypes.DoorAccessCard,
            SystemName = "Headquarters Door Access Card & Physical Keys",
            AccountIdentifier = $"CARD-{activeJob.IndividualID}",
            DoorBuildingName = "Main Office Tower",
            RevocationAction = StaffSeparationAccessRevocationActions.PhysicalCardCollected,
            ScheduledRevocationDate = lastWorkingDate,
            Status = StaffSeparationAccessRevocationStatuses.Pending
        });

        // Initialize Finance reconciliation with notice shortfall penalty if any
        if (shortfallPenalty > 0)
        {
            db.SeparationFinanceItems.Add(new SeparationFinanceItem
            {
                StaffSeparationId = separation.StaffSeparationId,
                Category = StaffSeparationFinanceCategories.NoticePenaltyDeduction,
                Description = $"Contractual notice shortfall penalty ({shortfallDays} days)",
                IsCredit = false,
                Amount = Math.Round(shortfallPenalty, 2),
                IsCleared = false
            });
        }

        // Add standard departmental clearance tasks
        var template = await db.ClearanceTemplates
            .Include(t => t.Tasks)
            .Where(t => t.OrganisationId == org && t.IsActive && (t.SeparationTypeId == null || t.SeparationTypeId == sepType.SeparationTypeId))
            .OrderByDescending(t => t.SeparationTypeId.HasValue)
            .FirstOrDefaultAsync();

        if (template != null && template.Tasks.Any())
        {
            foreach (var taskDef in template.Tasks.OrderBy(t => t.SequenceOrder))
            {
                db.ClearanceTasks.Add(new ClearanceTask
                {
                    StaffSeparationId = separation.StaffSeparationId,
                    Domain = taskDef.Domain,
                    TaskName = taskDef.TaskName,
                    TaskDescription = taskDef.TaskDescription,
                    AssignedOrganisationStructureId = taskDef.AssignedOrganisationStructureId,
                    AssignedToRoleId = taskDef.AssignedRoleId,
                    IsMandatory = taskDef.IsMandatory,
                    SequenceOrder = taskDef.SequenceOrder,
                    Status = StaffSeparationClearanceTaskStatuses.Pending
                });
            }
        }
        else
        {
            var defaultTasks = new[]
            {
                (StaffSeparationClearanceDomains.SupervisorHandover, "Operational Handover & Workflow Sign-off", "Completion of ongoing duties, project assignments, and pending workflow delegations", true, 1),
                (StaffSeparationClearanceDomains.AssetReturn, "Hardware & Physical Asset Custody Return", "Return of issued laptops, mobile phones, security keys, and accessories in good order", true, 2),
                (StaffSeparationClearanceDomains.FinanceSettlement, "Finance, Loans & Final Settlement Audit", "Reconciliation of salary arrears, unearned allowances, loans, and leave encashment", true, 3),
                (StaffSeparationClearanceDomains.ItRevocation, "IT Domain, Email & Application Access Revocation", "Deprovisioning of corporate credentials, email mailbox reassignment, and VPN revocation", true, 4),
                (StaffSeparationClearanceDomains.SecurityDoorAccess, "Physical Security & Building Access Card Return", "Collection and deactivation of RFID badges, physical office keys, and parking tags", true, 5),
                (StaffSeparationClearanceDomains.DepartmentalClearance, "Departmental Records & Knowledge Transfer", "Archiving of official records, documentation handover, and departure sign-off", false, 6)
            };

            foreach (var t in defaultTasks)
            {
                db.ClearanceTasks.Add(new ClearanceTask
                {
                    StaffSeparationId = separation.StaffSeparationId,
                    Domain = t.Item1,
                    TaskName = t.Item2,
                    TaskDescription = t.Item3,
                    IsMandatory = t.Item4,
                    SequenceOrder = t.Item5,
                    Status = StaffSeparationClearanceTaskStatuses.Pending
                });
            }
        }

        // Record initial immutable audit event
        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = separation.StaffSeparationId,
            EventType = StaffSeparationEventTypes.Initiated,
            EventTitle = $"Separation initiated ({sepType.Name})",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "Admin",
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(new { separation.ReasonCategory, separation.RequestedNoticeDate, separation.LastWorkingDate, separation.EffectiveSeparationDate })
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Separation request initiated successfully.");
    }

    public async Task<ServiceResult> AcknowledgeExitAsync(StaffSeparationEmployeeAcknowledgeInput input)
    {
        var (actor, _, _) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var separation = await db.StaffSeparations.FindAsync(input.StaffSeparationId);
        if (separation == null)
            return ServiceResult.Fail("Separation record not found.");

        if (separation.IndividualId != actor.IndividualId && !actor.IsSuperAdministrator)
            return ServiceResult.Fail("Only the separating employee can acknowledge their exit instructions.");

        separation.EmployeeAcknowledgementStatus = input.AcknowledgementStatus;
        separation.EmployeeAcknowledgedAtUtc = DateTime.UtcNow;
        separation.EmployeeExitComments = input.ExitComments;
        if (!string.IsNullOrWhiteSpace(input.PersonalContactNumber)) separation.PersonalContactNumber = input.PersonalContactNumber;
        if (!string.IsNullOrWhiteSpace(input.PersonalEmail)) separation.PersonalEmail = input.PersonalEmail;
        if (!string.IsNullOrWhiteSpace(input.ForwardingAddress)) separation.ForwardingAddress = input.ForwardingAddress;
        separation.UpdatedAtUtc = DateTime.UtcNow;

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = separation.StaffSeparationId,
            EventType = "EmployeeAcknowledged",
            EventTitle = $"Employee {input.AcknowledgementStatus.ToLower()} exit instructions",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "Employee",
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(new { input.AcknowledgementStatus, input.ExitComments })
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Exit instructions acknowledged successfully.");
    }

    public async Task<ServiceResult> SaveHandoverItemAsync(StaffSeparationHandoverItemInput input)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var separation = await db.StaffSeparations.Include(s => s.WorkHandover).FirstOrDefaultAsync(s => s.StaffSeparationId == input.StaffSeparationId);
        if (separation == null)
            return ServiceResult.Fail("Separation record not found.");

        if (!isHrOrAdmin && separation.IndividualId != actor.IndividualId && separation.SupervisorIndividualId != actor.IndividualId)
            return ServiceResult.Fail("Unauthorized to modify work handover items.");

        if (separation.WorkHandover == null)
        {
            separation.WorkHandover = new SeparationWorkHandover
            {
                StaffSeparationId = separation.StaffSeparationId,
                SupervisorIndividualId = separation.SupervisorIndividualId ?? actor.IndividualId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.SeparationWorkHandovers.Add(separation.WorkHandover);
            await db.SaveChangesAsync();
        }

        SeparationWorkHandoverItem item;
        if (input.WorkHandoverItemId.HasValue && input.WorkHandoverItemId.Value > 0)
        {
            item = await db.SeparationWorkHandoverItems.FindAsync(input.WorkHandoverItemId.Value)
                ?? throw new KeyNotFoundException("Work handover item not found.");
            if (!string.IsNullOrWhiteSpace(input.ItemType)) item.ItemType = input.ItemType;
            if (!string.IsNullOrWhiteSpace(input.ItemTitle)) item.ItemTitle = input.ItemTitle;
            if (input.Details != null) item.Details = input.Details;
            if (input.RecipientIndividualId > 0) item.RecipientIndividualId = input.RecipientIndividualId;
            if (input.TargetCompletionDate != default) item.TargetCompletionDate = input.TargetCompletionDate;
            if (!string.IsNullOrWhiteSpace(input.Status)) item.Status = input.Status;
            if (input.HandoverNotes != null) item.HandoverNotes = input.HandoverNotes;
            if (input.Status == StaffSeparationWorkHandoverItemStatuses.Verified)
            {
                item.VerifiedAtUtc = DateTime.UtcNow;
                item.VerifiedByIndividualId = actor.IndividualId > 0 ? actor.IndividualId : null;
            }
        }
        else
        {
            if (input.RecipientIndividualId <= 0)
                throw new InvalidOperationException("A valid recipient colleague is required.");

            item = new SeparationWorkHandoverItem
            {
                WorkHandoverId = separation.WorkHandover.WorkHandoverId,
                StaffSeparationId = separation.StaffSeparationId,
                ItemType = string.IsNullOrWhiteSpace(input.ItemType) ? StaffSeparationWorkHandoverItemTypes.ProjectAssignment : input.ItemType,
                ItemTitle = input.ItemTitle,
                Details = input.Details,
                RecipientIndividualId = input.RecipientIndividualId,
                TargetCompletionDate = input.TargetCompletionDate == default ? TodayMaldives : input.TargetCompletionDate,
                Status = string.IsNullOrWhiteSpace(input.Status) ? StaffSeparationWorkHandoverItemStatuses.Pending : input.Status,
                HandoverNotes = input.HandoverNotes
            };
            db.SeparationWorkHandoverItems.Add(item);
        }

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = separation.StaffSeparationId,
            EventType = StaffSeparationEventTypes.HandoverUpdated,
            EventTitle = $"Handover item '{input.ItemTitle}' updated",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "User",
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Handover item saved successfully.");
    }

    public async Task<ServiceResult> DeleteHandoverItemAsync(int workHandoverItemId)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var item = await db.SeparationWorkHandoverItems.FindAsync(workHandoverItemId);
        if (item == null) return ServiceResult.Fail("Handover item not found.");

        var sep = await db.StaffSeparations.FindAsync(item.StaffSeparationId);
        if (!isHrOrAdmin && sep?.IndividualId != actor.IndividualId && sep?.SupervisorIndividualId != actor.IndividualId)
            return ServiceResult.Fail("Unauthorized to delete handover item.");

        db.SeparationWorkHandoverItems.Remove(item);
        await db.SaveChangesAsync();
        return ServiceResult.Ok("Handover item deleted.");
    }

    public async Task<ServiceResult> ReviewHandoverAsync(StaffSeparationHandoverReviewInput input)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var sep = await db.StaffSeparations
            .Include(s => s.WorkHandover).ThenInclude(h => h!.Items)
            .FirstOrDefaultAsync(s => s.StaffSeparationId == input.StaffSeparationId);

        if (sep == null || sep.WorkHandover == null)
            return ServiceResult.Fail("Work handover record not found.");

        if (!isHrOrAdmin && sep.SupervisorIndividualId != actor.IndividualId)
            return ServiceResult.Fail("Unauthorized to review work handover.");

        if (input.IsSupervisor)
        {
            sep.WorkHandover.SupervisorStatus = input.Decision;
            sep.WorkHandover.SupervisorReviewedAtUtc = DateTime.UtcNow;
            sep.WorkHandover.SupervisorComments = input.Comments;
        }
        else
        {
            sep.WorkHandover.HodStatus = input.Decision;
            sep.WorkHandover.HodReviewedAtUtc = DateTime.UtcNow;
            sep.WorkHandover.HodComments = input.Comments;
        }

        var allItemsDone = !sep.WorkHandover.Items.Any() || sep.WorkHandover.Items.All(i =>
            i.Status == StaffSeparationWorkHandoverItemStatuses.Verified ||
            i.Status == StaffSeparationWorkHandoverItemStatuses.Waived);

        sep.WorkHandover.AllItemsCompleted = allItemsDone;
        sep.WorkHandover.UpdatedAtUtc = DateTime.UtcNow;

        if (input.Decision == "Approved" && allItemsDone)
        {
            if (sep.Status == StaffSeparationStatuses.Submitted || sep.Status == StaffSeparationStatuses.HandoverInProgress)
            {
                sep.Status = StaffSeparationStatuses.ClearanceInProgress;
            }

            var handoverTask = await db.ClearanceTasks.FirstOrDefaultAsync(t =>
                t.StaffSeparationId == sep.StaffSeparationId &&
                t.Domain == StaffSeparationClearanceDomains.SupervisorHandover);
            if (handoverTask != null)
            {
                handoverTask.Status = StaffSeparationClearanceTaskStatuses.Cleared;
                handoverTask.ClearedByUserId = actor.UserId;
                handoverTask.ClearedAtUtc = DateTime.UtcNow;
                handoverTask.Remarks = "Supervisor signed off work handover.";
            }
        }

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = sep.StaffSeparationId,
            EventType = StaffSeparationEventTypes.HandoverUpdated,
            EventTitle = $"Handover {(input.IsSupervisor ? "Supervisor" : "HOD")} decision: {input.Decision}",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "Supervisor",
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(new { input.Decision, input.Comments })
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Handover review saved.");
    }

    public async Task<ServiceResult> InspectAssetClearanceAsync(StaffSeparationAssetInspectionInput input)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var assetClearance = await db.SeparationAssetClearances
            .Include(a => a.AssetItem)
            .FirstOrDefaultAsync(a => a.SeparationAssetClearanceId == input.SeparationAssetClearanceId);

        if (assetClearance == null)
            return ServiceResult.Fail("Asset clearance item not found.");

        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetReturnInspect))
            return ServiceResult.Fail("Unauthorized to inspect asset returns.");

        assetClearance.DisposalAction = input.DisposalAction;
        assetClearance.AssessedDamageCost = input.AssessedDamageCost;
        assetClearance.DeductionFromFinalSettlement = input.DeductionFromFinalSettlement;
        assetClearance.Status = input.Status;
        assetClearance.Remarks = input.Remarks;
        assetClearance.VerifiedByUserId = actor.UserId;
        assetClearance.VerifiedAtUtc = DateTime.UtcNow;

        if (input.DeductionFromFinalSettlement && input.AssessedDamageCost is > 0)
        {
            var existingFinanceDebit = await db.SeparationFinanceItems.FirstOrDefaultAsync(f =>
                f.StaffSeparationId == assetClearance.StaffSeparationId &&
                f.Category == StaffSeparationFinanceCategories.AssetDamageDeduction &&
                f.Description.Contains(assetClearance.AssetItem.AssetCode));

            if (existingFinanceDebit != null)
            {
                existingFinanceDebit.Amount = input.AssessedDamageCost.Value;
                existingFinanceDebit.Remarks = $"Assessed damage for {assetClearance.AssetItem.Name}";
            }
            else
            {
                db.SeparationFinanceItems.Add(new SeparationFinanceItem
                {
                    StaffSeparationId = assetClearance.StaffSeparationId,
                    Category = StaffSeparationFinanceCategories.AssetDamageDeduction,
                    Description = $"Asset repair/replacement deduction ({assetClearance.AssetItem.AssetCode} - {assetClearance.AssetItem.Name})",
                    IsCredit = false,
                    Amount = input.AssessedDamageCost.Value,
                    IsCleared = false,
                    VerifiedByUserId = actor.UserId,
                    VerifiedAtUtc = DateTime.UtcNow
                });
            }
        }

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = assetClearance.StaffSeparationId,
            EventType = StaffSeparationEventTypes.AssetCleared,
            EventTitle = $"Asset '{assetClearance.AssetItem.Name}' inspected: {input.DisposalAction}",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "Asset Manager",
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(new { input.DisposalAction, input.AssessedDamageCost, input.Status })
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Asset inspection saved.");
    }

    public async Task<ServiceResult> SaveFinanceItemAsync(StaffSeparationFinanceItemInput input)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        if (!isHrOrAdmin && !await access.HasPermissionAsync("StaffFinance.Manage"))
            return ServiceResult.Fail("Unauthorized to modify separation finance settlements.");

        if (input.SeparationFinanceItemId.HasValue && input.SeparationFinanceItemId.Value > 0)
        {
            var item = await db.SeparationFinanceItems.FindAsync(input.SeparationFinanceItemId.Value);
            if (item == null) return ServiceResult.Fail("Finance item not found.");

            item.Category = input.Category;
            item.Description = input.Description;
            item.IsCredit = input.IsCredit;
            item.Amount = Math.Abs(input.Amount);
            item.IsCleared = input.IsCleared;
            item.PayrollBatchReference = input.PayrollBatchReference;
            item.Remarks = input.Remarks;
            item.VerifiedByUserId = actor.UserId;
            item.VerifiedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var item = new SeparationFinanceItem
            {
                StaffSeparationId = input.StaffSeparationId,
                Category = input.Category,
                Description = input.Description,
                IsCredit = input.IsCredit,
                Amount = Math.Abs(input.Amount),
                IsCleared = input.IsCleared,
                PayrollBatchReference = input.PayrollBatchReference,
                Remarks = input.Remarks,
                VerifiedByUserId = actor.UserId,
                VerifiedAtUtc = DateTime.UtcNow
            };
            db.SeparationFinanceItems.Add(item);
        }

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = input.StaffSeparationId,
            EventType = StaffSeparationEventTypes.FinanceUpdated,
            EventTitle = $"Finance line '{input.Description}' updated ({input.Amount:N2})",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "Finance Officer",
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Finance item saved.");
    }

    public async Task<ServiceResult> DeleteFinanceItemAsync(int separationFinanceItemId)
    {
        var (_, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        if (!isHrOrAdmin && !await access.HasPermissionAsync("StaffFinance.Manage"))
            return ServiceResult.Fail("Unauthorized to delete finance items.");

        var item = await db.SeparationFinanceItems.FindAsync(separationFinanceItemId);
        if (item == null) return ServiceResult.Fail("Finance item not found.");

        db.SeparationFinanceItems.Remove(item);
        await db.SaveChangesAsync();
        return ServiceResult.Ok("Finance item deleted.");
    }

    public async Task<ServiceResult> ProcessClearanceTaskAsync(StaffSeparationClearanceTaskActionInput input)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var task = await db.ClearanceTasks.FindAsync(input.ClearanceTaskId);
        if (task == null) return ServiceResult.Fail("Clearance task not found.");

        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.StaffSeparationClearanceReview))
            return ServiceResult.Fail("Unauthorized to clear tasks.");

        task.Status = input.Status;
        task.Remarks = input.Remarks;
        task.WaiverReason = input.WaiverReason;
        task.OutstandingObligation = input.OutstandingObligation;
        task.OutstandingObligationAmount = input.OutstandingObligationAmount;
        task.OutstandingObligationDetails = input.OutstandingObligationDetails;
        task.ClearedByUserId = actor.UserId;
        task.ClearedAtUtc = DateTime.UtcNow;

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = task.StaffSeparationId,
            EventType = input.Status == StaffSeparationClearanceTaskStatuses.Waived ? StaffSeparationEventTypes.TaskWaived : StaffSeparationEventTypes.TaskCleared,
            EventTitle = $"Clearance task '{task.TaskName}' marked {input.Status}",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "Reviewer",
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(new { input.Status, input.Remarks, input.WaiverReason, input.OutstandingObligation })
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Clearance task marked {input.Status}.");
    }

    public async Task<ServiceResult> ProcessAccessRevocationAsync(StaffSeparationAccessRevocationActionInput input)
    {
        var (actor, _, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        if (!isHrOrAdmin && !await access.HasPermissionAsync("Admin"))
            return ServiceResult.Fail("Unauthorized to process access revocations.");

        var revocation = await db.SeparationAccessRevocations.FindAsync(input.SeparationAccessRevocationId);
        if (revocation == null) return ServiceResult.Fail("Access revocation item not found.");

        revocation.Status = input.Status;
        revocation.ExecutionNotes = input.ExecutionNotes;
        revocation.RevokedByUserId = actor.UserId;
        revocation.RevokedAtUtc = DateTime.UtcNow;

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = revocation.StaffSeparationId,
            EventType = StaffSeparationEventTypes.AccessRevoked,
            EventTitle = $"Revocation item '{revocation.SystemName}' marked {input.Status}",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "Security Officer",
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Access revocation updated.");
    }

    public async Task<ServiceResult> FinalReviewAndExecuteAsync(StaffSeparationFinalReviewInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.StaffSeparationExecute))
            return ServiceResult.Fail("Unauthorized to execute employment separation.");

        var sep = await db.StaffSeparations
            .Include(s => s.ClearanceTasks)
            .Include(s => s.FinanceItems)
            .Include(s => s.AssetClearances)
            .Include(s => s.AccessRevocations)
            .FirstOrDefaultAsync(s => s.StaffSeparationId == input.StaffSeparationId);

        if (sep == null) return ServiceResult.Fail("Separation record not found.");

        var unclearedMandatory = sep.ClearanceTasks.Where(t =>
            t.IsMandatory &&
            t.Status != StaffSeparationClearanceTaskStatuses.Cleared &&
            t.Status != StaffSeparationClearanceTaskStatuses.Waived).ToList();

        if (unclearedMandatory.Any())
        {
            var taskList = string.Join(", ", unclearedMandatory.Select(t => t.TaskName));
            return ServiceResult.Fail($"Cannot finalize separation. Mandatory tasks remain pending: {taskList}");
        }

        sep.HrAdminReviewerUserId = actor.UserId;
        sep.HrAdminReviewedAtUtc = DateTime.UtcNow;
        sep.HrReviewRemarks = input.HrReviewRemarks;
        sep.ExitInterviewCompleted = input.ExitInterviewCompleted;
        sep.ExitInterviewDate = input.ExitInterviewDate;
        sep.ExitInterviewNotes = input.ExitInterviewNotes;
        sep.Status = StaffSeparationStatuses.Approved;

        if (input.ExecuteTerminationNow)
        {
            var job = await db.Jobs.Include(j => j.JobPositions).FirstOrDefaultAsync(j => j.JobId == sep.JobId);
            if (job != null)
            {
                job.TerminatedDate = sep.EffectiveSeparationDate;
                job.JobStateId = 5; // Ended / Terminated

                foreach (var pos in job.JobPositions.Where(p => p.JobPositionStateId == 4 && (!p.ToDate.HasValue || p.ToDate > sep.EffectiveSeparationDate)))
                {
                    pos.ToDate = sep.EffectiveSeparationDate;
                }
            }

            // Revoke organization roles
            var userOrgs = await db.UserOrganisations.Where(uo => uo.BusinessEntityID == org).Select(uo => uo.UserOrganisationID).ToListAsync();
            var userRoles = await db.UserOrganisationRoles
                .Where(ur => userOrgs.Contains(ur.UserOrganisationID))
                .ToListAsync();

            foreach (var r in userRoles)
            {
                r.IsActive = false;
            }

            var hasUnresolved = sep.ClearanceTasks.Any(t => t.OutstandingObligation) ||
                                sep.AssetClearances.Any(a => a.DisposalAction == StaffSeparationAssetDisposalActions.ReportedLost || a.Status == "Disputed") ||
                                sep.FinanceItems.Any(f => !f.IsCredit && !f.IsCleared);

            sep.HasUnresolvedObligations = hasUnresolved;
            if (hasUnresolved)
            {
                var summaryLines = new List<string>();
                foreach (var ot in sep.ClearanceTasks.Where(t => t.OutstandingObligation))
                    summaryLines.Add($"{ot.TaskName}: {ot.OutstandingObligationDetails} ({ot.OutstandingObligationAmount:N2})");
                foreach (var of in sep.FinanceItems.Where(f => !f.IsCredit && !f.IsCleared))
                    summaryLines.Add($"Pending deduction: {of.Description} ({of.Amount:N2})");

                sep.UnresolvedObligationsSummary = string.Join("; ", summaryLines);
            }

            sep.Status = StaffSeparationStatuses.Terminated;
            sep.TerminationExecutedAtUtc = DateTime.UtcNow;
            sep.ExecutedByUserId = actor.UserId;

            await GenerateDocumentInternalAsync(db, sep, StaffSeparationDocumentVariants.ExitClearanceCertificate, actor.UserId);
            await GenerateDocumentInternalAsync(db, sep, StaffSeparationDocumentVariants.ServiceCertificate, actor.UserId);
        }

        db.SeparationEvents.Add(new SeparationEvent
        {
            StaffSeparationId = sep.StaffSeparationId,
            EventType = StaffSeparationEventTypes.Terminated,
            EventTitle = input.ExecuteTerminationNow ? "Employment termination executed" : "HR approved clearance",
            ActorUserId = actor.UserId,
            ActorName = actor.Username ?? "HR Officer",
            OccurredAtUtc = DateTime.UtcNow,
            DetailsJson = JsonSerializer.Serialize(new { sep.Status, sep.EffectiveSeparationDate, sep.HasUnresolvedObligations })
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok(input.ExecuteTerminationNow ? "Separation executed and employment terminated." : "Separation approved.");
    }

    public async Task<SeparationDocumentDto> GenerateDocumentAsync(int staffSeparationId, string documentVariant)
    {
        var (actor, _, _) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var sep = await db.StaffSeparations
            .Include(s => s.SeparationType)
            .Include(s => s.ClearanceTasks)
            .Include(s => s.FinanceItems)
            .FirstOrDefaultAsync(s => s.StaffSeparationId == staffSeparationId);

        if (sep == null) throw new KeyNotFoundException("Separation record not found.");

        return await GenerateDocumentInternalAsync(db, sep, documentVariant, actor.UserId);
    }

    private static async Task<SeparationDocumentDto> GenerateDocumentInternalAsync(HrmTeContext db, StaffSeparation sep, string variant, int actorUserId)
    {
        var docNumber = $"DOC-SEP-{sep.StaffSeparationId}-{DateTime.UtcNow:yyyyMMdd}-{variant[..3].ToUpper()}";
        var title = variant switch
        {
            StaffSeparationDocumentVariants.ExitClearanceCertificate => "Official Exit Clearance Certificate",
            StaffSeparationDocumentVariants.ServiceCertificate => "Certificate of Service & Employment History",
            StaffSeparationDocumentVariants.FinalSettlementStatement => "Final Settlement & Payout Statement",
            _ => "Official Separation Document"
        };

        var html = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 30px; font-size: 13px; color: #1e293b; }}
                    .header {{ text-align: center; border-bottom: 2px solid #3b82f6; padding-bottom: 15px; margin-bottom: 25px; }}
                    .title {{ font-size: 20px; font-weight: bold; color: #0f172a; margin-top: 5px; }}
                    .subtitle {{ font-size: 12px; color: #64748b; }}
                    .section {{ margin-bottom: 20px; }}
                    .section-title {{ font-weight: bold; border-bottom: 1px solid #cbd5e1; padding-bottom: 4px; margin-bottom: 10px; font-size: 14px; }}
                    table {{ width: 100%; border-collapse: collapse; margin-top: 8px; }}
                    td, th {{ padding: 6px 8px; border: 1px solid #e2e8f0; text-align: left; }}
                    th {{ background: #f8fafc; font-weight: 600; }}
                    .footer {{ margin-top: 50px; display: flex; justify-content: space-between; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h2>{sep.OrganisationNameSnapshot}</h2>
                    <div class='title'>{title}</div>
                    <div class='subtitle'>Document Reference: {docNumber} | Generated on: {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC</div>
                </div>

                <div class='section'>
                    <div class='section-title'>1. Employee Details</div>
                    <table>
                        <tr><th>Full Name</th><td>{sep.FullNameSnapshot}</td><th>Employee ID</th><td>{sep.EmployeeNumberSnapshot}</td></tr>
                        <tr><th>Department</th><td>{sep.DepartmentNameSnapshot}</td><th>Designation</th><td>{sep.PositionTitleSnapshot}</td></tr>
                        <tr><th>Joined Date</th><td>{sep.DateJoinedSnapshot:dd-MMM-yyyy}</td><th>Separation Date</th><td>{sep.EffectiveSeparationDate:dd-MMM-yyyy}</td></tr>
                        <tr><th>Separation Reason</th><td>{sep.ReasonCategory}</td><th>Status</th><td>{sep.Status}</td></tr>
                    </table>
                </div>

                <div class='section'>
                    <div class='section-title'>2. Clearance Summary</div>
                    <table>
                        <tr><th>Domain</th><th>Task Requirement</th><th>Status</th><th>Notes</th></tr>
                        {string.Join("", sep.ClearanceTasks.Select(t => $"<tr><td>{t.Domain}</td><td>{t.TaskName}</td><td>{t.Status}</td><td>{t.Remarks ?? t.WaiverReason ?? "-"}</td></tr>"))}
                    </table>
                </div>

                <div class='section'>
                    <div class='section-title'>3. Financial Settlement Statement</div>
                    <table>
                        <tr><th>Line Item</th><th>Type</th><th>Amount (MVR)</th><th>Cleared</th></tr>
                        {string.Join("", sep.FinanceItems.Select(f => $"<tr><td>{f.Description}</td><td>{(f.IsCredit ? "Payable to Staff" : "Deduction")}</td><td>{f.Amount:N2}</td><td>{(f.IsCleared ? "Cleared" : "Pending")}</td></tr>"))}
                    </table>
                </div>

                <div class='footer'>
                    <div>
                        <p>___________________________</p>
                        <p>HR Administrator Sign & Date</p>
                    </div>
                    <div>
                        <p>___________________________</p>
                        <p>Employee Acknowledged Signature</p>
                    </div>
                </div>
            </body>
            </html>";

        var pdfBytes = Encoding.UTF8.GetBytes(html);
        var sha256 = Convert.ToHexString(SHA256.HashData(pdfBytes));

        var doc = new SeparationDocument
        {
            StaffSeparationId = sep.StaffSeparationId,
            DocumentNumber = docNumber,
            DocumentVariant = variant,
            Title = title,
            PdfBytes = pdfBytes,
            ContentType = "text/html",
            Sha256Hash = sha256,
            GeneratedByUserId = actorUserId,
            GeneratedAtUtc = DateTime.UtcNow
        };

        db.SeparationDocuments.Add(doc);
        await db.SaveChangesAsync();

        return new SeparationDocumentDto
        {
            SeparationDocumentId = doc.SeparationDocumentId,
            StaffSeparationId = doc.StaffSeparationId,
            DocumentNumber = doc.DocumentNumber,
            DocumentVariant = doc.DocumentVariant,
            Title = doc.Title,
            ContentType = doc.ContentType,
            Sha256Hash = doc.Sha256Hash,
            Base64Data = Convert.ToBase64String(pdfBytes),
            GeneratedAtUtc = doc.GeneratedAtUtc,
            GeneratedByName = $"User #{actorUserId}"
        };
    }
}
