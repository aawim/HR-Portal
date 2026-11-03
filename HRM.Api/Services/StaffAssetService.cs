using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Authorization;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Assets;
using HRM.DTOs.UserContext;
using HRM.Enums;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class StaffAssetService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService logs,
    IRequestService? requests = null)
    : global::HRM.Services.Contracts.StaffAssetServiceContracts,
      global::HRM.Services.Contracts.StaffAssetServiceContracts.IOperations
{
    private static DateTime TodayMaldives => DateTime.UtcNow.AddHours(5).Date;
    private static DateTime NowMaldives => DateTime.UtcNow.AddHours(5);

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
                          await access.HasPermissionAsync(Permissions.AssetInventoryManage) ||
                          await access.HasPermissionAsync(Permissions.AssetHrApprove);

        return (actor, org, isHrOrAdmin);
    }

    private static string CalculateSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void RecordAssetEvent(
        HrmTeContext db,
        int orgId,
        int? assetItemId,
        string entityType,
        int entityId,
        string eventType,
        string? fromStatus,
        string? toStatus,
        int actorUserId,
        object? metadata = null)
    {
        var json = metadata is not null ? JsonSerializer.Serialize(metadata) : null;
        db.AssetEvents.Add(new AssetEvent
        {
            OrganisationId = orgId,
            AssetItemId = assetItemId,
            RelatedEntityType = entityType,
            RelatedEntityId = entityId,
            EventType = eventType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            AtUtc = DateTime.UtcNow,
            MetadataJson = json
        });
    }

    public async Task<AssetDashboardData> LoadDashboardAsync(int? organisationId = null, string? statusFilter = null)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(organisationId);
        await using var db = await factory.CreateDbContextAsync();

        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == org)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? "Organisation";

        var isSuper = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var canManageInventory = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetInventoryManage);
        var canInitiateIssuance = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetIssuanceInitiate);
        var canApproveHr = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetHrApprove);
        var canInspectReturns = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetReturnInspect);
        var canViewAudit = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetAuditView);
        var canExportRestrictedPdf = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetExportRestrictedPdf);

        // Organisations for switcher
        var organisations = isSuper
            ? await db.Organisations.AsNoTracking()
                .OrderBy(o => o.OrganisationName)
                .Select(o => new HRM.DTOs.Organisation.OrganisationSettingsOption(o.BusinessEntityID, o.OrganisationName))
                .ToListAsync()
            : [new HRM.DTOs.Organisation.OrganisationSettingsOption(org, orgName)];

        // Departments for this org
        var departments = await db.OrganisationStructures.AsNoTracking()
            .Where(s => s.OrganisationBusinessEntityID == org)
            .OrderBy(s => s.Name)
            .Select(s => new AssetDepartmentOptionDto(s.OrganisationStructureId, s.OrganisationBusinessEntityID, s.Name))
            .ToListAsync();

        // Categories
        var categories = await db.AssetCategories.AsNoTracking()
            .Where(c => (c.OrganisationId == null || c.OrganisationId == org) && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new AssetCategoryDto
            {
                AssetCategoryId = c.AssetCategoryId,
                OrganisationId = c.OrganisationId,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                RequiresSerialNumber = c.RequiresSerialNumber,
                RequiresReturn = c.RequiresReturn,
                IsSecurityItem = c.IsSecurityItem,
                IsActive = c.IsActive,
                AssetCount = c.Assets.Count(a => a.OwnerOrganisationId == org && a.IsActive),
                AvailableCount = c.Assets.Count(a => a.OwnerOrganisationId == org && a.IsActive && a.Status == AssetInventoryStatuses.Available)
            })
            .ToListAsync();

        // Conditions
        var conditions = await db.AssetConditions.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new AssetConditionDto
            {
                AssetConditionId = c.AssetConditionId,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                RequiresMaintenanceOnReturn = c.RequiresMaintenanceOnReturn,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive
            })
            .ToListAsync();

        // Staff options for issuance selection
        var staffJobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == org && j.JobStateId == 4 && (j.TerminatedDate == null || j.TerminatedDate >= TodayMaldives))
            .Include(j => j.Individual)
            .Include(j => j.OrganisationStructure)
            .Include(j => j.JobPositions.Where(p => p.JobPositionStateId == 4 && (p.ToDate == null || p.ToDate >= TodayMaldives)))
                .ThenInclude(p => p.Position)
            .Take(500)
            .ToListAsync();

        var staffOptions = staffJobs.Select(j => new AssetStaffOptionDto
        {
            IndividualId = j.IndividualID,
            EmployeeNumber = j.Sapnumber ?? j.JobId.ToString(),
            FullName = string.Join(" ", new[] { j.Individual.FirstNameEnglish, j.Individual.MiddleNameEnglish, j.Individual.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))),
            JobId = j.JobId,
            PositionTitle = j.JobPositions.Select(p => p.Position.Name).FirstOrDefault() ?? "Staff",
            OrganisationId = j.OrganisationID,
            StructureId = j.OrganisationStructureId,
            DepartmentName = j.OrganisationStructure != null ? j.OrganisationStructure.Name : "",
            HasTerminatingOrTransferNotice = j.TerminatedDate != null
        })
        .OrderBy(s => s.FullName)
        .ToList();

        // Base queries scoped to organisation
        var baseAssetQuery = db.AssetItems.AsNoTracking()
            .Where(a => a.OwnerOrganisationId == org && a.IsActive);

        var allAssetStatusCounts = await baseAssetQuery
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalAssets = allAssetStatusCounts.Sum(x => x.Count);
        var assignedAssets = allAssetStatusCounts.FirstOrDefault(x => x.Status == AssetInventoryStatuses.Assigned)?.Count ?? 0;
        var availableAssets = allAssetStatusCounts.FirstOrDefault(x => x.Status == AssetInventoryStatuses.Available)?.Count ?? 0;
        var reservedAssets = allAssetStatusCounts.FirstOrDefault(x => x.Status is AssetInventoryStatuses.Reserved or AssetInventoryStatuses.PendingHandover)?.Count ?? 0;
        var maintenanceAssets = allAssetStatusCounts.FirstOrDefault(x => x.Status == AssetInventoryStatuses.Maintenance)?.Count ?? 0;
        var lostDisposedAssets = allAssetStatusCounts.Where(x => x.Status is AssetInventoryStatuses.Lost or AssetInventoryStatuses.Disposed).Sum(x => x.Count);

        var baseIssuanceQuery = db.AssetIssuances.AsNoTracking()
            .Where(i => i.OrganisationId == org);

        var staffPendingIssuances = await baseIssuanceQuery
            .CountAsync(i => i.Status == AssetIssuanceStatuses.PendingStaffAcknowledgement);

        var hrPendingIssuances = await baseIssuanceQuery
            .CountAsync(i => i.Status == AssetIssuanceStatuses.PendingHRApproval);

        var overdueAckThreshold = DateTime.UtcNow.AddDays(-3);
        var overdueAckCount = await db.AssetIssuanceItems.AsNoTracking()
            .CountAsync(li => li.Issuance.OrganisationId == org &&
                              li.Status == AssetIssuanceLineStatuses.HandedOver &&
                              li.HandedOverAtUtc != null &&
                              li.HandedOverAtUtc <= overdueAckThreshold);

        var activeCustodiesQuery = db.AssetCustodies.AsNoTracking()
            .Where(c => c.OrganisationId == org && c.CustodyToUtc == null && c.Status == AssetCustodyStatuses.Active);

        var overdueReturnsCount = await activeCustodiesQuery
            .CountAsync(c => c.ExpectedReturnDate != null && c.ExpectedReturnDate < TodayMaldives);

        var openReturnsCount = await db.AssetReturns.AsNoTracking()
            .CountAsync(r => r.OrganisationId == org && r.Status == AssetReturnStatuses.ReturnPending);

        // Chart slices
        var categoryBreakdown = categories.Select(c => new AssetChartSliceDto(
            c.Name,
            c.AssetCount,
            c.AvailableCount,
            c.AssetCount - c.AvailableCount,
            "blue")).ToList();

        var conditionBreakdown = conditions.Select(cond => new AssetChartSliceDto(
            cond.Name,
            db.AssetItems.AsNoTracking().Count(a => a.OwnerOrganisationId == org && a.IsActive && a.CurrentConditionId == cond.AssetConditionId),
            db.AssetItems.AsNoTracking().Count(a => a.OwnerOrganisationId == org && a.IsActive && a.CurrentConditionId == cond.AssetConditionId && a.Status == AssetInventoryStatuses.Available),
            db.AssetItems.AsNoTracking().Count(a => a.OwnerOrganisationId == org && a.IsActive && a.CurrentConditionId == cond.AssetConditionId && a.Status == AssetInventoryStatuses.Assigned),
            cond.Code == "DAMAGED" ? "rose" : cond.Code == "NEW" ? "teal" : "blue")).ToList();

        // Alerts
        var alerts = new List<AssetAlertRowDto>();

        var overdueRaw = await activeCustodiesQuery
            .Where(c => c.ExpectedReturnDate != null && c.ExpectedReturnDate < TodayMaldives)
            .OrderBy(c => c.ExpectedReturnDate)
            .Take(20)
            .Select(c => new
            {
                c.AssetCustodyId,
                c.AssetItem.AssetCode,
                ItemName = c.AssetItem.Name,
                c.IndividualId,
                c.Individual.FirstNameEnglish,
                c.Individual.LastNameEnglish,
                Expected = c.ExpectedReturnDate!.Value
            })
            .ToListAsync();

        var overdueCustodies = overdueRaw.Select(c => new
        {
            c.AssetCustodyId,
            c.AssetCode,
            c.ItemName,
            c.IndividualId,
            StaffName = string.Join(" ", new[] { c.FirstNameEnglish, c.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))),
            c.Expected
        }).ToList();

        foreach (var c in overdueCustodies)
        {
            alerts.Add(new AssetAlertRowDto(
                "OverdueReturn",
                "high",
                $"CUST-{c.AssetCustodyId}",
                c.AssetCustodyId,
                c.AssetCode,
                c.ItemName,
                c.IndividualId,
                c.StaffName,
                c.Expected,
                $"Asset is overdue for return since {c.Expected:dd MMM yyyy}."));
        }

        var overdueAcks = await db.AssetIssuanceItems.AsNoTracking()
            .Where(li => li.Issuance.OrganisationId == org &&
                         li.Status == AssetIssuanceLineStatuses.HandedOver &&
                         li.HandedOverAtUtc != null &&
                         li.HandedOverAtUtc <= overdueAckThreshold)
            .OrderBy(li => li.HandedOverAtUtc)
            .Take(20)
            .Select(li => new
            {
                li.Issuance.AssetIssuanceId,
                li.Issuance.ReferenceNumber,
                li.AssetItem.AssetCode,
                ItemName = li.AssetItem.Name,
                li.Issuance.IndividualId,
                li.Issuance.EmployeeNameSnapshot,
                HandedOverAt = li.HandedOverAtUtc!.Value
            })
            .ToListAsync();

        foreach (var a in overdueAcks)
        {
            alerts.Add(new AssetAlertRowDto(
                "OverdueAcknowledgement",
                "medium",
                a.ReferenceNumber,
                a.AssetIssuanceId,
                a.AssetCode,
                a.ItemName,
                a.IndividualId,
                a.EmployeeNameSnapshot,
                a.HandedOverAt.AddHours(5),
                $"Physically handed over on {a.HandedOverAt.AddHours(5):dd MMM yyyy}; pending staff confirmation."));
        }

        // Filtered Asset List
        var assetListQuery = baseAssetQuery;
        if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
            assetListQuery = assetListQuery.Where(a => a.Status == statusFilter);

        var rawAssets = await assetListQuery
            .Include(a => a.Category)
            .Include(a => a.CurrentCondition)
            .Include(a => a.OwnerStructure)
            .Include(a => a.Custodies.Where(c => c.CustodyToUtc == null && c.Status == AssetCustodyStatuses.Active))
                .ThenInclude(c => c.Individual)
            .Include(a => a.IssuanceItems)
            .OrderBy(a => a.AssetCode)
            .Take(300)
            .ToListAsync();

        var assets = rawAssets.Select(a =>
        {
            var activeCustody = a.Custodies.FirstOrDefault(c => c.CustodyToUtc == null && c.Status == AssetCustodyStatuses.Active);
            var custodianName = activeCustody?.Individual != null
                ? string.Join(" ", new[] { activeCustody.Individual.FirstNameEnglish, activeCustody.Individual.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x)))
                : null;

            return new AssetItemDto
            {
                AssetItemId = a.AssetItemId,
                CategoryId = a.CategoryId,
                CategoryCode = a.Category?.Code ?? "",
                CategoryName = a.Category?.Name ?? "",
                RequiresSerialNumber = a.Category?.RequiresSerialNumber ?? false,
                RequiresReturn = a.Category?.RequiresReturn ?? true,
                IsSecurityItem = a.Category?.IsSecurityItem ?? false,
                AssetCode = a.AssetCode,
                SerialNumber = a.SerialNumber,
                Name = a.Name,
                Brand = a.Brand,
                Model = a.Model,
                OwnerOrganisationId = a.OwnerOrganisationId,
                OwnerOrganisationName = orgName,
                OwnerStructureId = a.OwnerStructureId,
                OwnerStructureName = a.OwnerStructure?.Name,
                Location = a.Location,
                Status = a.Status,
                CurrentConditionId = a.CurrentConditionId,
                CurrentConditionName = a.CurrentCondition?.Name ?? "",
                WarrantyExpiryDate = a.WarrantyExpiryDate,
                DefaultAccessories = a.DefaultAccessories,
                Notes = a.Notes,
                IsActive = a.IsActive,
                ActiveCustodianIndividualId = activeCustody?.IndividualId,
                ActiveCustodianName = custodianName,
                ActiveCustodyFromLocal = activeCustody != null ? activeCustody.CustodyFromUtc.AddHours(5) : null,
                HasHistory = a.Custodies.Count > 0 || a.IssuanceItems.Count > 0,
                RowVersionBase64 = a.RowVersion != null ? Convert.ToBase64String(a.RowVersion) : ""
            };
        }).ToList();

        // Issuance queue rows
        var issuances = await baseIssuanceQuery
            .OrderByDescending(i => i.UpdatedAtUtc)
            .Take(150)
            .Select(i => new AssetIssuanceQueueRowDto
            {
                AssetIssuanceId = i.AssetIssuanceId,
                RequestId = i.RequestId,
                ReferenceNumber = i.ReferenceNumber,
                IndividualId = i.IndividualId,
                EmployeeName = i.EmployeeNameSnapshot,
                StaffNumber = i.StaffNumberSnapshot,
                JobId = i.JobId,
                PositionTitle = i.PositionTitleSnapshot,
                OrganisationId = i.OrganisationId,
                OrganisationName = i.OrganisationNameSnapshot,
                StructureId = i.StructureId,
                DepartmentName = i.DepartmentNameSnapshot,
                Reason = i.Reason,
                Status = i.Status,
                IssueDate = i.IssueDate,
                UpdatedAtLocal = i.UpdatedAtUtc.AddHours(5),
                TotalLines = i.Lines.Count,
                HandedOverLines = i.Lines.Count(l => l.Status == AssetIssuanceLineStatuses.HandedOver),
                AcceptedLines = i.Lines.Count(l => l.Status == AssetIssuanceLineStatuses.Accepted),
                RejectedLines = i.Lines.Count(l => l.Status == AssetIssuanceLineStatuses.Rejected),
                HrApprovedLines = i.Lines.Count(l => l.Status == AssetIssuanceLineStatuses.HRApproved),
                IsOverdueAcknowledgement = i.Lines.Any(l => l.Status == AssetIssuanceLineStatuses.HandedOver && l.HandedOverAtUtc <= overdueAckThreshold)
            })
            .ToListAsync();

        // Custodies (active and recently closed)
        var rawCustodies = await db.AssetCustodies.AsNoTracking()
            .Where(c => c.OrganisationId == org)
            .Include(c => c.AssetItem)
                .ThenInclude(a => a.Category)
            .Include(c => c.IssuanceItem)
                .ThenInclude(li => li.Issuance)
            .Include(c => c.Individual)
            .Include(c => c.Structure)
            .OrderByDescending(c => c.CustodyFromUtc)
            .Take(200)
            .ToListAsync();

        var custodies = rawCustodies.Select(c => new AssetCustodyRowDto
        {
            AssetCustodyId = c.AssetCustodyId,
            AssetItemId = c.AssetItemId,
            AssetCode = c.AssetItem.AssetCode,
            SerialNumber = c.AssetItem.SerialNumber,
            CategoryName = c.AssetItem.Category.Name,
            ItemName = c.AssetItem.Name,
            Brand = c.AssetItem.Brand,
            Model = c.AssetItem.Model,
            ConditionAtIssueName = c.IssuanceItem.ConditionNameSnapshot,
            Accessories = c.IssuanceItem.Accessories,
            IndividualId = c.IndividualId,
            EmployeeName = string.Join(" ", new[] { c.Individual.FirstNameEnglish, c.Individual.MiddleNameEnglish, c.Individual.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))),
            StaffNumber = c.IssuanceItem.Issuance.StaffNumberSnapshot,
            JobId = c.JobId,
            PositionTitle = c.IssuanceItem.Issuance.PositionTitleSnapshot,
            OrganisationId = c.OrganisationId,
            OrganisationName = orgName,
            StructureId = c.StructureId,
            DepartmentName = c.Structure != null ? c.Structure.Name : "",
            IssuanceId = c.IssuanceItem.IssuanceId,
            IssuanceReference = c.IssuanceItem.Issuance.ReferenceNumber,
            IssuanceItemId = c.IssuanceItemId,
            CustodyFromLocal = c.CustodyFromUtc.AddHours(5),
            CustodyToLocal = c.CustodyToUtc.HasValue ? c.CustodyToUtc.Value.AddHours(5) : null,
            ExpectedReturnDate = c.ExpectedReturnDate,
            IsOverdueReturn = c.CustodyToUtc == null && c.ExpectedReturnDate != null && c.ExpectedReturnDate < TodayMaldives,
            Status = c.Status,
            ClosedReason = c.ClosedReason
        }).ToList();

        // Returns
        var rawReturns = await db.AssetReturns.AsNoTracking()
            .Where(r => r.OrganisationId == org)
            .Include(r => r.Individual)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ReturnedToUser)
            .Include(r => r.Items)
                .ThenInclude(item => item.AssetItem)
                    .ThenInclude(a => a.Category)
            .Include(r => r.Items)
                .ThenInclude(item => item.ConditionAtReturn)
            .Include(r => r.Items)
                .ThenInclude(item => item.ReceivedByUser)
            .OrderByDescending(r => r.ReturnDate)
            .Take(100)
            .ToListAsync();

        var returns = rawReturns.Select(r => new AssetReturnDetailDto
        {
            AssetReturnId = r.AssetReturnId,
            ReferenceNumber = r.ReferenceNumber,
            OrganisationId = r.OrganisationId,
            IndividualId = r.IndividualId,
            EmployeeName = string.Join(" ", new[] { r.Individual.FirstNameEnglish, r.Individual.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))),
            JobId = r.JobId,
            StaffTransferId = r.StaffTransferId,
            ReturnType = r.ReturnType,
            RequestedByName = r.RequestedByUser.Username,
            ReturnedToName = r.ReturnedToUser != null ? r.ReturnedToUser.Username : null,
            ReturnDateLocal = r.ReturnDate.AddHours(5),
            CompletedAtLocal = r.CompletedAtUtc.HasValue ? r.CompletedAtUtc.Value.AddHours(5) : null,
            Status = r.Status,
            Notes = r.Notes,
            Items = r.Items.Select(item => new AssetReturnItemDto
            {
                AssetReturnItemId = item.AssetReturnItemId,
                AssetCustodyId = item.AssetCustodyId,
                AssetItemId = item.AssetItemId,
                AssetCode = item.AssetItem.AssetCode,
                SerialNumber = item.AssetItem.SerialNumber,
                CategoryName = item.AssetItem.Category.Name,
                ItemName = item.AssetItem.Name,
                ConditionAtReturnId = item.ConditionAtReturnId,
                ConditionAtReturnName = item.ConditionAtReturn != null ? item.ConditionAtReturn.Name : null,
                AccessoryChecklist = item.AccessoryChecklist,
                MissingAccessories = item.MissingAccessories,
                DamageOrLossEvidence = item.DamageOrLossEvidence,
                InspectionResult = item.InspectionResult,
                PostReturnDisposition = item.PostReturnDisposition,
                ReceivedByName = item.ReceivedByUser != null ? item.ReceivedByUser.Username : null,
                ReceivedAtLocal = item.ReceivedAtUtc.HasValue ? item.ReceivedAtUtc.Value.AddHours(5) : null,
                Status = item.Status
            }).ToList()
        }).ToList();

        // Transfer Dispositions
        var rawDispositions = await db.AssetTransferDispositions.AsNoTracking()
            .Where(d => d.FromOrganisationId == org || d.ToOrganisationId == org)
            .Include(d => d.Individual)
            .Include(d => d.AssetItem)
            .Include(d => d.FromStructure)
            .Include(d => d.ToStructure)
            .Include(d => d.DecidedByUser)
            .OrderByDescending(d => d.DecidedAtUtc)
            .Take(100)
            .ToListAsync();

        var dispositions = rawDispositions.Select(d => new AssetTransferDispositionDto
        {
            AssetTransferDispositionId = d.AssetTransferDispositionId,
            StaffTransferId = d.StaffTransferId,
            AssetCustodyId = d.AssetCustodyId,
            AssetItemId = d.AssetItemId,
            AssetCode = d.AssetItem.AssetCode,
            SerialNumber = d.AssetItem.SerialNumber,
            ItemName = d.AssetItem.Name,
            IndividualId = d.IndividualId,
            EmployeeName = string.Join(" ", new[] { d.Individual.FirstNameEnglish, d.Individual.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))),
            FromOrganisationId = d.FromOrganisationId,
            ToOrganisationId = d.ToOrganisationId,
            FromStructureId = d.FromStructureId,
            FromDepartmentName = d.FromStructure != null ? d.FromStructure.Name : "",
            ToStructureId = d.ToStructureId,
            ToDepartmentName = d.ToStructure != null ? d.ToStructure.Name : "",
            Decision = d.Decision,
            Status = d.Status,
            DecidedByName = d.DecidedByUser.Username,
            DecidedAtLocal = d.DecidedAtUtc.AddHours(5),
            ExecutedAtLocal = d.ExecutedAtUtc.HasValue ? d.ExecutedAtUtc.Value.AddHours(5) : null,
            Remarks = d.Remarks
        }).ToList();

        // Recent Audit Events
        var recentAuditEvents = await db.AssetEvents.AsNoTracking()
            .Where(e => e.OrganisationId == org)
            .OrderByDescending(e => e.AtUtc)
            .Take(100)
            .Select(e => new AssetEventDto(
                e.AssetEventId,
                e.AssetItemId,
                e.AssetItem != null ? e.AssetItem.AssetCode : null,
                e.RelatedEntityType,
                e.RelatedEntityId,
                e.EventType,
                e.FromStatus,
                e.ToStatus,
                e.ActorUser.Username,
                e.AtUtc.AddHours(5),
                e.MetadataJson))
            .ToListAsync();

        return new AssetDashboardData
        {
            OrganisationId = org,
            OrganisationName = orgName,
            IsSuperAdmin = isSuper,
            CanManageInventory = canManageInventory,
            CanInitiateIssuance = canInitiateIssuance,
            CanApproveHr = canApproveHr,
            CanInspectReturns = canInspectReturns,
            CanViewAudit = canViewAudit,
            CanExportRestrictedPdf = canExportRestrictedPdf,

            TotalAssetsCount = totalAssets,
            AssignedAssetsCount = assignedAssets,
            AvailableAssetsCount = availableAssets,
            StaffPendingCount = staffPendingIssuances,
            HrPendingCount = hrPendingIssuances,
            OverdueReturnsCount = overdueReturnsCount + openReturnsCount,

            ReservedOrHandedOverCount = reservedAssets,
            MaintenanceCount = maintenanceAssets,
            LostOrDisposedCount = lostDisposedAssets,
            OverdueAcknowledgementsCount = overdueAckCount,
            OpenClearanceCount = openReturnsCount,

            Organisations = organisations,
            Departments = departments,
            Categories = categories,
            Conditions = conditions,
            StaffOptions = staffOptions,

            CategoryBreakdown = categoryBreakdown,
            ConditionBreakdown = conditionBreakdown,
            OverdueAndReconciliationAlerts = alerts,

            Assets = assets,
            Issuances = issuances,
            Custodies = custodies,
            Returns = returns,
            TransferDispositions = dispositions,
            RecentAuditEvents = recentAuditEvents
        };
    }

    public async Task<ServiceResult> SaveCategoryAsync(AssetCategorySaveInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(input.OrganisationId);
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetInventoryManage))
            return ServiceResult.Fail("You do not have permission to manage asset categories.");

        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            return ServiceResult.Fail("Category code and name are required.");

        await using var db = await factory.CreateDbContextAsync();
        var code = input.Code.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        var duplicate = await db.AssetCategories.AnyAsync(c =>
            c.AssetCategoryId != input.AssetCategoryId &&
            (c.OrganisationId == null || c.OrganisationId == org) &&
            (c.Code == code || c.Name == name));

        if (duplicate)
            return ServiceResult.Fail($"A category with code '{code}' or name '{name}' already exists.");

        if (input.AssetCategoryId == 0)
        {
            var category = new AssetCategory
            {
                OrganisationId = org,
                Code = code,
                Name = name,
                Description = input.Description?.Trim() ?? "",
                RequiresSerialNumber = input.RequiresSerialNumber,
                RequiresReturn = input.RequiresReturn,
                IsSecurityItem = input.IsSecurityItem,
                IsActive = input.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.AssetCategories.Add(category);
            await db.SaveChangesAsync();

            RecordAssetEvent(db, org, null, "AssetCategory", category.AssetCategoryId, "CategoryCreated", null, "Active", actor.UserId);
            await db.SaveChangesAsync();
        }
        else
        {
            var category = await db.AssetCategories.FirstOrDefaultAsync(c => c.AssetCategoryId == input.AssetCategoryId);
            if (category == null) return ServiceResult.Fail("Category not found.");

            category.Name = name;
            category.Description = input.Description?.Trim() ?? "";
            category.RequiresSerialNumber = input.RequiresSerialNumber;
            category.RequiresReturn = input.RequiresReturn;
            category.IsSecurityItem = input.IsSecurityItem;
            category.IsActive = input.IsActive;

            await db.SaveChangesAsync();
            RecordAssetEvent(db, org, null, "AssetCategory", category.AssetCategoryId, "CategoryUpdated", null, category.IsActive ? "Active" : "Inactive", actor.UserId);
            await db.SaveChangesAsync();
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SaveAssetItemAsync(AssetItemSaveInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(input.OwnerOrganisationId);
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetInventoryManage))
            return ServiceResult.Fail("You do not have permission to manage physical asset items.");

        await using var db = await factory.CreateDbContextAsync();
        var category = await db.AssetCategories.AsNoTracking().FirstOrDefaultAsync(c => c.AssetCategoryId == input.CategoryId);
        if (category == null) return ServiceResult.Fail("Selected asset category does not exist.");

        var errors = StaffAssetValidator.ValidateAssetItem(input, category.RequiresSerialNumber);
        if (errors.Count > 0) return ServiceResult.Fail(string.Join(" ", errors));

        var assetCode = input.AssetCode.Trim().ToUpperInvariant();
        var serial = string.IsNullOrWhiteSpace(input.SerialNumber) ? null : input.SerialNumber.Trim();

        var codeExists = await db.AssetItems.AnyAsync(a => a.AssetItemId != input.AssetItemId && a.AssetCode == assetCode);
        if (codeExists)
            return ServiceResult.Fail($"Asset Code '{assetCode}' is already registered to another item.");

        if (!string.IsNullOrEmpty(serial))
        {
            var serialExists = await db.AssetItems.AnyAsync(a => a.AssetItemId != input.AssetItemId && a.OwnerOrganisationId == org && a.SerialNumber == serial);
            if (serialExists)
                return ServiceResult.Fail($"Serial Number '{serial}' already exists in this organization.");
        }

        if (input.AssetItemId == 0)
        {
            var item = new AssetItem
            {
                CategoryId = input.CategoryId,
                AssetCode = assetCode,
                SerialNumber = serial,
                Name = input.Name.Trim(),
                Brand = input.Brand?.Trim(),
                Model = input.Model?.Trim(),
                OwnerOrganisationId = org,
                OwnerStructureId = input.OwnerStructureId > 0 ? input.OwnerStructureId : null,
                Location = input.Location?.Trim(),
                Status = input.Status,
                CurrentConditionId = input.CurrentConditionId,
                WarrantyExpiryDate = input.WarrantyExpiryDate,
                DefaultAccessories = input.DefaultAccessories?.Trim(),
                Notes = input.Notes?.Trim(),
                IsActive = input.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.AssetItems.Add(item);
            await db.SaveChangesAsync();

            RecordAssetEvent(db, org, item.AssetItemId, "AssetItem", item.AssetItemId, "AssetRegistered", null, item.Status, actor.UserId, new { item.AssetCode, item.SerialNumber, item.Name });
            await db.SaveChangesAsync();
        }
        else
        {
            var item = await db.AssetItems.FirstOrDefaultAsync(a => a.AssetItemId == input.AssetItemId && a.OwnerOrganisationId == org);
            if (item == null) return ServiceResult.Fail("Asset item not found.");

            // Check concurrency rowversion if provided
            if (!string.IsNullOrWhiteSpace(input.RowVersionBase64))
            {
                var clientBytes = Convert.FromBase64String(input.RowVersionBase64);
                if (!item.RowVersion.SequenceEqual(clientBytes))
                    return ServiceResult.Fail("This asset record was updated by another user. Please refresh and try again.");
            }

            // Cannot directly reset status to Available if active custody exists
            var hasActiveCustody = await db.AssetCustodies.AnyAsync(c => c.AssetItemId == item.AssetItemId && c.CustodyToUtc == null && c.Status == AssetCustodyStatuses.Active);
            if (hasActiveCustody && input.Status == AssetInventoryStatuses.Available)
                return ServiceResult.Fail("Asset currently has active staff custody and cannot be set directly to Available. Please process a return or transfer.");

            var hasActiveReservation = await db.AssetIssuanceItems.AnyAsync(li => li.AssetItemId == item.AssetItemId && li.IsActiveReservation);
            if (hasActiveReservation && input.Status == AssetInventoryStatuses.Available)
                return ServiceResult.Fail("Asset currently has an active reservation in an open issuance and cannot be set directly to Available.");

            var fromStatus = item.Status;
            item.CategoryId = input.CategoryId;
            item.AssetCode = assetCode;
            item.SerialNumber = serial;
            item.Name = input.Name.Trim();
            item.Brand = input.Brand?.Trim();
            item.Model = input.Model?.Trim();
            item.OwnerStructureId = input.OwnerStructureId > 0 ? input.OwnerStructureId : null;
            item.Location = input.Location?.Trim();
            item.Status = input.Status;
            item.CurrentConditionId = input.CurrentConditionId;
            item.WarrantyExpiryDate = input.WarrantyExpiryDate;
            item.DefaultAccessories = input.DefaultAccessories?.Trim();
            item.Notes = input.Notes?.Trim();
            item.IsActive = input.IsActive;
            item.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            RecordAssetEvent(db, org, item.AssetItemId, "AssetItem", item.AssetItemId, "AssetUpdated", fromStatus, item.Status, actor.UserId, new { item.AssetCode, item.Status });
            await db.SaveChangesAsync();
        }

        return ServiceResult.Ok();
    }

    public async Task<AssetIssuanceDetailDto> LoadIssuanceDetailAsync(int assetIssuanceId)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var issuance = await db.AssetIssuances.AsNoTracking()
            .Include(i => i.Lines)
                .ThenInclude(l => l.AssetItem)
                    .ThenInclude(a => a.Category)
            .Include(i => i.Lines)
                .ThenInclude(l => l.ConditionAtIssue)
            .Include(i => i.Lines)
                .ThenInclude(l => l.HandedOverByUser)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Acknowledgements)
                    .ThenInclude(ack => ack.ConfirmedByUser)
            .Include(i => i.Lines)
                .ThenInclude(l => l.HrDecisions)
                    .ThenInclude(hr => hr.DecidedByUser)
            .Include(i => i.Documents)
                .ThenInclude(d => d.GeneratedByUser)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == assetIssuanceId);

        if (issuance == null)
            throw new KeyNotFoundException("Asset issuance not found.");

        if (issuance.OrganisationId != org && !actor.IsSuperAdministrator && !await access.HasOrganisationAccessAsync(issuance.OrganisationId))
            throw new UnauthorizedAccessException("You do not have access to this issuance.");

        var canEditDraft = (isHrOrAdmin || issuance.CreatedByUserId == actor.UserId) &&
                           issuance.Status is AssetIssuanceStatuses.Draft or AssetIssuanceStatuses.ReturnedForCorrection;
        var canReserveAndHandOver = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetInventoryManage);
        var canStaffAcknowledge = issuance.IndividualId == actor.IndividualId &&
                                 issuance.Status == AssetIssuanceStatuses.PendingStaffAcknowledgement;
        var canHrReview = isHrOrAdmin || await access.HasPermissionAsync(Permissions.AssetHrApprove);
        var canCancel = (isHrOrAdmin || issuance.CreatedByUserId == actor.UserId) &&
                        AssetIssuanceStatuses.CanCancel(issuance.Status);

        var lines = issuance.Lines.OrderBy(l => l.AssetIssuanceItemId).Select(l =>
        {
            var latestAck = l.Acknowledgements.OrderByDescending(a => a.ItemVersion).ThenByDescending(a => a.ConfirmedAtUtc).FirstOrDefault();
            var latestHr = l.HrDecisions.OrderByDescending(h => h.ItemVersion).ThenByDescending(h => h.DecidedAtUtc).FirstOrDefault();

            return new AssetIssuanceLineDto
            {
                AssetIssuanceItemId = l.AssetIssuanceItemId,
                IssuanceId = l.IssuanceId,
                AssetItemId = l.AssetItemId,
                AssetCode = l.AssetCodeSnapshot,
                SerialNumber = l.SerialNumberSnapshot,
                CategoryName = l.CategoryNameSnapshot,
                ItemName = l.ItemNameSnapshot,
                Brand = l.BrandSnapshot,
                Model = l.ModelSnapshot,
                ConditionAtIssueId = l.ConditionAtIssueId,
                ConditionName = l.ConditionNameSnapshot,
                IsSecurityItem = l.IsSecurityItemSnapshot,
                Accessories = l.Accessories,
                ExpectedReturnDate = l.ExpectedReturnDate,
                HandedOverAtLocal = l.HandedOverAtUtc.HasValue ? l.HandedOverAtUtc.Value.AddHours(5) : null,
                HandedOverByName = l.HandedOverByUser != null ? l.HandedOverByUser.Username : null,
                HandoverNotes = l.HandoverNotes,
                Status = l.Status,
                IsActiveReservation = l.IsActiveReservation,
                ReplacedFromIssuanceItemId = l.ReplacedFromIssuanceItemId,
                Version = l.Version,
                RowVersionBase64 = Convert.ToBase64String(l.RowVersion),

                StaffDecision = latestAck?.Decision,
                StaffRejectionReasonCode = latestAck?.RejectionReasonCode,
                StaffRemarks = latestAck?.Remarks,
                StaffConfirmedAtLocal = latestAck != null ? latestAck.ConfirmedAtUtc.AddHours(5) : null,
                StaffDecisionVersion = latestAck?.ItemVersion,

                HrDecision = latestHr?.Decision,
                HrComments = latestHr?.Comments,
                HrDecidedAtLocal = latestHr != null ? latestHr.DecidedAtUtc.AddHours(5) : null,
                HrDecidedByName = latestHr?.DecidedByUser.Username,

                AcknowledgementHistory = l.Acknowledgements.OrderByDescending(a => a.ConfirmedAtUtc).Select(a => new AssetAcknowledgementHistoryDto(
                    a.AcknowledgementId,
                    a.ItemVersion,
                    a.Decision,
                    a.RejectionReasonCode,
                    a.Remarks,
                    a.ConfirmedByUser.Username,
                    a.ConfirmedAtUtc.AddHours(5))).ToList(),

                HrDecisionHistory = l.HrDecisions.OrderByDescending(h => h.DecidedAtUtc).Select(h => new AssetHrDecisionHistoryDto(
                    h.HrDecisionId,
                    h.ItemVersion,
                    h.Decision,
                    h.Comments,
                    h.DecidedByUser.Username,
                    h.DecidedAtUtc.AddHours(5))).ToList()
            };
        }).ToList();

        var documents = issuance.Documents.OrderByDescending(d => d.DocumentVersion).Select(d => new AssetDocumentSummaryDto(
            d.AssetDocumentId,
            d.AssetIssuanceId,
            d.DocumentNumber,
            d.DocumentVariant,
            d.DocumentVersion,
            d.Sha256Hash,
            d.GeneratedByUser.Username,
            d.GeneratedAtUtc.AddHours(5))).ToList();

        var events = await db.AssetEvents.AsNoTracking()
            .Where(e => (e.RelatedEntityType == "AssetIssuance" && e.RelatedEntityId == assetIssuanceId) ||
                        (e.RelatedEntityType == "AssetIssuanceItem" && issuance.Lines.Select(l => l.AssetIssuanceItemId).Contains(e.RelatedEntityId)))
            .OrderByDescending(e => e.AtUtc)
            .Take(50)
            .Select(e => new AssetEventDto(
                e.AssetEventId,
                e.AssetItemId,
                e.AssetItem != null ? e.AssetItem.AssetCode : null,
                e.RelatedEntityType,
                e.RelatedEntityId,
                e.EventType,
                e.FromStatus,
                e.ToStatus,
                e.ActorUser.Username,
                e.AtUtc.AddHours(5),
                e.MetadataJson))
            .ToListAsync();

        return new AssetIssuanceDetailDto
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            RequestId = issuance.RequestId,
            ReferenceNumber = issuance.ReferenceNumber,
            IndividualId = issuance.IndividualId,
            EmployeeName = issuance.EmployeeNameSnapshot,
            StaffNumber = issuance.StaffNumberSnapshot,
            JobId = issuance.JobId,
            PositionTitle = issuance.PositionTitleSnapshot,
            OrganisationId = issuance.OrganisationId,
            OrganisationName = issuance.OrganisationNameSnapshot,
            StructureId = issuance.StructureId,
            DepartmentName = issuance.DepartmentNameSnapshot,
            Reason = issuance.Reason,
            Status = issuance.Status,
            CreatedByUserId = issuance.CreatedByUserId,
            CreatedByName = issuance.CreatedByUser.Username,
            IssueDate = issuance.IssueDate,
            CreatedAtLocal = issuance.CreatedAtUtc.AddHours(5),
            UpdatedAtLocal = issuance.UpdatedAtUtc.AddHours(5),
            SubmittedAtLocal = issuance.SubmittedAtUtc.HasValue ? issuance.SubmittedAtUtc.Value.AddHours(5) : null,
            FinalizedAtLocal = issuance.FinalizedAtUtc.HasValue ? issuance.FinalizedAtUtc.Value.AddHours(5) : null,
            RowVersionBase64 = Convert.ToBase64String(issuance.RowVersion),

            CanEditDraft = canEditDraft,
            CanReserveAndHandOver = canReserveAndHandOver,
            CanStaffAcknowledge = canStaffAcknowledge,
            CanHrReview = canHrReview,
            CanCancel = canCancel,

            Lines = lines,
            Documents = documents,
            Events = events
        };
    }

    public async Task<ServiceResult> SaveIssuanceDraftAsync(AssetIssuanceDraftInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(input.OrganisationId);
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetIssuanceInitiate))
            return ServiceResult.Fail("You do not have permission to initiate staff asset issuance.");

        var errors = StaffAssetValidator.ValidateIssuanceDraft(input);
        if (errors.Count > 0) return ServiceResult.Fail(string.Join(" ", errors));

        await using var db = await factory.CreateDbContextAsync();

        // Verify individual and job
        var job = await db.Jobs.AsNoTracking()
            .Include(j => j.Individual)
            .Include(j => j.Organisation)
            .Include(j => j.OrganisationStructure)
            .Include(j => j.JobPositions.Where(p => p.JobPositionStateId == 4 && (p.ToDate == null || p.ToDate >= TodayMaldives)))
                .ThenInclude(p => p.Position)
            .FirstOrDefaultAsync(j => j.JobId == input.JobId && j.IndividualID == input.IndividualId && j.OrganisationID == org);

        if (job == null) return ServiceResult.Fail("Selected employee and active job were not found in this organisation.");

        var employeeName = string.Join(" ", new[] { job.Individual.FirstNameEnglish, job.Individual.MiddleNameEnglish, job.Individual.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var staffNumber = job.Sapnumber ?? job.JobId.ToString();
        var positionTitle = job.JobPositions.FirstOrDefault()?.Position.Name ?? "Staff";
        var deptName = job.OrganisationStructure?.Name ?? "";
        var orgName = job.Organisation.OrganisationName;

        // Verify asset items
        var requestedAssetIds = input.Lines.Select(l => l.AssetItemId).Distinct().ToList();
        var assetEntities = await db.AssetItems
            .Include(a => a.Category)
            .Include(a => a.CurrentCondition)
            .Where(a => requestedAssetIds.Contains(a.AssetItemId) && a.OwnerOrganisationId == org && a.IsActive)
            .ToListAsync();

        if (assetEntities.Count != requestedAssetIds.Count)
            return ServiceResult.Fail("One or more selected assets do not exist or are inactive.");

        // Check availability if reserving or handing over
        if (input.ReserveImmediately || input.VerifyHandoverImmediately)
        {
            var unavailable = assetEntities.Where(a => a.Status != AssetInventoryStatuses.Available).ToList();
            if (unavailable.Count > 0)
            {
                var codes = string.Join(", ", unavailable.Select(u => $"{u.AssetCode} ({u.Status})"));
                return ServiceResult.Fail($"The following assets are not Available for reservation: {codes}.");
            }
        }

        var conditions = await db.AssetConditions.AsNoTracking().ToDictionaryAsync(c => c.AssetConditionId);

        AssetIssuance? issuance;
        if (input.AssetIssuanceId == 0)
        {
            var refNumber = $"SAI-{TodayMaldives.Year}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
            issuance = new AssetIssuance
            {
                ReferenceNumber = refNumber,
                IndividualId = input.IndividualId,
                JobId = input.JobId,
                OrganisationId = org,
                StructureId = input.StructureId > 0 ? input.StructureId : job.OrganisationStructureId,
                Reason = input.Reason.Trim(),
                Status = AssetIssuanceStatuses.Draft,
                CreatedByUserId = actor.UserId,
                IssueDate = input.IssueDate,
                EmployeeNameSnapshot = employeeName,
                StaffNumberSnapshot = staffNumber,
                PositionTitleSnapshot = positionTitle,
                DepartmentNameSnapshot = deptName,
                OrganisationNameSnapshot = orgName,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.AssetIssuances.Add(issuance);
            await db.SaveChangesAsync();

            // Link workflow Request if available
            if (requests != null)
            {
                try
                {
                    var reqType = await db.RequestTypes.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TypeName == RequestTypeName);
                    if (reqType != null)
                    {
                        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"Staff Asset Issuance Creation for staff {input.IndividualId}");
                        var req = await requests.CreateAsync(db, new CreateRequestDto
                        {
                            RequestTypeId = reqType.RequestTypeId,
                            ApplicantBusinessEntityId = input.IndividualId,
                            OrganisationBusinessEntityId = org,
                            EffectiveDate = input.IssueDate,
                            OperationLogId = audit.OperationLogId
                        });
                        issuance.RequestId = req.RequestId;
                        issuance.ReferenceNumber = req.ReferenceNumber ?? issuance.ReferenceNumber;
                        await db.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Non-blocking fallback to internal reference
                }
            }
        }
        else
        {
            issuance = await db.AssetIssuances.Include(i => i.Lines).FirstOrDefaultAsync(i => i.AssetIssuanceId == input.AssetIssuanceId);
            if (issuance == null) return ServiceResult.Fail("Issuance not found.");

            if (issuance.Status is not (AssetIssuanceStatuses.Draft or AssetIssuanceStatuses.ReturnedForCorrection))
                return ServiceResult.Fail($"Cannot edit issuance while in state '{issuance.Status}'.");

            issuance.Reason = input.Reason.Trim();
            issuance.IssueDate = input.IssueDate;
            issuance.UpdatedAtUtc = DateTime.UtcNow;

            // Remove proposed lines that were unselected
            var incomingItemIds = input.Lines.Select(l => l.AssetItemId).ToHashSet();
            var removableLines = issuance.Lines.Where(l => l.Status == AssetIssuanceLineStatuses.Proposed && !incomingItemIds.Contains(l.AssetItemId)).ToList();
            db.AssetIssuanceItems.RemoveRange(removableLines);
        }

        // Add or update lines
        foreach (var lineInput in input.Lines)
        {
            var asset = assetEntities.First(a => a.AssetItemId == lineInput.AssetItemId);
            var cond = conditions.TryGetValue(lineInput.ConditionAtIssueId, out var cVal) ? cVal : asset.CurrentCondition;

            var existingLine = issuance.Lines.FirstOrDefault(l => l.AssetItemId == lineInput.AssetItemId);
            if (existingLine == null)
            {
                var newLine = new AssetIssuanceItem
                {
                    IssuanceId = issuance.AssetIssuanceId,
                    AssetItemId = asset.AssetItemId,
                    ConditionAtIssueId = cond.AssetConditionId,
                    Accessories = lineInput.Accessories?.Trim() ?? asset.DefaultAccessories,
                    Status = AssetIssuanceLineStatuses.Proposed,
                    IsActiveReservation = false,
                    Version = 1,
                    ExpectedReturnDate = lineInput.ExpectedReturnDate,
                    AssetCodeSnapshot = asset.AssetCode,
                    SerialNumberSnapshot = asset.SerialNumber,
                    CategoryNameSnapshot = asset.Category.Name,
                    ItemNameSnapshot = asset.Name,
                    BrandSnapshot = asset.Brand,
                    ModelSnapshot = asset.Model,
                    ConditionNameSnapshot = cond.Name,
                    IsSecurityItemSnapshot = asset.Category.IsSecurityItem
                };
                db.AssetIssuanceItems.Add(newLine);
                if (!issuance.Lines.Contains(newLine))
                {
                    issuance.Lines.Add(newLine);
                }
            }
            else
            {
                existingLine.ConditionAtIssueId = cond.AssetConditionId;
                existingLine.ConditionNameSnapshot = cond.Name;
                existingLine.Accessories = lineInput.Accessories?.Trim();
                existingLine.ExpectedReturnDate = lineInput.ExpectedReturnDate;
            }
        }

        await db.SaveChangesAsync();

        // Process immediate reservation or handover if requested
        if (input.VerifyHandoverImmediately)
        {
            var handoverInput = new AssetHandoverInput
            {
                AssetIssuanceId = issuance.AssetIssuanceId,
                Lines = issuance.Lines.Select(l => new AssetHandoverLineInput
                {
                    AssetIssuanceItemId = l.AssetIssuanceItemId,
                    ConditionAtIssueId = l.ConditionAtIssueId,
                    Accessories = l.Accessories,
                    HandoverNotes = input.HandoverNotes,
                    ExpectedReturnDate = l.ExpectedReturnDate
                }).ToList()
            };
            var handoverRes = await VerifyHandoverAsync(handoverInput);
            if (!handoverRes.Success) return handoverRes;
        }
        else if (input.ReserveImmediately)
        {
            var reserveRes = await ReserveIssuanceLinesAsync(issuance.AssetIssuanceId);
            if (!reserveRes.Success) return reserveRes;
        }

        RecordAssetEvent(db, org, null, "AssetIssuance", issuance.AssetIssuanceId, "IssuanceSaved", null, issuance.Status, actor.UserId, new { issuance.ReferenceNumber, TotalLines = issuance.Lines.Count });
        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReserveIssuanceLinesAsync(int assetIssuanceId)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetInventoryManage))
            return ServiceResult.Fail("You do not have permission to reserve inventory assets.");

        await using var db = await factory.CreateDbContextAsync();
        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
                .ThenInclude(l => l.AssetItem)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == assetIssuanceId);

        if (issuance == null) return ServiceResult.Fail("Issuance not found.");

        var proposedLines = issuance.Lines.Where(l => l.Status == AssetIssuanceLineStatuses.Proposed).ToList();
        if (proposedLines.Count == 0)
            return ServiceResult.Fail("No proposed lines available to reserve.");

        // Check availability
        foreach (var line in proposedLines)
        {
            if (line.AssetItem.Status != AssetInventoryStatuses.Available)
                return ServiceResult.Fail($"Asset '{line.AssetItem.AssetCode}' is no longer Available (current status: {line.AssetItem.Status}).");

            line.Status = AssetIssuanceLineStatuses.Reserved;
            line.IsActiveReservation = true;
            line.AssetItem.Status = AssetInventoryStatuses.Reserved;
            line.AssetItem.UpdatedAtUtc = DateTime.UtcNow;

            RecordAssetEvent(db, org, line.AssetItemId, "AssetIssuanceItem", line.AssetIssuanceItemId, "LineReserved", "Proposed", "Reserved", actor.UserId);
        }

        if (issuance.Status == AssetIssuanceStatuses.Draft)
            issuance.Status = AssetIssuanceStatuses.AwaitingHandover;

        issuance.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> VerifyHandoverAsync(AssetHandoverInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetInventoryManage))
            return ServiceResult.Fail("You do not have permission to verify physical handover of assets.");

        if (input.Lines.Count == 0)
            return ServiceResult.Fail("No lines selected for handover.");

        await using var db = await factory.CreateDbContextAsync();
        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
                .ThenInclude(l => l.AssetItem)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == input.AssetIssuanceId);

        if (issuance == null) return ServiceResult.Fail("Issuance not found.");

        var lineMap = input.Lines.GroupBy(l => l.AssetIssuanceItemId).ToDictionary(g => g.Key, g => g.Last());
        var handoverTimeUtc = DateTime.UtcNow;

        foreach (var line in issuance.Lines)
        {
            if (!lineMap.TryGetValue(line.AssetIssuanceItemId, out var handoverLine)) continue;

            if (line.Status is not (AssetIssuanceLineStatuses.Proposed or AssetIssuanceLineStatuses.Reserved))
                return ServiceResult.Fail($"Line #{line.AssetIssuanceItemId} ({line.AssetCodeSnapshot}) cannot be handed over from status '{line.Status}'.");

            // Verify physical possession blocked state: asset inventory becomes PendingHandover
            line.Status = AssetIssuanceLineStatuses.HandedOver;
            line.IsActiveReservation = true;
            line.HandedOverAtUtc = handoverTimeUtc;
            line.HandedOverByUserId = actor.UserId;
            line.HandoverNotes = handoverLine.HandoverNotes?.Trim();
            if (handoverLine.ConditionAtIssueId > 0)
                line.ConditionAtIssueId = handoverLine.ConditionAtIssueId;
            if (!string.IsNullOrWhiteSpace(handoverLine.Accessories))
                line.Accessories = handoverLine.Accessories.Trim();
            if (handoverLine.ExpectedReturnDate.HasValue)
                line.ExpectedReturnDate = handoverLine.ExpectedReturnDate;

            line.AssetItem.Status = AssetInventoryStatuses.PendingHandover;
            line.AssetItem.UpdatedAtUtc = handoverTimeUtc;

            RecordAssetEvent(db, org, line.AssetItemId, "AssetIssuanceItem", line.AssetIssuanceItemId, "HandoverVerified", "Reserved", "HandedOver", actor.UserId, new { HandedOverAtUtc = handoverTimeUtc, line.Accessories });
        }

        issuance.Status = AssetIssuanceStatuses.PendingStaffAcknowledgement;
        issuance.SubmittedAtUtc ??= handoverTimeUtc;
        issuance.UpdatedAtUtc = handoverTimeUtc;

        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelIssuanceAsync(int assetIssuanceId, string reason)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
                .ThenInclude(l => l.AssetItem)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == assetIssuanceId);

        if (issuance == null) return ServiceResult.Fail("Issuance not found.");

        if (!isHrOrAdmin && issuance.CreatedByUserId != actor.UserId)
            return ServiceResult.Fail("You are not authorized to cancel this issuance.");

        if (!AssetIssuanceStatuses.CanCancel(issuance.Status))
            return ServiceResult.Fail($"Issuance in status '{issuance.Status}' cannot be cancelled.");

        // Release reservations on assets
        foreach (var line in issuance.Lines)
        {
            if (line.IsActiveReservation)
            {
                line.IsActiveReservation = false;
                line.Status = AssetIssuanceLineStatuses.Closed;
                line.AssetItem.Status = AssetInventoryStatuses.Available;
                line.AssetItem.UpdatedAtUtc = DateTime.UtcNow;

                RecordAssetEvent(db, org, line.AssetItemId, "AssetIssuanceItem", line.AssetIssuanceItemId, "ReservationReleased", line.Status, "Closed", actor.UserId, new { Reason = reason });
            }
        }

        issuance.Status = AssetIssuanceStatuses.Cancelled;
        issuance.UpdatedAtUtc = DateTime.UtcNow;

        RecordAssetEvent(db, org, null, "AssetIssuance", issuance.AssetIssuanceId, "IssuanceCancelled", null, "Cancelled", actor.UserId, new { Reason = reason });
        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<MyAssetsPortalData> LoadMyAssetsPortalAsync()
    {
        var actor = await access.RequireContextAsync();
        if (actor.IndividualId <= 0)
            throw new UnauthorizedAccessException("Signed-in user does not have a linked staff record.");

        await using var db = await factory.CreateDbContextAsync();
        var individualId = actor.IndividualId;

        var ind = await db.Individuals.AsNoTracking().FirstOrDefaultAsync(i => i.BusinessEntityId == individualId);
        var activeJob = await db.Jobs.AsNoTracking()
            .Include(j => j.Organisation)
            .Include(j => j.OrganisationStructure)
            .Include(j => j.JobPositions.Where(p => p.JobPositionStateId == 4 && (p.ToDate == null || p.ToDate >= TodayMaldives)))
                .ThenInclude(p => p.Position)
            .FirstOrDefaultAsync(j => j.IndividualID == individualId && j.JobStateId == 4 && (j.TerminatedDate == null || j.TerminatedDate >= TodayMaldives));

        var fullName = ind != null ? string.Join(" ", new[] { ind.FirstNameEnglish, ind.MiddleNameEnglish, ind.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))) : "Staff";
        var staffNumber = activeJob?.Sapnumber ?? activeJob?.JobId.ToString() ?? "";
        var positionTitle = activeJob?.JobPositions.FirstOrDefault()?.Position.Name ?? "Staff";
        var deptName = activeJob?.OrganisationStructure?.Name ?? "";
        var orgName = activeJob?.Organisation.OrganisationName ?? "";

        // Pending issuances awaiting employee decision
        var pendingIssuanceIds = await db.AssetIssuances.AsNoTracking()
            .Where(i => i.IndividualId == individualId && i.Status == AssetIssuanceStatuses.PendingStaffAcknowledgement)
            .Select(i => i.AssetIssuanceId)
            .ToListAsync();

        var pendingIssuances = new List<AssetIssuanceDetailDto>();
        foreach (var id in pendingIssuanceIds)
        {
            var detail = await LoadIssuanceDetailAsync(id);
            pendingIssuances.Add(detail);
        }

        // Active custodies
        var custodiesQuery = db.AssetCustodies.AsNoTracking()
            .Include(c => c.AssetItem)
                .ThenInclude(a => a.Category)
            .Include(c => c.IssuanceItem)
                .ThenInclude(li => li.Issuance)
            .Include(c => c.Structure)
            .Include(c => c.Organisation)
            .Where(c => c.IndividualId == individualId);

        var activeCustodies = await custodiesQuery
            .Where(c => c.CustodyToUtc == null && c.Status == AssetCustodyStatuses.Active)
            .OrderByDescending(c => c.CustodyFromUtc)
            .Select(c => new AssetCustodyRowDto
            {
                AssetCustodyId = c.AssetCustodyId,
                AssetItemId = c.AssetItemId,
                AssetCode = c.AssetItem.AssetCode,
                SerialNumber = c.AssetItem.SerialNumber,
                CategoryName = c.AssetItem.Category.Name,
                ItemName = c.AssetItem.Name,
                Brand = c.AssetItem.Brand,
                Model = c.AssetItem.Model,
                ConditionAtIssueName = c.IssuanceItem.ConditionNameSnapshot,
                Accessories = c.IssuanceItem.Accessories,
                IndividualId = c.IndividualId,
                EmployeeName = fullName,
                StaffNumber = staffNumber,
                JobId = c.JobId,
                PositionTitle = positionTitle,
                OrganisationId = c.OrganisationId,
                OrganisationName = c.Organisation.OrganisationName,
                StructureId = c.StructureId,
                DepartmentName = c.Structure != null ? c.Structure.Name : deptName,
                IssuanceId = c.IssuanceItem.IssuanceId,
                IssuanceReference = c.IssuanceItem.Issuance.ReferenceNumber,
                IssuanceItemId = c.IssuanceItemId,
                CustodyFromLocal = c.CustodyFromUtc.AddHours(5),
                CustodyToLocal = null,
                ExpectedReturnDate = c.ExpectedReturnDate,
                IsOverdueReturn = c.ExpectedReturnDate != null && c.ExpectedReturnDate < TodayMaldives,
                Status = c.Status
            })
            .ToListAsync();

        // Historical custodies
        var custodyHistory = await custodiesQuery
            .Where(c => c.CustodyToUtc != null || c.Status != AssetCustodyStatuses.Active)
            .OrderByDescending(c => c.CustodyFromUtc)
            .Take(100)
            .Select(c => new AssetCustodyRowDto
            {
                AssetCustodyId = c.AssetCustodyId,
                AssetItemId = c.AssetItemId,
                AssetCode = c.AssetItem.AssetCode,
                SerialNumber = c.AssetItem.SerialNumber,
                CategoryName = c.AssetItem.Category.Name,
                ItemName = c.AssetItem.Name,
                Brand = c.AssetItem.Brand,
                Model = c.AssetItem.Model,
                ConditionAtIssueName = c.IssuanceItem.ConditionNameSnapshot,
                Accessories = c.IssuanceItem.Accessories,
                IndividualId = c.IndividualId,
                EmployeeName = fullName,
                StaffNumber = staffNumber,
                JobId = c.JobId,
                PositionTitle = positionTitle,
                OrganisationId = c.OrganisationId,
                OrganisationName = c.Organisation.OrganisationName,
                StructureId = c.StructureId,
                DepartmentName = c.Structure != null ? c.Structure.Name : deptName,
                IssuanceId = c.IssuanceItem.IssuanceId,
                IssuanceReference = c.IssuanceItem.Issuance.ReferenceNumber,
                IssuanceItemId = c.IssuanceItemId,
                CustodyFromLocal = c.CustodyFromUtc.AddHours(5),
                CustodyToLocal = c.CustodyToUtc.HasValue ? c.CustodyToUtc.Value.AddHours(5) : null,
                ExpectedReturnDate = c.ExpectedReturnDate,
                IsOverdueReturn = false,
                Status = c.Status,
                ClosedReason = c.ClosedReason
            })
            .ToListAsync();

        // Returns
        var returns = await db.AssetReturns.AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.ReturnedToUser)
            .Include(r => r.Items)
                .ThenInclude(i => i.AssetItem)
                    .ThenInclude(a => a.Category)
            .Where(r => r.IndividualId == individualId)
            .OrderByDescending(r => r.ReturnDate)
            .Take(50)
            .Select(r => new AssetReturnDetailDto
            {
                AssetReturnId = r.AssetReturnId,
                ReferenceNumber = r.ReferenceNumber,
                OrganisationId = r.OrganisationId,
                IndividualId = r.IndividualId,
                EmployeeName = fullName,
                JobId = r.JobId,
                StaffTransferId = r.StaffTransferId,
                ReturnType = r.ReturnType,
                RequestedByName = r.RequestedByUser.Username,
                ReturnedToName = r.ReturnedToUser != null ? r.ReturnedToUser.Username : null,
                ReturnDateLocal = r.ReturnDate.AddHours(5),
                CompletedAtLocal = r.CompletedAtUtc.HasValue ? r.CompletedAtUtc.Value.AddHours(5) : null,
                Status = r.Status,
                Notes = r.Notes,
                Items = r.Items.Select(item => new AssetReturnItemDto
                {
                    AssetReturnItemId = item.AssetReturnItemId,
                    AssetCustodyId = item.AssetCustodyId,
                    AssetItemId = item.AssetItemId,
                    AssetCode = item.AssetItem.AssetCode,
                    SerialNumber = item.AssetItem.SerialNumber,
                    CategoryName = item.AssetItem.Category.Name,
                    ItemName = item.AssetItem.Name,
                    ConditionAtReturnId = item.ConditionAtReturnId,
                    ConditionAtReturnName = item.ConditionAtReturn != null ? item.ConditionAtReturn.Name : null,
                    AccessoryChecklist = item.AccessoryChecklist,
                    MissingAccessories = item.MissingAccessories,
                    DamageOrLossEvidence = item.DamageOrLossEvidence,
                    InspectionResult = item.InspectionResult,
                    PostReturnDisposition = item.PostReturnDisposition,
                    ReceivedByName = item.ReceivedByUser != null ? item.ReceivedByUser.Username : null,
                    ReceivedAtLocal = item.ReceivedAtUtc.HasValue ? item.ReceivedAtUtc.Value.AddHours(5) : null,
                    Status = item.Status
                }).ToList()
            })
            .ToListAsync();

        // Approved documents
        var documents = await db.AssetDocuments.AsNoTracking()
            .Include(d => d.GeneratedByUser)
            .Where(d => d.AssetIssuance.IndividualId == individualId && d.DocumentVariant == AssetDocumentVariants.StandardHandoverRecord)
            .OrderByDescending(d => d.GeneratedAtUtc)
            .Take(50)
            .Select(d => new AssetDocumentSummaryDto(
                d.AssetDocumentId,
                d.AssetIssuanceId,
                d.DocumentNumber,
                d.DocumentVariant,
                d.DocumentVersion,
                d.Sha256Hash,
                d.GeneratedByUser.Username,
                d.GeneratedAtUtc.AddHours(5)))
            .ToListAsync();

        return new MyAssetsPortalData
        {
            IndividualId = individualId,
            EmployeeNumber = staffNumber,
            FullName = fullName,
            DepartmentName = deptName,
            PositionTitle = positionTitle,
            OrganisationName = orgName,
            PendingIssuances = pendingIssuances,
            ActiveCustodies = activeCustodies,
            CustodyHistory = custodyHistory,
            Returns = returns,
            ApprovedDocuments = documents
        };
    }

    public async Task<ServiceResult> SubmitStaffDecisionsAsync(StaffAssetDecisionBatchInput input)
    {
        var actor = await access.RequireContextAsync();
        if (actor.IndividualId <= 0)
            return ServiceResult.Fail("You must have a linked staff record to acknowledge assets.");

        var errors = StaffAssetValidator.ValidateStaffDecisionBatch(input);
        if (errors.Count > 0) return ServiceResult.Fail(string.Join(" ", errors));

        await using var db = await factory.CreateDbContextAsync();
        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == input.AssetIssuanceId);

        if (issuance == null) return ServiceResult.Fail("Issuance not found.");

        // Security check: cannot acknowledge on behalf of another employee
        if (issuance.IndividualId != actor.IndividualId)
            return ServiceResult.Fail("You can only submit acknowledgements for your own issued assets.");

        if (issuance.Status != AssetIssuanceStatuses.PendingStaffAcknowledgement)
            return ServiceResult.Fail($"Issuance is not currently awaiting staff acknowledgement (current status: {issuance.Status}).");

        var decisionMap = input.Decisions.ToDictionary(d => d.AssetIssuanceItemId);
        var nowUtc = DateTime.UtcNow;

        foreach (var line in issuance.Lines)
        {
            if (!decisionMap.TryGetValue(line.AssetIssuanceItemId, out var dec)) continue;

            if (line.Status != AssetIssuanceLineStatuses.HandedOver)
                return ServiceResult.Fail($"Line #{line.AssetIssuanceItemId} ({line.AssetCodeSnapshot}) is not in HandedOver status.");

            if (line.Version != dec.ExpectedVersion)
                return ServiceResult.Fail($"Line #{line.AssetIssuanceItemId} has been modified (version mismatch). Please refresh.");

            var fromStatus = line.Status;
            line.Status = dec.Decision == AssetStaffDecisions.Accepted
                ? AssetIssuanceLineStatuses.Accepted
                : AssetIssuanceLineStatuses.Rejected;

            var ack = new AssetAcknowledgement
            {
                IssuanceItemId = line.AssetIssuanceItemId,
                IndividualId = actor.IndividualId,
                Decision = dec.Decision,
                RejectionReasonCode = dec.Decision == AssetStaffDecisions.Rejected ? dec.RejectionReasonCode : null,
                Remarks = dec.Remarks?.Trim(),
                AcknowledgementStatement = StaffAssetValidator.DefaultAcknowledgementStatement,
                ConfirmedByUserId = actor.UserId,
                ConfirmedAtUtc = nowUtc,
                ItemVersion = line.Version
            };
            db.AssetAcknowledgements.Add(ack);

            RecordAssetEvent(db, issuance.OrganisationId, line.AssetItemId, "AssetAcknowledgement", line.AssetIssuanceItemId,
                dec.Decision == AssetStaffDecisions.Accepted ? "StaffAccepted" : "StaffRejected",
                fromStatus, line.Status, actor.UserId,
                new { dec.Decision, dec.RejectionReasonCode, dec.Remarks, ItemVersion = line.Version });
        }

        // Evaluate overall issuance status
        var allLinesDecided = issuance.Lines.All(l => l.Status is AssetIssuanceLineStatuses.Accepted or AssetIssuanceLineStatuses.Rejected or AssetIssuanceLineStatuses.Closed);
        if (allLinesDecided)
        {
            var hasAccepted = issuance.Lines.Any(l => l.Status == AssetIssuanceLineStatuses.Accepted);
            issuance.Status = hasAccepted ? AssetIssuanceStatuses.PendingHRApproval : AssetIssuanceStatuses.ReturnedForCorrection;
        }

        issuance.UpdatedAtUtc = nowUtc;
        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SubmitHrReviewAsync(AssetHrReviewBatchInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetHrApprove))
            return ServiceResult.Fail("You do not have permission to authorize asset issuances.");

        var errors = StaffAssetValidator.ValidateHrReviewBatch(input);
        if (errors.Count > 0) return ServiceResult.Fail(string.Join(" ", errors));

        await using var db = await factory.CreateDbContextAsync();
        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
                .ThenInclude(l => l.AssetItem)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == input.AssetIssuanceId);

        if (issuance == null) return ServiceResult.Fail("Issuance not found.");

        if (issuance.OrganisationId != org && !actor.IsSuperAdministrator && !await access.HasOrganisationAccessAsync(issuance.OrganisationId))
            return ServiceResult.Fail("You cannot review issuances belonging to another organisation.");

        var decisionMap = input.Decisions.ToDictionary(d => d.AssetIssuanceItemId);
        var nowUtc = DateTime.UtcNow;

        var idempotencyPrefix = string.IsNullOrWhiteSpace(input.IdempotencyKey)
            ? $"HR-{issuance.AssetIssuanceId}-{DateTime.UtcNow.Ticks}"
            : input.IdempotencyKey.Trim();

        foreach (var line in issuance.Lines)
        {
            if (!decisionMap.TryGetValue(line.AssetIssuanceItemId, out var dec)) continue;

            if (line.Version != dec.ExpectedVersion)
                return ServiceResult.Fail($"Line #{line.AssetIssuanceItemId} ({line.AssetCodeSnapshot}) version changed. Please refresh.");

            var lineIdempotencyKey = $"{idempotencyPrefix}-L{line.AssetIssuanceItemId}-V{line.Version}";
            var alreadyProcessed = await db.AssetHrDecisions.AnyAsync(h => h.IssuanceItemId == line.AssetIssuanceItemId && h.ItemVersion == line.Version && h.IdempotencyKey == lineIdempotencyKey);
            if (alreadyProcessed) continue;

            if (line.Status == AssetIssuanceLineStatuses.HRApproved && dec.Decision == AssetHrDecisionKinds.HRApproved)
                continue;
            if (line.Status == AssetIssuanceLineStatuses.Closed && dec.Decision == AssetHrDecisionKinds.Closed)
                continue;

            var fromStatus = line.Status;

            if (dec.Decision == AssetHrDecisionKinds.HRApproved)
            {
                // Rule: Only accepted lines can be approved. HR cannot override employee rejection.
                if (line.Status != AssetIssuanceLineStatuses.Accepted)
                    return ServiceResult.Fail($"Line #{line.AssetIssuanceItemId} cannot be HR approved because it was not Accepted by staff (current status: {line.Status}).");

                line.Status = AssetIssuanceLineStatuses.HRApproved;
                line.IsActiveReservation = false;

                // Create active custody atomically
                var custodyIdempotency = $"CUST-{line.AssetIssuanceItemId}-{line.Version}";
                var custody = await db.AssetCustodies.FirstOrDefaultAsync(c => c.IssuanceItemId == line.AssetIssuanceItemId);
                if (custody == null)
                {
                    custody = new AssetCustody
                    {
                        AssetItemId = line.AssetItemId,
                        IndividualId = issuance.IndividualId,
                        JobId = issuance.JobId,
                        OrganisationId = issuance.OrganisationId,
                        StructureId = issuance.StructureId,
                        IssuanceItemId = line.AssetIssuanceItemId,
                        CustodyFromUtc = line.HandedOverAtUtc ?? nowUtc,
                        CustodyToUtc = null,
                        ExpectedReturnDate = line.ExpectedReturnDate,
                        Status = AssetCustodyStatuses.Active,
                        IdempotencyKey = custodyIdempotency
                    };
                    db.AssetCustodies.Add(custody);
                }

                line.AssetItem.Status = AssetInventoryStatuses.Assigned;
                line.AssetItem.UpdatedAtUtc = nowUtc;
            }
            else if (dec.Decision == AssetHrDecisionKinds.ReturnedForCorrection)
            {
                line.Status = AssetIssuanceLineStatuses.ReturnedForCorrection;
            }
            else if (dec.Decision == AssetHrDecisionKinds.Closed)
            {
                line.Status = AssetIssuanceLineStatuses.Closed;
                line.IsActiveReservation = false;
                line.AssetItem.Status = AssetInventoryStatuses.Available;
                line.AssetItem.UpdatedAtUtc = nowUtc;
            }

            var hrDec = new AssetHrDecision
            {
                IssuanceItemId = line.AssetIssuanceItemId,
                Decision = dec.Decision,
                Comments = dec.Comments?.Trim(),
                DecidedByUserId = actor.UserId,
                DecidedAtUtc = nowUtc,
                ItemVersion = line.Version,
                IdempotencyKey = lineIdempotencyKey
            };
            db.AssetHrDecisions.Add(hrDec);

            RecordAssetEvent(db, issuance.OrganisationId, line.AssetItemId, "AssetHrDecision", line.AssetIssuanceItemId,
                dec.Decision, fromStatus, line.Status, actor.UserId,
                new { dec.Decision, dec.Comments, ItemVersion = line.Version, lineIdempotencyKey });
        }

        // Determine terminal issuance status
        var allTerminal = issuance.Lines.All(l => l.Status is AssetIssuanceLineStatuses.HRApproved or AssetIssuanceLineStatuses.Closed);
        var anyReturned = issuance.Lines.Any(l => l.Status == AssetIssuanceLineStatuses.ReturnedForCorrection);

        if (anyReturned)
        {
            issuance.Status = AssetIssuanceStatuses.ReturnedForCorrection;
        }
        else if (allTerminal)
        {
            var anyApproved = issuance.Lines.Any(l => l.Status == AssetIssuanceLineStatuses.HRApproved);
            var anyClosed = issuance.Lines.Any(l => l.Status == AssetIssuanceLineStatuses.Closed);

            issuance.Status = (anyApproved && anyClosed)
                ? AssetIssuanceStatuses.PartiallyCompleted
                : anyApproved
                    ? AssetIssuanceStatuses.Completed
                    : AssetIssuanceStatuses.Cancelled;

            issuance.FinalizedAtUtc = nowUtc;
            issuance.FinalizedByUserId = actor.UserId;
            issuance.FinalizationIdempotencyKey = idempotencyPrefix;

            // Generate approved document snapshots
            await GenerateAndSaveDocumentsAsync(db, issuance, actor.UserId, nowUtc);

            // Update workflow request if present
            if (issuance.RequestId.HasValue)
            {
                var req = await db.Requests.FirstOrDefaultAsync(r => r.RequestId == issuance.RequestId.Value);
                if (req != null)
                {
                    req.RequestStateId = 4; // Approved
                }
            }
        }

        issuance.UpdatedAtUtc = nowUtc;
        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    private static async Task GenerateAndSaveDocumentsAsync(HrmTeContext db, AssetIssuance issuance, int userId, DateTime nowUtc)
    {
        var existingDoc = await db.AssetDocuments.FirstOrDefaultAsync(d => d.AssetIssuanceId == issuance.AssetIssuanceId && d.DocumentVariant == AssetDocumentVariants.StandardHandoverRecord);
        var version = (existingDoc?.DocumentVersion ?? 0) + 1;

        // Build printable document DTO
        var stdDoc = BuildPrintableDto(issuance, AssetDocumentVariants.StandardHandoverRecord, version, nowUtc);
        var stdJson = JsonSerializer.Serialize(stdDoc);
        var stdHash = CalculateSha256(stdJson);

        var docEntity = new AssetDocument
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            DocumentNumber = stdDoc.DocumentNumber,
            DocumentVariant = AssetDocumentVariants.StandardHandoverRecord,
            DocumentVersion = version,
            SnapshotJson = stdJson,
            Sha256Hash = stdHash,
            ContentType = "application/pdf",
            GeneratedByUserId = userId,
            GeneratedAtUtc = nowUtc
        };
        db.AssetDocuments.Add(docEntity);

        // Also build restricted security variant if any items are security items
        var hasSecurityItems = issuance.Lines.Any(l => l.IsSecurityItemSnapshot);
        if (hasSecurityItems)
        {
            var secDoc = BuildPrintableDto(issuance, AssetDocumentVariants.RestrictedSecurityRecord, version, nowUtc);
            var secJson = JsonSerializer.Serialize(secDoc);
            var secHash = CalculateSha256(secJson);

            var secEntity = new AssetDocument
            {
                AssetIssuanceId = issuance.AssetIssuanceId,
                DocumentNumber = secDoc.DocumentNumber,
                DocumentVariant = AssetDocumentVariants.RestrictedSecurityRecord,
                DocumentVersion = version,
                SnapshotJson = secJson,
                Sha256Hash = secHash,
                ContentType = "application/pdf",
                GeneratedByUserId = userId,
                GeneratedAtUtc = nowUtc
            };
            db.AssetDocuments.Add(secEntity);
        }
    }

    private static AssetPrintableDocumentDto BuildPrintableDto(AssetIssuance issuance, string variant, int version, DateTime nowUtc)
    {
        var docNumber = $"DOC-{issuance.ReferenceNumber}-V{version}{(variant == AssetDocumentVariants.RestrictedSecurityRecord ? "-SEC" : "")}";
        var lines = (issuance.Lines ?? []).OrderBy(l => l.AssetIssuanceItemId).Select((l, index) =>
        {
            var latestAck = l.Acknowledgements?.OrderByDescending(a => a.ItemVersion).FirstOrDefault();
            var latestHr = l.HrDecisions?.OrderByDescending(h => h.ItemVersion).FirstOrDefault();

            var serialDisplay = variant == AssetDocumentVariants.RestrictedSecurityRecord
                ? l.SerialNumberSnapshot ?? "N/A"
                : (l.IsSecurityItemSnapshot && (l.SerialNumberSnapshot?.Length ?? 0) > 4)
                    ? $"***{l.SerialNumberSnapshot![^4..]}"
                    : l.SerialNumberSnapshot ?? "N/A";

            return new AssetPrintableLineDto
            {
                LineNumber = index + 1,
                CategoryName = l.CategoryNameSnapshot ?? "",
                AssetCode = l.AssetCodeSnapshot ?? "",
                ItemName = l.ItemNameSnapshot ?? "",
                BrandModel = string.Join(" ", new[] { l.BrandSnapshot, l.ModelSnapshot }.Where(x => !string.IsNullOrWhiteSpace(x))),
                SerialNumberDisplay = serialDisplay,
                ConditionName = l.ConditionNameSnapshot ?? "",
                Accessories = l.Accessories ?? "Standard",
                HandedOverAtLocal = l.HandedOverAtUtc?.AddHours(5),
                HandedOverByName = l.HandedOverByUser?.Username ?? "",
                StaffDecision = latestAck?.Decision ?? l.Status,
                StaffRemarks = latestAck?.Remarks ?? "",
                StaffConfirmedAtLocal = latestAck?.ConfirmedAtUtc.AddHours(5),
                HrDecision = latestHr?.Decision ?? l.Status,
                HrOfficerName = latestHr?.DecidedByUser?.Username ?? "",
                HrDecidedAtLocal = latestHr?.DecidedAtUtc.AddHours(5),
                CustodyStatus = l.Status == AssetIssuanceLineStatuses.HRApproved ? "Active Custody Authorized" : "Not In Custody",
                ItemVersion = l.Version
            };
        }).ToList();

        var doc = new AssetPrintableDocumentDto
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            DocumentNumber = docNumber,
            DocumentVariant = variant,
            DocumentVersion = version,
            GeneratedAtLocal = nowUtc.AddHours(5),
            GeneratedByName = "System Finalizer",
            OrganisationName = issuance.OrganisationNameSnapshot,
            DepartmentName = issuance.DepartmentNameSnapshot,
            IssuanceReference = issuance.ReferenceNumber,
            IssuanceStatus = issuance.Status,
            IssueDate = issuance.IssueDate,
            EmployeeName = issuance.EmployeeNameSnapshot,
            StaffNumber = issuance.StaffNumberSnapshot,
            PositionTitle = issuance.PositionTitleSnapshot,
            Reason = issuance.Reason,
            CustodyTermsText = "The staff member acknowledges receipt of the equipment listed above in good working condition. The staff member agrees to exercise reasonable care and return the property upon reassignment, transfer, or clearance.",
            AcknowledgementDisclaimerText = StaffAssetValidator.DefaultAcknowledgementStatement,
            Lines = lines
        };

        doc.Sha256Hash = CalculateSha256(JsonSerializer.Serialize(doc));
        return doc;
    }

    public async Task<ServiceResult> ReplaceIssuanceLineAsync(AssetReplaceLineInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetInventoryManage))
            return ServiceResult.Fail("You do not have permission to replace issuance lines.");

        await using var db = await factory.CreateDbContextAsync();
        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
                .ThenInclude(l => l.AssetItem)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == input.AssetIssuanceId);

        if (issuance == null) return ServiceResult.Fail("Issuance not found.");

        var oldLine = issuance.Lines.FirstOrDefault(l => l.AssetIssuanceItemId == input.RejectedOrReturnedIssuanceItemId);
        if (oldLine == null) return ServiceResult.Fail("Original issuance line not found.");

        if (oldLine.Status is not (AssetIssuanceLineStatuses.Rejected or AssetIssuanceLineStatuses.ReturnedForCorrection))
            return ServiceResult.Fail($"Only rejected or returned lines can be replaced (current status: {oldLine.Status}).");

        var replacementAsset = await db.AssetItems
            .Include(a => a.Category)
            .Include(a => a.CurrentCondition)
            .FirstOrDefaultAsync(a => a.AssetItemId == input.ReplacementAssetItemId && a.OwnerOrganisationId == org && a.IsActive);

        if (replacementAsset == null) return ServiceResult.Fail("Replacement asset was not found.");
        if (replacementAsset.Status != AssetInventoryStatuses.Available)
            return ServiceResult.Fail($"Replacement asset '{replacementAsset.AssetCode}' is not Available (current status: {replacementAsset.Status}).");

        // Release old asset reservation back to Available
        oldLine.IsActiveReservation = false;
        oldLine.Status = AssetIssuanceLineStatuses.Closed;
        oldLine.AssetItem.Status = AssetInventoryStatuses.Available;
        oldLine.AssetItem.UpdatedAtUtc = DateTime.UtcNow;

        // Create new line with incremented version requiring fresh staff acknowledgement
        var cond = await db.AssetConditions.FirstOrDefaultAsync(c => c.AssetConditionId == input.ConditionAtIssueId) ?? replacementAsset.CurrentCondition;
        var nowUtc = DateTime.UtcNow;

        var newLine = new AssetIssuanceItem
        {
            IssuanceId = issuance.AssetIssuanceId,
            AssetItemId = replacementAsset.AssetItemId,
            ConditionAtIssueId = cond.AssetConditionId,
            Accessories = input.Accessories?.Trim() ?? replacementAsset.DefaultAccessories,
            Status = input.VerifyHandoverImmediately ? AssetIssuanceLineStatuses.HandedOver : AssetIssuanceLineStatuses.Reserved,
            IsActiveReservation = true,
            ReplacedFromIssuanceItemId = oldLine.AssetIssuanceItemId,
            Version = oldLine.Version + 1,
            ExpectedReturnDate = input.ExpectedReturnDate,
            HandedOverAtUtc = input.VerifyHandoverImmediately ? nowUtc : null,
            HandedOverByUserId = input.VerifyHandoverImmediately ? actor.UserId : null,
            HandoverNotes = input.HandoverNotes?.Trim(),
            AssetCodeSnapshot = replacementAsset.AssetCode,
            SerialNumberSnapshot = replacementAsset.SerialNumber,
            CategoryNameSnapshot = replacementAsset.Category.Name,
            ItemNameSnapshot = replacementAsset.Name,
            BrandSnapshot = replacementAsset.Brand,
            ModelSnapshot = replacementAsset.Model,
            ConditionNameSnapshot = cond.Name,
            IsSecurityItemSnapshot = replacementAsset.Category.IsSecurityItem
        };

        replacementAsset.Status = input.VerifyHandoverImmediately
            ? AssetInventoryStatuses.PendingHandover
            : AssetInventoryStatuses.Reserved;
        replacementAsset.UpdatedAtUtc = nowUtc;

        db.AssetIssuanceItems.Add(newLine);

        issuance.Status = AssetIssuanceStatuses.PendingStaffAcknowledgement;
        issuance.UpdatedAtUtc = nowUtc;

        RecordAssetEvent(db, org, replacementAsset.AssetItemId, "AssetIssuanceItem", newLine.AssetIssuanceItemId,
            "LineReplaced", "Proposed", newLine.Status, actor.UserId,
            new { ReplacedLineId = oldLine.AssetIssuanceItemId, replacementAsset.AssetCode, newLine.Version });

        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CreateReturnAsync(AssetReturnCreateInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(input.OrganisationId);
        if (input.AssetCustodyIds.Count == 0)
            return ServiceResult.Fail("Select at least one active custody item to return.");

        await using var db = await factory.CreateDbContextAsync();
        var individualId = input.IndividualId ?? actor.IndividualId;

        var custodies = await db.AssetCustodies
            .Include(c => c.AssetItem)
            .Where(c => input.AssetCustodyIds.Contains(c.AssetCustodyId) && c.CustodyToUtc == null && c.Status == AssetCustodyStatuses.Active)
            .ToListAsync();

        if (custodies.Count != input.AssetCustodyIds.Count)
            return ServiceResult.Fail("One or more selected custody items are no longer active.");

        // Security check
        if (!isHrOrAdmin && custodies.Any(c => c.IndividualId != actor.IndividualId))
            return ServiceResult.Fail("Staff can only initiate returns for their own custody assets.");

        var refNumber = $"RET-{TodayMaldives.Year}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
        var returnEntity = new AssetReturn
        {
            ReferenceNumber = refNumber,
            OrganisationId = org,
            IndividualId = individualId,
            JobId = custodies.First().JobId,
            StaffTransferId = input.StaffTransferId,
            ReturnType = input.ReturnType,
            RequestedByUserId = actor.UserId,
            ReturnDate = DateTime.UtcNow,
            Status = AssetReturnStatuses.ReturnPending,
            Notes = input.Notes?.Trim()
        };
        db.AssetReturns.Add(returnEntity);
        await db.SaveChangesAsync();

        foreach (var c in custodies)
        {
            c.Status = AssetCustodyStatuses.ReturnPending;
            c.AssetItem.Status = AssetInventoryStatuses.ReturnPending;
            c.AssetItem.UpdatedAtUtc = DateTime.UtcNow;

            var returnItem = new AssetReturnItem
            {
                AssetReturnId = returnEntity.AssetReturnId,
                AssetCustodyId = c.AssetCustodyId,
                AssetItemId = c.AssetItemId,
                Status = AssetReturnStatuses.ReturnPending,
                InspectionResult = AssetInspectionResults.Pending
            };
            db.AssetReturnItems.Add(returnItem);

            RecordAssetEvent(db, org, c.AssetItemId, "AssetReturnItem", returnItem.AssetReturnItemId, "ReturnInitiated", "Active", "ReturnPending", actor.UserId);
        }

        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> InspectReturnAsync(AssetReturnInspectInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetReturnInspect))
            return ServiceResult.Fail("You do not have permission to inspect returns and authorize asset disposition.");

        await using var db = await factory.CreateDbContextAsync();
        var returnEntity = await db.AssetReturns
            .Include(r => r.Items)
                .ThenInclude(i => i.AssetCustody)
            .Include(r => r.Items)
                .ThenInclude(i => i.AssetItem)
            .FirstOrDefaultAsync(r => r.AssetReturnId == input.AssetReturnId);

        if (returnEntity == null) return ServiceResult.Fail("Return transaction not found.");
        if (returnEntity.Status != AssetReturnStatuses.ReturnPending)
            return ServiceResult.Fail($"Return transaction is not pending inspection (current status: {returnEntity.Status}).");

        var itemMap = input.Items.ToDictionary(i => i.AssetReturnItemId);
        var nowUtc = DateTime.UtcNow;

        foreach (var item in returnEntity.Items)
        {
            if (!itemMap.TryGetValue(item.AssetReturnItemId, out var inspectData)) continue;

            item.ConditionAtReturnId = inspectData.ConditionAtReturnId > 0 ? inspectData.ConditionAtReturnId : null;
            item.AccessoryChecklist = inspectData.AccessoryChecklist?.Trim();
            item.MissingAccessories = inspectData.MissingAccessories?.Trim();
            item.DamageOrLossEvidence = inspectData.DamageOrLossEvidence?.Trim();
            item.InspectionResult = inspectData.InspectionResult;
            item.ReceivedByUserId = actor.UserId;
            item.ReceivedAtUtc = nowUtc;
            item.Status = AssetReturnStatuses.Closed;

            // Close active custody
            var custody = item.AssetCustody;
            custody.CustodyToUtc = nowUtc;
            custody.ClosedByUserId = actor.UserId;
            custody.ClosedReason = $"Returned via {returnEntity.ReferenceNumber} ({inspectData.InspectionResult})";
            custody.Status = inspectData.InspectionResult == AssetInspectionResults.LostConfirmed
                ? AssetCustodyStatuses.Lost
                : AssetCustodyStatuses.Returned;

            // Update physical inventory disposition
            var asset = item.AssetItem;
            asset.CurrentConditionId = inspectData.ConditionAtReturnId > 0 ? inspectData.ConditionAtReturnId : asset.CurrentConditionId;
            asset.UpdatedAtUtc = nowUtc;

            string targetInventoryStatus;
            switch (inspectData.InspectionResult)
            {
                case AssetInspectionResults.DamagedMaintenance:
                    targetInventoryStatus = AssetInventoryStatuses.Maintenance;
                    break;
                case AssetInspectionResults.LostConfirmed:
                    targetInventoryStatus = AssetInventoryStatuses.Lost;
                    break;
                case AssetInspectionResults.Disposed:
                    targetInventoryStatus = AssetInventoryStatuses.Disposed;
                    break;
                default:
                    targetInventoryStatus = AssetInventoryStatuses.Available;
                    break;
            }

            asset.Status = targetInventoryStatus;
            item.PostReturnDisposition = targetInventoryStatus;

            RecordAssetEvent(db, org, asset.AssetItemId, "AssetReturnItem", item.AssetReturnItemId, "ReturnInspected", "ReturnPending", targetInventoryStatus, actor.UserId, new { inspectData.InspectionResult, targetInventoryStatus });
        }

        returnEntity.Status = AssetReturnStatuses.Closed;
        returnEntity.ReturnedToUserId = actor.UserId;
        returnEntity.CompletedAtUtc = nowUtc;
        if (!string.IsNullOrWhiteSpace(input.Notes))
            returnEntity.Notes = string.Join("; ", new[] { returnEntity.Notes, input.Notes.Trim() }.Where(x => !string.IsNullOrWhiteSpace(x)));

        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RecordTransferDispositionAsync(AssetTransferDispositionInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetInventoryManage))
            return ServiceResult.Fail("You do not have permission to manage asset transfer dispositions.");

        await using var db = await factory.CreateDbContextAsync();
        var custody = await db.AssetCustodies
            .Include(c => c.AssetItem)
            .FirstOrDefaultAsync(c => c.AssetCustodyId == input.AssetCustodyId && c.CustodyToUtc == null && c.Status == AssetCustodyStatuses.Active);

        if (custody == null) return ServiceResult.Fail("Active asset custody record not found.");

        var nowUtc = DateTime.UtcNow;
        var disp = new AssetTransferDisposition
        {
            StaffTransferId = input.StaffTransferId,
            AssetCustodyId = custody.AssetCustodyId,
            AssetItemId = custody.AssetItemId,
            IndividualId = custody.IndividualId,
            FromJobId = custody.JobId,
            ToJobId = input.ToJobId ?? custody.JobId,
            FromOrganisationId = custody.OrganisationId,
            ToOrganisationId = input.ToOrganisationId > 0 ? input.ToOrganisationId : custody.OrganisationId,
            FromStructureId = custody.StructureId,
            ToStructureId = input.ToStructureId,
            Decision = input.Decision,
            Status = input.ExecuteImmediately ? "Executed" : "Pending",
            DecidedByUserId = actor.UserId,
            ApprovedByUserId = input.ExecuteImmediately ? actor.UserId : null,
            DecidedAtUtc = nowUtc,
            ExecutedAtUtc = input.ExecuteImmediately ? nowUtc : null,
            Remarks = input.Remarks?.Trim()
        };
        db.AssetTransferDispositions.Add(disp);

        if (input.ExecuteImmediately)
        {
            if (input.Decision == AssetTransferDecisions.Retain)
            {
                if (input.ToJobId.HasValue && input.ToJobId.Value > 0)
                    custody.JobId = input.ToJobId.Value;
                if (input.ToOrganisationId > 0)
                    custody.OrganisationId = input.ToOrganisationId;
                if (input.ToStructureId.HasValue && input.ToStructureId.Value > 0)
                    custody.StructureId = input.ToStructureId.Value;
            }
            else if (input.Decision == AssetTransferDecisions.Return)
            {
                // Open standard return
                await CreateReturnAsync(new AssetReturnCreateInput
                {
                    OrganisationId = custody.OrganisationId,
                    IndividualId = custody.IndividualId,
                    StaffTransferId = input.StaffTransferId,
                    ReturnType = AssetReturnTypes.TransferReturn,
                    Notes = $"Transfer return disposition: {input.Remarks}",
                    AssetCustodyIds = [custody.AssetCustodyId]
                });
            }
            else if (input.Decision == AssetTransferDecisions.ChangeOwningDepartment)
            {
                if (input.ToStructureId.HasValue && input.ToStructureId.Value > 0)
                {
                    custody.StructureId = input.ToStructureId.Value;
                    custody.AssetItem.OwnerStructureId = input.ToStructureId.Value;
                }
            }
        }

        RecordAssetEvent(db, org, custody.AssetItemId, "AssetTransferDisposition", disp.AssetTransferDispositionId, "TransferDispositionRecorded", null, input.Decision, actor.UserId, new { input.Decision, input.StaffTransferId });
        await db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<AssetPrintableDocumentDto> GetPrintableDocumentAsync(int assetIssuanceId, string variant = AssetDocumentVariants.StandardHandoverRecord)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var normalizedVariant = variant == AssetDocumentVariants.RestrictedSecurityRecord
            ? AssetDocumentVariants.RestrictedSecurityRecord
            : AssetDocumentVariants.StandardHandoverRecord;

        if (normalizedVariant == AssetDocumentVariants.RestrictedSecurityRecord &&
            !isHrOrAdmin && !await access.HasPermissionAsync(Permissions.AssetExportRestrictedPdf))
        {
            throw new UnauthorizedAccessException("You do not have permission to export the restricted security asset record.");
        }

        var savedDoc = await db.AssetDocuments.AsNoTracking()
            .Where(d => d.AssetIssuanceId == assetIssuanceId && d.DocumentVariant == normalizedVariant)
            .OrderByDescending(d => d.DocumentVersion)
            .ThenByDescending(d => d.GeneratedAtUtc)
            .FirstOrDefaultAsync();

        if (savedDoc != null)
        {
            var deserialized = JsonSerializer.Deserialize<AssetPrintableDocumentDto>(savedDoc.SnapshotJson);
            if (deserialized != null) return deserialized;
        }

        // Live fallback
        var issuance = await db.AssetIssuances
            .Include(i => i.Lines)
                .ThenInclude(l => l.AssetItem)
            .Include(i => i.Lines)
                .ThenInclude(l => l.HandedOverByUser)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Acknowledgements)
            .Include(i => i.Lines)
                .ThenInclude(l => l.HrDecisions)
            .FirstOrDefaultAsync(i => i.AssetIssuanceId == assetIssuanceId);

        if (issuance == null) throw new KeyNotFoundException("Asset issuance not found.");

        return BuildPrintableDto(issuance, normalizedVariant, 1, DateTime.UtcNow);
    }
}
