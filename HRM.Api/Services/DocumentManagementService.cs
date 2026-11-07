using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HRM.Authorization;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Documents;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Api.Services;

public sealed class DocumentManagementService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IDocumentStorageProvider storage,
    IOperationLogService logs,
    IRequestService requests,
    ILogger<DocumentManagementService> logger)
    : DocumentManagementServiceContracts.IOperations
{
    private static int _schemaInitialized;
    private static readonly Regex PlaceholderRegex = new(@"\{\{\s*([^}]+?)\s*\}\}", RegexOptions.Compiled);
    private static DateTime TodayMaldives => DateTime.UtcNow.AddHours(5).Date;

    private async Task<HrmTeContext> OpenDbAsync(int? organisationIdToSeed = null)
    {
        var db = await factory.CreateDbContextAsync();
        await EnsureSchemaAsync(db);
        await EnsureDefaultCataloguesAsync(db, organisationIdToSeed);
        return db;
    }

    private async Task EnsureSchemaAsync(HrmTeContext db)
    {
        if (!db.Database.IsSqlServer())
            return;

        if (Interlocked.Exchange(ref _schemaInitialized, 1) == 1)
            return;

        try
        {
            using var stream = typeof(DocumentManagementService).Assembly
                .GetManifestResourceStream("HRM.Database.Scripts.20261107_DocumentRecordsManagement.sql");
            if (stream is null)
                return;

            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await db.Database.ExecuteSqlRawAsync(sql);
            }
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _schemaInitialized, 0);
            logger.LogWarning(ex, "Could not auto-verify DHR-DMS schema.");
        }
    }

    private static async Task EnsureDefaultCataloguesAsync(HrmTeContext db, int? orgId = null)
    {
        // Seed system-level Retention Policies if none exist
        if (!await db.DmsRetentionPolicies.AnyAsync())
        {
            var policies = new List<DmsRetentionPolicy>
            {
                new()
                {
                    Code = "RET-PERM-RECORD",
                    Name = "Permanent Official Employment & Institutional Record",
                    Description = "Retained permanently as part of official Civil Service & HR institutional archives; never auto-disposed.",
                    RetentionMonths = null,
                    TriggerEvent = "CreationDate",
                    ArchiveAfterMonths = 84,
                    DispositionAction = "PermanentKeep",
                    RequiresApprovalToDispose = true,
                    PolicyReferenceNumber = "DHR-GOV-RET-001",
                    IsActive = true
                },
                new()
                {
                    Code = "RET-STAFF-7YR",
                    Name = "Standard Post-Employment 7-Year Retention",
                    Description = "Retained for 84 months after employment end or document supersession, then eligible for archival review.",
                    RetentionMonths = 84,
                    TriggerEvent = "EmploymentEndDate",
                    ArchiveAfterMonths = 36,
                    DispositionAction = "ReviewRequired",
                    RequiresApprovalToDispose = true,
                    PolicyReferenceNumber = "DHR-GOV-RET-002",
                    IsActive = true
                },
                new()
                {
                    Code = "RET-OPS-3YR",
                    Name = "Operational & Workflow Attachment 3-Year Schedule",
                    Description = "Supporting request attachments and operational records retained for 36 months from creation.",
                    RetentionMonths = 36,
                    TriggerEvent = "CreationDate",
                    ArchiveAfterMonths = 12,
                    DispositionAction = "ArchiveOnly",
                    RequiresApprovalToDispose = true,
                    PolicyReferenceNumber = "DHR-GOV-RET-003",
                    IsActive = true
                }
            };
            db.DmsRetentionPolicies.AddRange(policies);
            await db.SaveChangesAsync();
        }

        // Seed the 12 required Enterprise Document Categories if none exist
        if (!await db.DmsCategories.AnyAsync())
        {
            var categories = new List<DmsCategory>
            {
                new() { Code = "STAFF_PERSONAL", Name = "Staff Personal Documents", Description = "National ID, passport, civil status, police clearance, and emergency records.", TargetDomain = "Staff", DisplayOrder = 1, IsSystemCategory = true },
                new() { Code = "EMPLOYMENT_POSITIONS", Name = "Employment & Position Records", Description = "Appointment letters, job descriptions, contracts, promotions, and salary revisions.", TargetDomain = "Staff", DisplayOrder = 2, IsSystemCategory = true },
                new() { Code = "STAFF_CERTIFICATES", Name = "Staff Qualifications & Certificates", Description = "Academic degrees, professional licences, training certificates, and issued service credentials.", TargetDomain = "Staff", DisplayOrder = 3, IsSystemCategory = true },
                new() { Code = "ORG_POLICIES", Name = "Organisation Policies & Regulations", Description = "HR policies, standing orders, SOPs, circulars, and guidelines.", TargetDomain = "Organisation", DisplayOrder = 4, IsSystemCategory = true },
                new() { Code = "OFFICIAL_CORRESPONDENCE", Name = "Official Correspondence & Letters", Description = "Incoming/outgoing official letters, memos, and verification letters.", TargetDomain = "Organisation", DisplayOrder = 5, IsSystemCategory = true },
                new() { Code = "WORKFLOW_ATTACHMENTS", Name = "Workflow & Request Attachments", Description = "Medical certificates, finance vouchers, and supporting request evidence.", TargetDomain = "Workflow", DisplayOrder = 6, IsSystemCategory = true },
                new() { Code = "TRANSFER_OFFBOARDING", Name = "Transfer & Offboarding Documents", Description = "Internal transfer orders, handover reports, resignation, and exit clearance records.", TargetDomain = "Transfer", DisplayOrder = 7, IsSystemCategory = true },
                new() { Code = "ASSETS_EQUIPMENT", Name = "Assets & Equipment Records", Description = "Asset handover receipts, inspection checklists, damage reports, and service certificates.", TargetDomain = "Asset", DisplayOrder = 8, IsSystemCategory = true },
                new() { Code = "PROJECT_TASK_DOCS", Name = "Project & Task Documentation", Description = "Project charters, deliverables, assignment records, and technical specifications.", TargetDomain = "Project", DisplayOrder = 9, IsSystemCategory = true },
                new() { Code = "EVALUATION_RECORDS", Name = "Performance Evaluation Documents", Description = "Annual appraisal forms, competency assessments, and development plans.", TargetDomain = "Evaluation", DisplayOrder = 10, IsSystemCategory = true },
                new() { Code = "LEGAL_CONTRACTUAL", Name = "Legal & Contractual Documents", Description = "Agreements, MOUs, NDAs, bond agreements, and disciplinary rulings.", TargetDomain = "Legal", DisplayOrder = 11, IsSystemCategory = true },
                new() { Code = "ORG_STRUCTURE_RECORDS", Name = "Organisation Structure Records", Description = "Approved organograms, establishment charters, and department mandates.", TargetDomain = "Organisation", DisplayOrder = 12, IsSystemCategory = true }
            };
            db.DmsCategories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        // Seed standard Document Types if none exist
        if (!await db.DmsDocumentTypes.AnyAsync())
        {
            var cats = await db.DmsCategories.ToDictionaryAsync(c => c.Code, c => c.CategoryId);
            var permRetention = await db.DmsRetentionPolicies.FirstOrDefaultAsync(r => r.Code == "RET-PERM-RECORD");
            var staffRetention = await db.DmsRetentionPolicies.FirstOrDefaultAsync(r => r.Code == "RET-STAFF-7YR");
            var opsRetention = await db.DmsRetentionPolicies.FirstOrDefaultAsync(r => r.Code == "RET-OPS-3YR");

            var types = new List<DmsDocumentType>
            {
                new()
                {
                    CategoryId = cats["STAFF_PERSONAL"],
                    Code = "NATIONAL_ID_PASSPORT",
                    Name = "National Identity Card / Passport",
                    DefaultClassification = "Confidential",
                    DefaultAccessScope = "EmployeeAndHr",
                    PermittedExtensions = ".pdf,.png,.jpg,.jpeg",
                    MaxFileSizeMb = 10,
                    RequiresVerification = true,
                    AllowEmployeeUpload = true,
                    RetentionPolicyId = staffRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["EMPLOYMENT_POSITIONS"],
                    Code = "APPOINTMENT_CONTRACT",
                    Name = "Employment Contract / Appointment Letter",
                    DefaultClassification = "Confidential",
                    DefaultAccessScope = "EmployeeAndHr",
                    PermittedExtensions = ".pdf,.docx",
                    MaxFileSizeMb = 15,
                    RequiresApproval = true,
                    RequiresSignature = true,
                    IsOfficialRecordWhenApproved = true,
                    AllowEmployeeUpload = false,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["STAFF_CERTIFICATES"],
                    Code = "ACADEMIC_QUALIFICATION",
                    Name = "Academic Qualification / Degree Certificate",
                    DefaultClassification = "Internal",
                    DefaultAccessScope = "EmployeeAndHr",
                    PermittedExtensions = ".pdf,.png,.jpg,.jpeg",
                    MaxFileSizeMb = 15,
                    RequiresVerification = true,
                    AllowEmployeeUpload = true,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["STAFF_CERTIFICATES"],
                    Code = "WORK_EXPERIENCE_RECORD",
                    Name = "Prior Work Experience / Service Certificate",
                    DefaultClassification = "Internal",
                    DefaultAccessScope = "EmployeeAndHr",
                    PermittedExtensions = ".pdf,.png,.jpg,.jpeg",
                    MaxFileSizeMb = 15,
                    RequiresVerification = true,
                    AllowEmployeeUpload = true,
                    AllowEmployeeRequest = true,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["ORG_POLICIES"],
                    Code = "HR_POLICY_CIRCULAR",
                    Name = "HR Policy / Circular / Standing Guideline",
                    DefaultClassification = "Public",
                    DefaultAccessScope = "PublicOrg",
                    PermittedExtensions = ".pdf,.docx",
                    MaxFileSizeMb = 25,
                    RequiresApproval = true,
                    RequiresSignature = true,
                    IsOfficialRecordWhenApproved = true,
                    AllowEmployeeUpload = false,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["OFFICIAL_CORRESPONDENCE"],
                    Code = "EMPLOYMENT_VERIFICATION_LETTER",
                    Name = "Official Employment Verification Letter",
                    DefaultClassification = "Confidential",
                    DefaultAccessScope = "EmployeeAndHr",
                    PermittedExtensions = ".pdf",
                    MaxFileSizeMb = 10,
                    RequiresApproval = true,
                    RequiresSignature = true,
                    IsOfficialRecordWhenApproved = true,
                    AllowEmployeeUpload = false,
                    AllowEmployeeRequest = true,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["TRANSFER_OFFBOARDING"],
                    Code = "TRANSFER_CLEARANCE_ORDER",
                    Name = "Staff Transfer & Handover Order",
                    DefaultClassification = "Confidential",
                    DefaultAccessScope = "SupervisorAndHr",
                    PermittedExtensions = ".pdf,.docx",
                    MaxFileSizeMb = 15,
                    RequiresApproval = true,
                    IsOfficialRecordWhenApproved = true,
                    AllowEmployeeUpload = false,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["ASSETS_EQUIPMENT"],
                    Code = "ASSET_CUSTODY_CLEARANCE",
                    Name = "Asset Issue, Custody & Clearance Record",
                    DefaultClassification = "Internal",
                    DefaultAccessScope = "EmployeeAndHr",
                    PermittedExtensions = ".pdf,.png,.jpg,.jpeg",
                    MaxFileSizeMb = 15,
                    RequiresVerification = true,
                    AllowEmployeeUpload = true,
                    RetentionPolicyId = opsRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["EVALUATION_RECORDS"],
                    Code = "PERFORMANCE_APPRAISAL_RECORD",
                    Name = "Authorised Performance Evaluation Record",
                    DefaultClassification = "Restricted",
                    DefaultAccessScope = "SupervisorAndHr",
                    PermittedExtensions = ".pdf",
                    MaxFileSizeMb = 15,
                    RequiresApproval = true,
                    IsOfficialRecordWhenApproved = true,
                    AllowEmployeeUpload = false,
                    RetentionPolicyId = staffRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["LEGAL_CONTRACTUAL"],
                    Code = "LEGAL_AGREEMENT_MOU",
                    Name = "Institutional Agreement / Legal Contract",
                    DefaultClassification = "Restricted",
                    DefaultAccessScope = "HrOnly",
                    PermittedExtensions = ".pdf,.docx",
                    MaxFileSizeMb = 25,
                    RequiresApproval = true,
                    RequiresSignature = true,
                    IsOfficialRecordWhenApproved = true,
                    AllowEmployeeUpload = false,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["ORG_STRUCTURE_RECORDS"],
                    Code = "APPROVED_ORGANOGRAM_CHARTER",
                    Name = "Organisation Structure Approval & Mandate",
                    DefaultClassification = "Public",
                    DefaultAccessScope = "PublicOrg",
                    PermittedExtensions = ".pdf,.xlsx,.docx",
                    MaxFileSizeMb = 25,
                    RequiresApproval = true,
                    IsOfficialRecordWhenApproved = true,
                    AllowEmployeeUpload = false,
                    RetentionPolicyId = permRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["PROJECT_TASK_DOCS"],
                    Code = "PROJECT_DELIVERABLE_SPEC",
                    Name = "Project Charter / Task Deliverable",
                    DefaultClassification = "Internal",
                    DefaultAccessScope = "DepartmentAndHr",
                    PermittedExtensions = ".pdf,.docx,.xlsx,.png,.jpg",
                    MaxFileSizeMb = 20,
                    AllowEmployeeUpload = true,
                    RetentionPolicyId = opsRetention?.RetentionPolicyId,
                    IsSystemType = true
                },
                new()
                {
                    CategoryId = cats["WORKFLOW_ATTACHMENTS"],
                    Code = "SUPPORTING_WORKFLOW_EVIDENCE",
                    Name = "Workflow Supporting Attachment",
                    DefaultClassification = "Confidential",
                    DefaultAccessScope = "EmployeeAndHr",
                    PermittedExtensions = ".pdf,.png,.jpg,.jpeg",
                    MaxFileSizeMb = 10,
                    AllowEmployeeUpload = true,
                    RetentionPolicyId = opsRetention?.RetentionPolicyId,
                    IsSystemType = true
                }
            };
            db.DmsDocumentTypes.AddRange(types);
            await db.SaveChangesAsync();
        }

        // Seed Official Document Templates if none exist
        if (!await db.DmsDocumentTemplates.AnyAsync())
        {
            var corrCat = await db.DmsCategories.FirstOrDefaultAsync(c => c.Code == "OFFICIAL_CORRESPONDENCE");
            var certCat = await db.DmsCategories.FirstOrDefaultAsync(c => c.Code == "STAFF_CERTIFICATES");
            var verType = await db.DmsDocumentTypes.FirstOrDefaultAsync(t => t.Code == "EMPLOYMENT_VERIFICATION_LETTER");
            var expType = await db.DmsDocumentTypes.FirstOrDefaultAsync(t => t.Code == "WORK_EXPERIENCE_RECORD");

            var templates = new List<DmsDocumentTemplate>
            {
                new()
                {
                    CategoryId = corrCat?.CategoryId,
                    DmsDocumentTypeId = verType?.DmsDocumentTypeId,
                    Code = "TPL-EMP-VERIFY",
                    Name = "Employment Verification Letter",
                    Description = "Standard official confirmation of current employment status, designation, and tenure.",
                    VersionNumber = 1,
                    DocumentTitlePattern = "Employment Verification Letter - {{EmployeeName}}",
                    HeaderText = "OFFICIAL EMPLOYMENT VERIFICATION",
                    BodyTemplate = "This is to officially certify that {{EmployeeName}} (National ID: {{NationalId}}, Staff No: {{StaffNumber}}) is employed at {{OrganisationName}} since {{EmploymentStartDate}}.\n\nCurrently, {{EmployeeName}} holds the designation of {{CurrentPosition}} within {{DepartmentName}}. This official verification is issued upon the employee's request for the purpose of {{Purpose}} ({{AddressedTo}}).",
                    FooterText = "This document is an official digital record generated from verified HRM data. Verify authenticity using reference {{DocumentNumber}}.",
                    SignatoryName = "Director of Human Resources",
                    SignatoryTitle = "Human Resources Division",
                    IncludeOrganisationLetterhead = true,
                    IncludePositionHistoryTable = false,
                    IncludeQrVerification = true,
                    IsEmployeeRequestable = true,
                    IsActive = true
                },
                new()
                {
                    CategoryId = certCat?.CategoryId,
                    DmsDocumentTypeId = expType?.DmsDocumentTypeId,
                    Code = "TPL-SERVICE-CERT",
                    Name = "Comprehensive Service & Position History Record",
                    Description = "Official service record detailing verified employment dates and chronological position progression.",
                    VersionNumber = 1,
                    DocumentTitlePattern = "Official Service Record - {{EmployeeName}}",
                    HeaderText = "RECORD OF SERVICE & POSITION HISTORY",
                    BodyTemplate = "This is to certify that {{EmployeeName}} (National ID: {{NationalId}}, Staff No: {{StaffNumber}}) has served at {{OrganisationName}} commencing on {{EmploymentStartDate}} ({{ServiceDuration}}).\n\nThe chronological record of verified positions held at {{OrganisationName}} is set forth below for {{AddressedTo}} ({{Purpose}}).",
                    FooterText = "Issued as an immutable official record under the Digital HR Document & Records Management System.",
                    SignatoryName = "Head of Human Resources",
                    SignatoryTitle = "Human Resources & Administration",
                    IncludeOrganisationLetterhead = true,
                    IncludePositionHistoryTable = true,
                    IncludeQrVerification = true,
                    IsEmployeeRequestable = true,
                    IsActive = true
                },
                new()
                {
                    CategoryId = corrCat?.CategoryId,
                    DmsDocumentTypeId = verType?.DmsDocumentTypeId,
                    Code = "TPL-NOC-LETTER",
                    Name = "No Objection Certificate (NOC)",
                    Description = "Official HR No Objection Certificate for visa, embassy, or academic enrolment.",
                    VersionNumber = 1,
                    DocumentTitlePattern = "No Objection Certificate - {{EmployeeName}}",
                    HeaderText = "NO OBJECTION CERTIFICATE",
                    BodyTemplate = "This is to confirm that {{OrganisationName}} has no objection to {{EmployeeName}} (National ID: {{NationalId}}, Staff No: {{StaffNumber}}), currently serving as {{CurrentPosition}} in {{DepartmentName}}, proceeding with {{Purpose}}.\n\nThis letter is issued on {{IssueDate}} and addressed to {{AddressedTo}}.",
                    FooterText = "Official correspondence subject to organisation verification.",
                    SignatoryName = "Director of Human Resources",
                    SignatoryTitle = "Human Resources Division",
                    IncludeOrganisationLetterhead = true,
                    IncludePositionHistoryTable = false,
                    IncludeQrVerification = true,
                    IsEmployeeRequestable = true,
                    IsActive = true
                }
            };
            db.DmsDocumentTemplates.AddRange(templates);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<int> EnsureDocumentRequestTypeAsync(HrmTeContext db)
    {
        var reqType = await db.RequestTypes
            .FirstOrDefaultAsync(t => t.TypeName == DocumentManagementServiceContracts.RequestTypeName
                                   || t.RequestTypeId == SharedConfig.RequestTypes.DOCUMENT_MANAGEMENT_REQUEST);

        if (reqType != null && reqType.SequenceNumberTypeId.HasValue)
        {
            return reqType.RequestTypeId;
        }

        var seqType = await db.SequenceNumberTypes
            .FirstOrDefaultAsync(s => s.TypeName == "Official Document Requests" || s.Prefix == "DMR");

        if (seqType == null)
        {
            seqType = new SequenceNumberType
            {
                TypeName = "Official Document Requests",
                Prefix = "DMR",
                FormatString = "{PREFIX}-{YEAR}-{NUMBER}",
                IsResetAnnually = false,
                StartingNumber = 1,
                NextNumber = 1
            };
            db.SequenceNumberTypes.Add(seqType);
            await db.SaveChangesAsync();
        }
        else if (!seqType.NextNumber.HasValue)
        {
            seqType.NextNumber = 1;
            await db.SaveChangesAsync();
        }

        if (reqType == null)
        {
            reqType = new RequestType
            {
                TypeName = DocumentManagementServiceContracts.RequestTypeName,
                SequenceNumberTypeId = seqType.SequenceNumberTypeId,
                ViewUrl = DocumentManagementServiceContracts.AdminUrl,
                IsSystemType = true
            };
            if (!db.Database.IsSqlServer())
            {
                reqType.RequestTypeId = SharedConfig.RequestTypes.DOCUMENT_MANAGEMENT_REQUEST;
            }
            db.RequestTypes.Add(reqType);
            await db.SaveChangesAsync();
        }
        else if (!reqType.SequenceNumberTypeId.HasValue)
        {
            reqType.SequenceNumberTypeId = seqType.SequenceNumberTypeId;
            await db.SaveChangesAsync();
        }

        return reqType.RequestTypeId;
    }

    private async Task<(UserContextDto Actor, int OrgId, bool IsHrOrAdmin, bool IsSupervisor)> ScopeAsync(int? selectedOrgId = null)
    {
        var actor = await access.RequireContextAsync();
        var currentOrg = await access.GetCurrentOrganisationIdAsync();
        var isSuper = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var org = isSuper && selectedOrgId is > 0 ? selectedOrgId.Value : currentOrg;
        if (org <= 0)
            throw new UnauthorizedAccessException("Please select an active organisation context.");

        if (!isSuper && !await access.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("You do not have access to this organisation.");

        var isHrOrAdmin = isSuper || actor.IsHRStaff || actor.IsHRManager || actor.IsHRHead ||
                          actor.IsOrganisationAdmin || actor.IsHeadOfOrganisation ||
                          await access.HasPermissionAsync("Admin") ||
                          await access.HasPermissionAsync(Permissions.DocumentApprove) ||
                          await access.HasPermissionAsync(Permissions.DocumentConfigure);

        var isSupervisor = isHrOrAdmin || actor.IsSupervisor || await access.HasPermissionAsync(Permissions.DocumentReview);

        return (actor, org, isHrOrAdmin, isSupervisor);
    }

    private static string FullName(Individual? i) =>
        i is null
            ? "Unknown"
            : string.Join(" ", new[] { i.FirstNameEnglish, i.MiddleNameEnglish, i.LastNameEnglish }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

    /// <summary>
    /// Evaluates the effective server-side permission matrix for a specific DmsDocument and UserContext.
    /// Enforces Requirement 7: A document linked to multiple records must not automatically become visible
    /// to every viewer of those records; the document's own effective access policy is strictly applied.
    /// </summary>
    private static DmsEffectivePermissionsDto EvaluateEffectivePermissions(
        DmsDocument doc,
        UserContextDto actor,
        bool isHrOrAdmin,
        bool isSupervisor)
    {
        var perms = new DmsEffectivePermissionsDto();

        // 1. Strict tenant isolation
        if (!actor.IsSuperAdministrator && doc.OrganisationId != actor.UserOrganisationId &&
            !actor.OrganisationAccess.Any(o => o.BusinessEntityID == doc.OrganisationId))
        {
            return perms;
        }

        // 2. Check explicit Deny ACEs first
        var activeAces = doc.Permissions
            .Where(p => p.IsActive && (!p.ExpiresAtUtc.HasValue || p.ExpiresAtUtc.Value > DateTime.UtcNow))
            .ToList();

        bool MatchesPrincipal(DmsDocumentPermission ace) =>
            (ace.PrincipalType.Equals("User", StringComparison.OrdinalIgnoreCase) && ace.PrincipalId == actor.UserId) ||
            (ace.PrincipalType.Equals("Individual", StringComparison.OrdinalIgnoreCase) && ace.PrincipalId == actor.IndividualId) ||
            (ace.PrincipalType.Equals("Department", StringComparison.OrdinalIgnoreCase) && ace.PrincipalId.HasValue && actor.ActiveJob?.OrganisationStructureId == ace.PrincipalId.Value) ||
            (ace.PrincipalType.Equals("UserGroup", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ace.PrincipalName) && actor.HasUserGroup(ace.PrincipalName));

        if (!actor.IsSuperAdministrator && activeAces.Any(a => a.PermissionLevel.Equals("Deny", StringComparison.OrdinalIgnoreCase) && MatchesPrincipal(a)))
        {
            return perms;
        }

        var isOwnerOrSubject = doc.CreatedByUserId == actor.UserId ||
                               (doc.SubjectIndividualId.HasValue && doc.SubjectIndividualId.Value == actor.IndividualId);

        var isSameDepartment = doc.DepartmentStructureId.HasValue &&
                               actor.ActiveJob?.OrganisationStructureId == doc.DepartmentStructureId.Value;

        // 3. Evaluate Classification ceiling
        if (doc.Classification.Equals("Secret", StringComparison.OrdinalIgnoreCase) ||
            doc.Classification.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
        {
            if (!isHrOrAdmin &&
                !(isSupervisor && doc.AccessScope.Equals("SupervisorAndHr", StringComparison.OrdinalIgnoreCase)) &&
                !(isOwnerOrSubject && doc.AccessScope.Equals("EmployeeAndHr", StringComparison.OrdinalIgnoreCase)) &&
                !activeAces.Any(MatchesPrincipal))
            {
                return perms;
            }
        }

        // 4. Evaluate Document's AccessScope
        var scopeAllowed = doc.AccessScope switch
        {
            "PublicOrg" => true,
            "DepartmentAndHr" => isHrOrAdmin || isSameDepartment || isOwnerOrSubject,
            "EmployeeAndHr" => isHrOrAdmin || isOwnerOrSubject,
            "SupervisorAndHr" => isHrOrAdmin || isSupervisor || isOwnerOrSubject,
            "HrOnly" => isHrOrAdmin,
            "ExecutiveOnly" => actor.IsSuperAdministrator || actor.IsHeadOfOrganisation || actor.IsHRHead || actor.IsOrganisationAdmin,
            "RestrictedCustom" => isHrOrAdmin || activeAces.Any(MatchesPrincipal),
            _ => isHrOrAdmin || isOwnerOrSubject
        };

        var explicitGrantLevels = activeAces
            .Where(MatchesPrincipal)
            .Select(a => a.PermissionLevel)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!scopeAllowed && explicitGrantLevels.Count == 0)
        {
            return perms;
        }

        var isDisposed = doc.Status.Equals("Disposed", StringComparison.OrdinalIgnoreCase) || doc.IsDeleted;

        perms.CanViewMetadata = true;
        perms.CanPreview = !isDisposed && (scopeAllowed || explicitGrantLevels.Overlaps(["Preview", "Download", "Contribute", "Approve", "Manage"]));
        perms.CanDownload = !isDisposed && (scopeAllowed || explicitGrantLevels.Overlaps(["Download", "Contribute", "Approve", "Manage"]));
        perms.CanUpload = isHrOrAdmin || actor.HasPermission(Permissions.DocumentUpload) || isOwnerOrSubject;

        var canMutateContent = !doc.IsImmutable && !doc.IsOfficialRecord && !doc.HasActiveLegalHold && !isDisposed &&
                               (!doc.IsLockedForEdit || doc.CheckedOutByUserId == actor.UserId || isHrOrAdmin);

        perms.CanModifyMetadata = canMutateContent && (isHrOrAdmin || doc.CreatedByUserId == actor.UserId || explicitGrantLevels.Overlaps(["Contribute", "Manage"]) || actor.HasPermission(Permissions.DocumentModifyMetadata));
        perms.CanAddVersion = canMutateContent && (isHrOrAdmin || isOwnerOrSubject || explicitGrantLevels.Overlaps(["Contribute", "Manage"]) || actor.HasPermission(Permissions.DocumentAddVersion));
        perms.CanSubmitForApproval = !isDisposed && (isHrOrAdmin || isOwnerOrSubject || actor.HasPermission(Permissions.DocumentSubmitApproval));
        perms.CanReview = !isDisposed && (isHrOrAdmin || isSupervisor || actor.HasPermission(Permissions.DocumentReview));
        perms.CanApprove = !isDisposed && (isHrOrAdmin || explicitGrantLevels.Overlaps(["Approve", "Manage"]) || actor.HasPermission(Permissions.DocumentApprove));
        perms.CanSign = !isDisposed && (isHrOrAdmin || isOwnerOrSubject || actor.HasPermission(Permissions.DocumentSign));
        perms.CanShare = !isDisposed && (isHrOrAdmin || actor.HasPermission(Permissions.DocumentShare));
        perms.CanArchive = !isDisposed && (isHrOrAdmin || actor.HasPermission(Permissions.DocumentArchive));
        perms.CanDispose = !isDisposed && !doc.HasActiveLegalHold && !doc.IsImmutable && (isHrOrAdmin || actor.HasPermission(Permissions.DocumentDispose));
        perms.CanManageLegalHold = isHrOrAdmin || actor.HasPermission(Permissions.DocumentConfigure);

        return perms;
    }

    // =========================================================================
    // 1. DASHBOARD & CONFIGURATION CATALOGUES
    // =========================================================================

    public async Task<DmsDashboardSummaryDto> GetDashboardAsync(int? organisationId = null)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync(organisationId);
        await using var db = await OpenDbAsync(org);

        var orgName = (await db.Organisations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.BusinessEntityID == org))?.OrganisationName ?? $"Organisation #{org}";

        var docs = await db.DmsDocuments
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.DocumentType)
            .Include(d => d.SubjectIndividual).ThenInclude(i => i!.Staff)
            .Include(d => d.Versions)
            .Include(d => d.Links)
            .Include(d => d.Permissions)
            .Include(d => d.Signatures)
            .Where(d => d.OrganisationId == org && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync();

        var visibleDocs = docs
            .Where(d => EvaluateEffectivePermissions(d, actor, isHrOrAdmin, isSupervisor).CanViewMetadata)
            .ToList();

        var pendingRequests = await db.DmsDocumentRequests
            .AsNoTracking()
            .CountAsync(r => r.OrganisationId == org &&
                             (r.Status == "Submitted" || r.Status == "UnderReview") &&
                             (isHrOrAdmin || r.ApplicantIndividualId == actor.IndividualId));

        var categories = await GetCategoriesAsync(org);
        var docTypes = await GetDocumentTypesAsync(org);
        var retentions = await GetRetentionPoliciesAsync(org);
        var templates = await GetTemplatesAsync(org, activeOnly: false);

        var totalBytes = visibleDocs
            .SelectMany(d => d.Versions)
            .Sum(v => v.FileSizeBytes);

        return new DmsDashboardSummaryDto
        {
            OrganisationId = org,
            OrganisationName = orgName,
            TotalDocuments = visibleDocs.Count,
            StaffDocumentsCount = visibleDocs.Count(d => d.SubjectIndividualId.HasValue || d.Category.TargetDomain == "Staff"),
            OrganisationLibraryCount = visibleDocs.Count(d => d.Category.TargetDomain == "Organisation" || d.Category.TargetDomain == "Legal"),
            OfficialRecordsCount = visibleDocs.Count(d => d.IsOfficialRecord),
            PendingVerificationCount = visibleDocs.Count(d => d.Status == "PendingVerification" || (!d.IsVerified && d.DocumentType.RequiresVerification)),
            PendingApprovalCount = visibleDocs.Count(d => d.Status == "PendingApproval"),
            PendingDocumentRequestsCount = pendingRequests,
            ActiveLegalHoldsCount = visibleDocs.Count(d => d.HasActiveLegalHold),
            ArchivedDocumentsCount = visibleDocs.Count(d => d.Status == "Archived"),
            RetentionDueCount = visibleDocs.Count(d => d.RetentionDueDateUtc.HasValue && d.RetentionDueDateUtc.Value <= DateTime.UtcNow.AddDays(30) && d.Status != "Archived" && d.Status != "Disposed"),
            TotalStorageBytes = totalBytes,
            Categories = categories,
            DocumentTypes = docTypes,
            RetentionPolicies = retentions,
            Templates = templates,
            RecentDocuments = visibleDocs.Take(15).Select(d => MapToSummaryDto(d, orgName)).ToList()
        };
    }

    public async Task<List<DmsCategoryDto>> GetCategoriesAsync(int? organisationId = null, string? targetDomain = null)
    {
        var (_, org, _, _) = await ScopeAsync(organisationId);
        await using var db = await OpenDbAsync(org);

        var query = db.DmsCategories
            .AsNoTracking()
            .Where(c => c.IsActive && (c.OrganisationId == null || c.OrganisationId == org));

        if (!string.IsNullOrWhiteSpace(targetDomain))
        {
            query = query.Where(c => c.TargetDomain == targetDomain);
        }

        var rows = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new DmsCategoryDto
            {
                CategoryId = c.CategoryId,
                OrganisationId = c.OrganisationId,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                TargetDomain = c.TargetDomain,
                DisplayOrder = c.DisplayOrder,
                IsSystemCategory = c.IsSystemCategory,
                IsActive = c.IsActive,
                DocumentTypesCount = c.DocumentTypes.Count(t => t.IsActive && (t.OrganisationId == null || t.OrganisationId == org)),
                DocumentsCount = c.Documents.Count(d => d.OrganisationId == org && !d.IsDeleted)
            })
            .ToListAsync();

        return rows;
    }

    public async Task<List<DmsDocumentTypeDto>> GetDocumentTypesAsync(int? organisationId = null, int? categoryId = null)
    {
        var (_, org, _, _) = await ScopeAsync(organisationId);
        await using var db = await OpenDbAsync(org);

        var query = db.DmsDocumentTypes
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.RetentionPolicy)
            .Where(t => t.OrganisationId == null || t.OrganisationId == org);

        if (categoryId is > 0)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        var rows = await query
            .OrderBy(t => t.Category.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return rows.Select(t => new DmsDocumentTypeDto
        {
            DmsDocumentTypeId = t.DmsDocumentTypeId,
            CategoryId = t.CategoryId,
            CategoryName = t.Category.Name,
            TargetDomain = t.Category.TargetDomain,
            LegacyDocumentTypeId = t.LegacyDocumentTypeId,
            OrganisationId = t.OrganisationId,
            Code = t.Code,
            Name = t.Name,
            NameDhivehi = t.NameDhivehi,
            Description = t.Description,
            DefaultClassification = t.DefaultClassification,
            DefaultAccessScope = t.DefaultAccessScope,
            PermittedExtensions = t.PermittedExtensions,
            MaxFileSizeMb = t.MaxFileSizeMb,
            RequiredMetadataJson = t.RequiredMetadataJson,
            RequiresApproval = t.RequiresApproval,
            RequiresVerification = t.RequiresVerification,
            RequiresSignature = t.RequiresSignature,
            IsOfficialRecordWhenApproved = t.IsOfficialRecordWhenApproved,
            AllowEmployeeUpload = t.AllowEmployeeUpload,
            AllowEmployeeRequest = t.AllowEmployeeRequest,
            RetentionPolicyId = t.RetentionPolicyId,
            RetentionPolicyName = t.RetentionPolicy?.Name,
            IsSystemType = t.IsSystemType,
            IsActive = t.IsActive
        }).ToList();
    }

    public async Task<ServiceResult> SaveDocumentTypeAsync(DmsDocumentTypeDto input)
    {
        var (_, org, isHrOrAdmin, _) = await ScopeAsync(input.OrganisationId);
        if (!isHrOrAdmin)
            return ServiceResult.Fail("Only HR Administrators can configure document types and policies.");

        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            return ServiceResult.Fail("Document Type Code and Name are required.");

        if (input.CategoryId <= 0)
            return ServiceResult.Fail("Please select a valid Document Category.");

        await using var db = await OpenDbAsync(org);

        var code = input.Code.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        DmsDocumentType entity;
        if (input.DmsDocumentTypeId > 0)
        {
            entity = await db.DmsDocumentTypes
                .FirstOrDefaultAsync(t => t.DmsDocumentTypeId == input.DmsDocumentTypeId &&
                                          (t.OrganisationId == org || t.OrganisationId == null))
                ?? throw new InvalidOperationException("Document type not found.");
        }
        else
        {
            if (await db.DmsDocumentTypes.AnyAsync(t => (t.OrganisationId == org || t.OrganisationId == null) && t.Code == code))
                return ServiceResult.Fail($"A document type with code '{code}' already exists.");

            entity = new DmsDocumentType
            {
                OrganisationId = org,
                Code = code,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.DmsDocumentTypes.Add(entity);
        }

        entity.CategoryId = input.CategoryId;
        entity.Name = name;
        entity.NameDhivehi = input.NameDhivehi?.Trim();
        entity.Description = input.Description?.Trim();
        entity.DefaultClassification = string.IsNullOrWhiteSpace(input.DefaultClassification) ? "Internal" : input.DefaultClassification;
        entity.DefaultAccessScope = string.IsNullOrWhiteSpace(input.DefaultAccessScope) ? "EmployeeAndHr" : input.DefaultAccessScope;
        entity.PermittedExtensions = string.IsNullOrWhiteSpace(input.PermittedExtensions) ? ".pdf,.png,.jpg,.jpeg" : input.PermittedExtensions.Trim();
        entity.MaxFileSizeMb = Math.Clamp(input.MaxFileSizeMb, 1, 100);
        entity.RequiredMetadataJson = input.RequiredMetadataJson;
        entity.RequiresApproval = input.RequiresApproval;
        entity.RequiresVerification = input.RequiresVerification;
        entity.RequiresSignature = input.RequiresSignature;
        entity.IsOfficialRecordWhenApproved = input.IsOfficialRecordWhenApproved;
        entity.AllowEmployeeUpload = input.AllowEmployeeUpload;
        entity.AllowEmployeeRequest = input.AllowEmployeeRequest;
        entity.RetentionPolicyId = input.RetentionPolicyId;
        entity.IsActive = input.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        // Also synchronise with legacy dbo.DocumentTypes if not yet linked
        if (!entity.LegacyDocumentTypeId.HasValue)
        {
            var legacy = await db.DocumentTypes.FirstOrDefaultAsync(lt => (lt.OrganisationId == org || lt.OrganisationId == null) && lt.TypeName == name);
            if (legacy is null)
            {
                legacy = new DocumentType
                {
                    TypeName = name,
                    TypeNameDhivehi = entity.NameDhivehi,
                    Description = entity.Description,
                    OrganisationId = org,
                    IsActive = entity.IsActive,
                    IsSystemType = entity.IsSystemType
                };
                db.DocumentTypes.Add(legacy);
                await db.SaveChangesAsync();
            }
            entity.LegacyDocumentTypeId = legacy.DocumentTypeId;
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Document type policy saved.");
    }

    public async Task<List<DmsRetentionPolicyDto>> GetRetentionPoliciesAsync(int? organisationId = null)
    {
        var (_, org, _, _) = await ScopeAsync(organisationId);
        await using var db = await OpenDbAsync(org);

        var rows = await db.DmsRetentionPolicies
            .AsNoTracking()
            .Where(r => r.OrganisationId == null || r.OrganisationId == org)
            .OrderBy(r => r.Code)
            .ToListAsync();

        return rows.Select(r => new DmsRetentionPolicyDto
        {
            RetentionPolicyId = r.RetentionPolicyId,
            OrganisationId = r.OrganisationId,
            Code = r.Code,
            Name = r.Name,
            Description = r.Description,
            RetentionMonths = r.RetentionMonths,
            TriggerEvent = r.TriggerEvent,
            ArchiveAfterMonths = r.ArchiveAfterMonths,
            DispositionAction = r.DispositionAction,
            RequiresApprovalToDispose = r.RequiresApprovalToDispose,
            PolicyReferenceNumber = r.PolicyReferenceNumber,
            IsActive = r.IsActive
        }).ToList();
    }

    public async Task<ServiceResult> SaveRetentionPolicyAsync(DmsRetentionPolicyDto input)
    {
        var (actor, org, isHrOrAdmin, _) = await ScopeAsync(input.OrganisationId);
        if (!isHrOrAdmin)
            return ServiceResult.Fail("Only HR Administrators can configure retention schedules.");

        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name))
            return ServiceResult.Fail("Retention policy code and name are required.");

        if (string.IsNullOrWhiteSpace(input.PolicyReferenceNumber))
            return ServiceResult.Fail("An approved organisation Policy Reference Number is required to establish or modify a retention schedule.");

        await using var db = await OpenDbAsync(org);

        DmsRetentionPolicy entity;
        if (input.RetentionPolicyId > 0)
        {
            entity = await db.DmsRetentionPolicies
                .FirstOrDefaultAsync(r => r.RetentionPolicyId == input.RetentionPolicyId &&
                                          (r.OrganisationId == org || r.OrganisationId == null))
                ?? throw new InvalidOperationException("Retention policy not found.");
        }
        else
        {
            entity = new DmsRetentionPolicy
            {
                OrganisationId = org,
                Code = input.Code.Trim().ToUpperInvariant(),
                CreatedByUserId = actor.UserId,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.DmsRetentionPolicies.Add(entity);
        }

        entity.Name = input.Name.Trim();
        entity.Description = input.Description?.Trim();
        entity.RetentionMonths = input.RetentionMonths;
        entity.TriggerEvent = string.IsNullOrWhiteSpace(input.TriggerEvent) ? "CreationDate" : input.TriggerEvent;
        entity.ArchiveAfterMonths = input.ArchiveAfterMonths;
        entity.DispositionAction = string.IsNullOrWhiteSpace(input.DispositionAction) ? "PermanentKeep" : input.DispositionAction;
        entity.RequiresApprovalToDispose = input.RequiresApprovalToDispose;
        entity.PolicyReferenceNumber = input.PolicyReferenceNumber.Trim();
        entity.IsActive = input.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Retention schedule saved.");
    }

    // =========================================================================
    // 2. CENTRAL REPOSITORY SEARCH, UPLOAD, VERSIONING, DOWNLOAD & PREVIEW
    // =========================================================================

    public async Task<List<DmsDocumentSummaryDto>> SearchDocumentsAsync(DmsDocumentSearchFilterDto filter)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync(filter.OrganisationId);
        await using var db = await OpenDbAsync(org);

        var orgName = (await db.Organisations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.BusinessEntityID == org))?.OrganisationName ?? $"Organisation #{org}";

        var query = db.DmsDocuments
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.DocumentType)
            .Include(d => d.SubjectIndividual).ThenInclude(i => i!.Staff)
            .Include(d => d.Versions)
            .Include(d => d.Links)
            .Include(d => d.Permissions)
            .Include(d => d.Signatures)
            .Where(d => d.OrganisationId == org && !d.IsDeleted);

        if (!filter.IncludeArchived && string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(d => d.Status != "Archived" && d.Status != "Disposed");
        }

        if (filter.CategoryId is > 0)
            query = query.Where(d => d.CategoryId == filter.CategoryId.Value);

        if (filter.DmsDocumentTypeId is > 0)
            query = query.Where(d => d.DmsDocumentTypeId == filter.DmsDocumentTypeId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TargetDomain))
            query = query.Where(d => d.Category.TargetDomain == filter.TargetDomain);

        if (filter.SubjectIndividualId is > 0)
        {
            var staffId = filter.SubjectIndividualId.Value;
            query = query.Where(d => d.SubjectIndividualId == staffId ||
                                     d.Links.Any(l => l.IsActive && l.TargetEntityType == "Staff" && l.TargetEntityId == staffId));
        }

        if (filter.DepartmentStructureId is > 0)
            query = query.Where(d => d.DepartmentStructureId == filter.DepartmentStructureId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Classification))
            query = query.Where(d => d.Classification == filter.Classification);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(d => d.Status == filter.Status);

        if (filter.OnlyOfficialRecords == true)
            query = query.Where(d => d.IsOfficialRecord);

        if (filter.OnlyLegalHold == true)
            query = query.Where(d => d.HasActiveLegalHold);

        if (!string.IsNullOrWhiteSpace(filter.LinkedEntityType) && filter.LinkedEntityId is > 0)
        {
            var tType = filter.LinkedEntityType.Trim();
            var tId = filter.LinkedEntityId.Value;
            query = query.Where(d => d.Links.Any(l => l.IsActive && l.TargetEntityType == tType && l.TargetEntityId == tId));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var s = filter.SearchText.Trim();
            query = query.Where(d =>
                d.DocumentNumber.Contains(s) ||
                d.Title.Contains(s) ||
                (d.Description != null && d.Description.Contains(s)) ||
                (d.ReferenceCode != null && d.ReferenceCode.Contains(s)) ||
                (d.Tags != null && d.Tags.Contains(s)));
        }

        var rows = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(250)
            .ToListAsync();

        // Enforce server-side per-document effective access policy on every search result
        return rows
            .Where(d => EvaluateEffectivePermissions(d, actor, isHrOrAdmin, isSupervisor).CanViewMetadata)
            .Select(d => MapToSummaryDto(d, orgName))
            .ToList();
    }

    public async Task<List<DmsDocumentSummaryDto>> GetStaffDocumentsAsync(int individualId, string? categoryCode = null)
    {
        var (_, org, _, _) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        int? categoryId = null;
        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            var cat = await db.DmsCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == categoryCode && (c.OrganisationId == null || c.OrganisationId == org));
            categoryId = cat?.CategoryId;
        }

        return await SearchDocumentsAsync(new DmsDocumentSearchFilterDto
        {
            OrganisationId = org,
            SubjectIndividualId = individualId,
            CategoryId = categoryId,
            IncludeArchived = true
        });
    }

    public async Task<List<DmsDocumentSummaryDto>> GetOrganisationLibraryAsync(int? organisationId = null, int? departmentStructureId = null, string? categoryCode = null)
    {
        var (_, org, _, _) = await ScopeAsync(organisationId);
        await using var db = await OpenDbAsync(org);

        int? categoryId = null;
        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            var cat = await db.DmsCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == categoryCode && (c.OrganisationId == null || c.OrganisationId == org));
            categoryId = cat?.CategoryId;
        }

        return await SearchDocumentsAsync(new DmsDocumentSearchFilterDto
        {
            OrganisationId = org,
            DepartmentStructureId = departmentStructureId,
            CategoryId = categoryId,
            TargetDomain = categoryId.HasValue ? null : "Organisation",
            IncludeArchived = false
        });
    }

    public async Task<List<DmsDocumentSummaryDto>> GetLinkedEntityDocumentsAsync(string targetEntityType, long targetEntityId)
    {
        var (_, org, _, _) = await ScopeAsync();
        return await SearchDocumentsAsync(new DmsDocumentSearchFilterDto
        {
            OrganisationId = org,
            LinkedEntityType = targetEntityType,
            LinkedEntityId = targetEntityId,
            IncludeArchived = true
        });
    }

    public async Task<DmsDocumentDetailDto?> GetDocumentDetailAsync(long documentId)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.DocumentType)
            .Include(d => d.RetentionPolicy)
            .Include(d => d.SubjectIndividual).ThenInclude(i => i!.Staff)
            .Include(d => d.Versions)
            .Include(d => d.Links)
            .Include(d => d.Permissions)
            .Include(d => d.Signatures)
            .Include(d => d.LegalHolds)
            .Include(d => d.Events)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (doc is null)
            return null;

        if (!actor.IsSuperAdministrator && doc.OrganisationId != org)
            throw new UnauthorizedAccessException("Document belongs to a different organisation.");

        var perms = EvaluateEffectivePermissions(doc, actor, isHrOrAdmin, isSupervisor);
        if (!perms.CanViewMetadata)
            throw new UnauthorizedAccessException("You do not have permission to view this document.");

        var orgName = (await db.Organisations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.BusinessEntityID == doc.OrganisationId))?.OrganisationName ?? $"Organisation #{doc.OrganisationId}";

        return new DmsDocumentDetailDto
        {
            Summary = MapToSummaryDto(doc, orgName),
            CustomMetadataJson = doc.CustomMetadataJson,
            RetentionPolicyId = doc.RetentionPolicyId,
            RetentionPolicyName = doc.RetentionPolicy?.Name,
            SupersededByDocumentId = doc.SupersededByDocumentId,
            RevokedReason = doc.RevokedReason,
            ArchivedAtUtc = doc.ArchivedAtUtc,
            DisposedAtUtc = doc.DisposedAtUtc,
            DisposalCertificateRef = doc.DisposalCertificateRef,
            EffectivePermissions = perms,
            Versions = doc.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new DmsDocumentVersionDto
                {
                    DocumentVersionId = v.DocumentVersionId,
                    DocumentId = v.DocumentId,
                    VersionNumber = v.VersionNumber,
                    VersionLabel = v.VersionLabel,
                    OriginalFileName = v.OriginalFileName,
                    ContentType = v.ContentType,
                    FileExtension = v.FileExtension,
                    FileSizeBytes = v.FileSizeBytes,
                    Sha256Hash = v.Sha256Hash,
                    ChangeComments = v.ChangeComments,
                    IsCurrentVersion = v.IsCurrentVersion,
                    IsOfficialSnapshot = v.IsOfficialSnapshot,
                    CreatedAtUtc = v.CreatedAtUtc,
                    CreatedByUserName = v.CreatedByUserName
                }).ToList(),
            Links = doc.Links
                .Where(l => l.IsActive)
                .OrderByDescending(l => l.LinkedAtUtc)
                .Select(l => new DmsDocumentLinkDto
                {
                    DocumentLinkId = l.DocumentLinkId,
                    DocumentId = l.DocumentId,
                    TargetEntityType = l.TargetEntityType,
                    TargetEntityId = l.TargetEntityId,
                    TargetReferenceCode = l.TargetReferenceCode,
                    TargetDisplayName = l.TargetDisplayName,
                    LinkRelationship = l.LinkRelationship,
                    Notes = l.Notes,
                    LinkedAtUtc = l.LinkedAtUtc,
                    LinkedByUserName = l.LinkedByUserName
                }).ToList(),
            Permissions = doc.Permissions
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.GrantedAtUtc)
                .Select(p => new DmsDocumentPermissionDto
                {
                    DocumentPermissionId = p.DocumentPermissionId,
                    DocumentId = p.DocumentId,
                    PrincipalType = p.PrincipalType,
                    PrincipalId = p.PrincipalId,
                    PrincipalName = p.PrincipalName,
                    PermissionLevel = p.PermissionLevel,
                    ExpiresAtUtc = p.ExpiresAtUtc,
                    GrantReason = p.GrantReason,
                    GrantedAtUtc = p.GrantedAtUtc,
                    GrantedByUserName = p.GrantedByUserName
                }).ToList(),
            Signatures = doc.Signatures
                .OrderByDescending(s => s.SignedAtUtc)
                .Select(s => new DmsDocumentSignatureDto
                {
                    DocumentSignatureId = s.DocumentSignatureId,
                    DocumentId = s.DocumentId,
                    DocumentVersionId = s.DocumentVersionId,
                    VersionNumber = doc.Versions.FirstOrDefault(v => v.DocumentVersionId == s.DocumentVersionId)?.VersionNumber ?? 1,
                    SignerUserId = s.SignerUserId,
                    SignerName = s.SignerName,
                    SignerTitle = s.SignerTitle,
                    SignatureRole = s.SignatureRole,
                    SignatureStatus = s.SignatureStatus,
                    ContentSha256AtSigning = s.ContentSha256AtSigning,
                    SignatureDigest = s.SignatureDigest,
                    Remarks = s.Remarks,
                    SignedAtUtc = s.SignedAtUtc
                }).ToList(),
            LegalHolds = doc.LegalHolds
                .OrderByDescending(h => h.PlacedAtUtc)
                .Select(h => new DmsLegalHoldDto
                {
                    LegalHoldId = h.LegalHoldId,
                    DocumentId = h.DocumentId,
                    DocumentNumber = doc.DocumentNumber,
                    DocumentTitle = doc.Title,
                    HoldReference = h.HoldReference,
                    CaseOrMatterName = h.CaseOrMatterName,
                    HoldReason = h.HoldReason,
                    IsActive = h.IsActive,
                    PlacedAtUtc = h.PlacedAtUtc,
                    PlacedByUserName = h.PlacedByUserName,
                    ReleasedAtUtc = h.ReleasedAtUtc,
                    ReleasedByUserName = h.ReleasedByUserName,
                    ReleaseNotes = h.ReleaseNotes
                }).ToList(),
            AuditEvents = doc.Events
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(100)
                .Select(e => new DmsDocumentEventDto
                {
                    DocumentEventId = e.DocumentEventId,
                    DocumentId = e.DocumentId,
                    DocumentVersionId = e.DocumentVersionId,
                    EventType = e.EventType,
                    EventDescription = e.EventDescription,
                    PerformedByUserName = e.PerformedByUserName,
                    OccurredAtUtc = e.OccurredAtUtc
                }).ToList()
        };
    }

    public async Task<ServiceResult> UploadDocumentAsync(DmsDocumentUploadInput input)
    {
        var (actor, org, isHrOrAdmin, _) = await ScopeAsync(input.OrganisationId);

        if (string.IsNullOrWhiteSpace(input.Title))
            return ServiceResult.Fail("Document Title is required.");

        if (input.DmsDocumentTypeId <= 0)
            return ServiceResult.Fail("Please select a valid Document Type.");

        if (input.FileContent is null || input.FileContent.Length == 0)
            return ServiceResult.Fail("Please select a file to upload.");

        await using var db = await OpenDbAsync(org);

        var docType = await db.DmsDocumentTypes
            .Include(t => t.Category)
            .Include(t => t.RetentionPolicy)
            .FirstOrDefaultAsync(t => t.DmsDocumentTypeId == input.DmsDocumentTypeId &&
                                      t.IsActive &&
                                      (t.OrganisationId == null || t.OrganisationId == org));

        if (docType is null)
            return ServiceResult.Fail("Selected Document Type is invalid or inactive.");

        if (!isHrOrAdmin && !docType.AllowEmployeeUpload && !actor.HasPermission(Permissions.DocumentUpload))
            return ServiceResult.Fail($"Only HR Administrators may upload documents of type '{docType.Name}'.");

        var subjectIndividualId = input.SubjectIndividualId;
        if (!isHrOrAdmin && docType.Category.TargetDomain == "Staff")
        {
            subjectIndividualId = actor.IndividualId;
        }

        // Store binary via secure IDocumentStorageProvider
        var writeResult = await storage.StoreAsync(new DocumentStorageWriteRequest
        {
            OrganisationId = org,
            OriginalFileName = input.FileName,
            DeclaredContentType = input.ContentType,
            Content = input.FileContent,
            PermittedExtensionsCsv = docType.PermittedExtensions,
            MaxFileSizeMb = docType.MaxFileSizeMb,
            ActorUserId = actor.UserId
        });

        if (!writeResult.Success)
        {
            return ServiceResult.Fail(writeResult.ErrorMessage ?? "Secure storage validation failed.");
        }

        var docNumber = $"DMS-{org}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var initialStatus = input.SubmitImmediatelyForApproval && docType.RequiresApproval
            ? "PendingApproval"
            : docType.RequiresVerification && !isHrOrAdmin
                ? "PendingVerification"
                : "Active";

        var retentionMonths = docType.RetentionPolicy?.RetentionMonths;
        DateTime? retentionDue = retentionMonths.HasValue
            ? DateTime.UtcNow.AddMonths(retentionMonths.Value)
            : null;

        // Also register in legacy dbo.Documents and BE_BusinessEntityDocuments for backward compatibility
        int? legacyDocId = null;
        if (docType.LegacyDocumentTypeId.HasValue)
        {
            var legacyDoc = new Document
            {
                DocumentName = input.Title.Trim(),
                DocumentDate = DateOnly.FromDateTime(input.DocumentDate ?? DateTime.UtcNow),
                Comments = input.Description?.Trim(),
                Mimetype = writeResult.VerifiedContentType,
                DocumentSize = (int)Math.Min(int.MaxValue, writeResult.FileSizeBytes),
                IsDeleted = false,
                DocumentTypeId = docType.LegacyDocumentTypeId.Value
            };
            db.Documents.Add(legacyDoc);
            await db.SaveChangesAsync();
            legacyDocId = legacyDoc.DocumentId;

            var entityToLink = subjectIndividualId ?? org;
            if (entityToLink > 0 && !await db.BusinessEntitiesDocuments.AnyAsync(b => b.BusinessEntityId == entityToLink && b.DocumentId == legacyDoc.DocumentId))
            {
                db.BusinessEntitiesDocuments.Add(new BusinessEntitiesDocument
                {
                    BusinessEntityId = entityToLink,
                    DocumentId = legacyDoc.DocumentId,
                    LinkedDate = DateTime.UtcNow,
                    LinkedByGlobalUserId = actor.UserId
                });
            }
        }

        var dmsDoc = new DmsDocument
        {
            LegacyDocumentId = legacyDocId,
            DocumentNumber = docNumber,
            OrganisationId = org,
            DepartmentStructureId = input.DepartmentStructureId ?? actor.ActiveJob?.OrganisationStructureId,
            SubjectIndividualId = subjectIndividualId,
            SubjectJobId = subjectIndividualId == actor.IndividualId ? actor.ActiveJob?.JobId : null,
            CategoryId = docType.CategoryId,
            DmsDocumentTypeId = docType.DmsDocumentTypeId,
            Title = input.Title.Trim(),
            Description = input.Description?.Trim(),
            ReferenceCode = input.ReferenceCode?.Trim(),
            Classification = string.IsNullOrWhiteSpace(input.Classification) ? docType.DefaultClassification : input.Classification,
            AccessScope = string.IsNullOrWhiteSpace(input.AccessScope) ? docType.DefaultAccessScope : input.AccessScope,
            Status = initialStatus,
            CurrentVersionNumber = 1,
            DocumentDate = input.DocumentDate ?? DateTime.UtcNow,
            EffectiveFromDate = input.EffectiveFromDate,
            ExpiryDate = input.ExpiryDate,
            Tags = input.Tags?.Trim(),
            CustomMetadataJson = input.CustomMetadataJson,
            IsVerified = isHrOrAdmin && docType.RequiresVerification,
            VerifiedByUserId = isHrOrAdmin && docType.RequiresVerification ? actor.UserId : null,
            VerifiedByUserName = isHrOrAdmin && docType.RequiresVerification ? actor.FullName : null,
            VerifiedAtUtc = isHrOrAdmin && docType.RequiresVerification ? DateTime.UtcNow : null,
            RetentionPolicyId = docType.RetentionPolicyId,
            RetentionDueDateUtc = retentionDue,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actor.UserId,
            CreatedByUserName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName
        };

        db.DmsDocuments.Add(dmsDoc);
        await db.SaveChangesAsync();

        var version1 = new DmsDocumentVersion
        {
            DocumentId = dmsDoc.DocumentId,
            VersionNumber = 1,
            VersionLabel = "v1.0",
            StoredObjectId = writeResult.StoredObjectId,
            OriginalFileName = writeResult.SafeFileName,
            ContentType = writeResult.VerifiedContentType,
            FileExtension = writeResult.FileExtension,
            FileSizeBytes = writeResult.FileSizeBytes,
            Sha256Hash = writeResult.Sha256Hash,
            ChangeComments = string.IsNullOrWhiteSpace(input.VersionComments) ? "Initial registration and upload." : input.VersionComments.Trim(),
            IsCurrentVersion = true,
            IsOfficialSnapshot = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actor.UserId,
            CreatedByUserName = dmsDoc.CreatedByUserName
        };

        db.DmsDocumentVersions.Add(version1);
        await db.SaveChangesAsync();

        dmsDoc.CurrentVersionId = version1.DocumentVersionId;

        // Create primary links without duplicating physical file
        if (subjectIndividualId is > 0)
        {
            var staffName = (await db.Individuals.AsNoTracking().FirstOrDefaultAsync(i => i.BusinessEntityId == subjectIndividualId.Value)) is { } ind
                ? FullName(ind)
                : $"Staff #{subjectIndividualId.Value}";

            db.DmsDocumentLinks.Add(new DmsDocumentLink
            {
                DocumentId = dmsDoc.DocumentId,
                OrganisationId = org,
                TargetEntityType = "Staff",
                TargetEntityId = subjectIndividualId.Value,
                TargetReferenceCode = $"STAFF-{subjectIndividualId.Value}",
                TargetDisplayName = staffName,
                LinkRelationship = "Primary",
                LinkedAtUtc = DateTime.UtcNow,
                LinkedByUserId = actor.UserId,
                LinkedByUserName = dmsDoc.CreatedByUserName
            });
        }
        else if (docType.Category.TargetDomain == "Organisation" || docType.Category.TargetDomain == "Legal")
        {
            var orgName = (await db.Organisations.AsNoTracking().FirstOrDefaultAsync(o => o.BusinessEntityID == org))?.OrganisationName ?? $"Organisation #{org}";
            db.DmsDocumentLinks.Add(new DmsDocumentLink
            {
                DocumentId = dmsDoc.DocumentId,
                OrganisationId = org,
                TargetEntityType = "Organisation",
                TargetEntityId = org,
                TargetReferenceCode = $"ORG-{org}",
                TargetDisplayName = orgName,
                LinkRelationship = "Primary",
                LinkedAtUtc = DateTime.UtcNow,
                LinkedByUserId = actor.UserId,
                LinkedByUserName = dmsDoc.CreatedByUserName
            });
        }

        if (!string.IsNullOrWhiteSpace(input.PrimaryLinkEntityType) && input.PrimaryLinkEntityId is > 0)
        {
            var alreadyLinkedStaff = input.PrimaryLinkEntityType.Equals("Staff", StringComparison.OrdinalIgnoreCase) &&
                                     subjectIndividualId == input.PrimaryLinkEntityId.Value;
            if (!alreadyLinkedStaff)
            {
                db.DmsDocumentLinks.Add(new DmsDocumentLink
                {
                    DocumentId = dmsDoc.DocumentId,
                    OrganisationId = org,
                    TargetEntityType = input.PrimaryLinkEntityType.Trim(),
                    TargetEntityId = input.PrimaryLinkEntityId.Value,
                    TargetReferenceCode = input.PrimaryLinkReferenceCode?.Trim(),
                    TargetDisplayName = input.PrimaryLinkDisplayName?.Trim(),
                    LinkRelationship = "SupportingEvidence",
                    LinkedAtUtc = DateTime.UtcNow,
                    LinkedByUserId = actor.UserId,
                    LinkedByUserName = dmsDoc.CreatedByUserName
                });
            }
        }

        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = dmsDoc.DocumentId,
            DocumentVersionId = version1.DocumentVersionId,
            OrganisationId = org,
            EventType = "Uploaded",
            EventDescription = $"Registered document '{dmsDoc.Title}' ({dmsDoc.DocumentNumber}) v1.0 [{writeResult.SafeFileName}, SHA-256: {writeResult.Sha256Hash[..12]}...].",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = dmsDoc.CreatedByUserName,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Document '{dmsDoc.DocumentNumber}' uploaded and registered.");
    }

    public async Task<ServiceResult> UploadNewVersionAsync(DmsDocumentNewVersionInput input)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .Include(d => d.DocumentType)
            .Include(d => d.Versions)
            .Include(d => d.Permissions)
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId && d.OrganisationId == org && !d.IsDeleted);

        if (doc is null)
            return ServiceResult.Fail("Document not found.");

        if (doc.IsImmutable || doc.IsOfficialRecord)
            return ServiceResult.Fail("Finalised official records are immutable and cannot have new versions uploaded directly. Issue a superseding document instead.");

        if (doc.HasActiveLegalHold)
            return ServiceResult.Fail("This document is under an active Legal Hold and cannot be modified.");

        var perms = EvaluateEffectivePermissions(doc, actor, isHrOrAdmin, isSupervisor);
        if (!perms.CanAddVersion)
            return ServiceResult.Fail("You do not have permission to upload a new version for this document.");

        if (doc.IsLockedForEdit && doc.CheckedOutByUserId != actor.UserId && !isHrOrAdmin)
            return ServiceResult.Fail($"Document is currently checked out by {doc.CheckedOutByUserName}.");

        var writeResult = await storage.StoreAsync(new DocumentStorageWriteRequest
        {
            OrganisationId = org,
            OriginalFileName = input.FileName,
            DeclaredContentType = input.ContentType,
            Content = input.FileContent,
            PermittedExtensionsCsv = doc.DocumentType.PermittedExtensions,
            MaxFileSizeMb = doc.DocumentType.MaxFileSizeMb,
            ActorUserId = actor.UserId
        });

        if (!writeResult.Success)
            return ServiceResult.Fail(writeResult.ErrorMessage ?? "Secure storage validation failed.");

        // Mark existing versions as non-current without overwriting them
        foreach (var v in doc.Versions.Where(v => v.IsCurrentVersion))
        {
            v.IsCurrentVersion = false;
        }

        var nextVerNum = (doc.Versions.Count > 0 ? doc.Versions.Max(v => v.VersionNumber) : doc.CurrentVersionNumber) + 1;
        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;

        var newVer = new DmsDocumentVersion
        {
            DocumentId = doc.DocumentId,
            VersionNumber = nextVerNum,
            VersionLabel = $"v{nextVerNum}.0",
            StoredObjectId = writeResult.StoredObjectId,
            OriginalFileName = writeResult.SafeFileName,
            ContentType = writeResult.VerifiedContentType,
            FileExtension = writeResult.FileExtension,
            FileSizeBytes = writeResult.FileSizeBytes,
            Sha256Hash = writeResult.Sha256Hash,
            ChangeComments = string.IsNullOrWhiteSpace(input.ChangeComments) ? $"Uploaded version v{nextVerNum}.0" : input.ChangeComments.Trim(),
            IsCurrentVersion = true,
            IsOfficialSnapshot = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actor.UserId,
            CreatedByUserName = actorName
        };

        db.DmsDocumentVersions.Add(newVer);
        await db.SaveChangesAsync();

        doc.CurrentVersionNumber = nextVerNum;
        doc.CurrentVersionId = newVer.DocumentVersionId;
        doc.UpdatedAtUtc = DateTime.UtcNow;
        doc.UpdatedByUserId = actor.UserId;

        if (input.CheckInAfterUpload && doc.IsLockedForEdit)
        {
            doc.IsLockedForEdit = false;
            doc.CheckedOutByUserId = null;
            doc.CheckedOutByUserName = null;
            doc.CheckedOutAtUtc = null;
            doc.CheckOutNote = null;
        }

        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = doc.DocumentId,
            DocumentVersionId = newVer.DocumentVersionId,
            OrganisationId = org,
            EventType = "VersionAdded",
            EventDescription = $"Added version {newVer.VersionLabel} ({writeResult.SafeFileName}, SHA-256: {writeResult.Sha256Hash[..12]}...). Comments: {newVer.ChangeComments}",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actorName,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Version {newVer.VersionLabel} uploaded and set as current version.");
    }

    public async Task<ServiceResult> UpdateDocumentMetadataAsync(DmsDocumentMetadataUpdateInput input)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .Include(d => d.Permissions)
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId && d.OrganisationId == org && !d.IsDeleted);

        if (doc is null)
            return ServiceResult.Fail("Document not found.");

        var perms = EvaluateEffectivePermissions(doc, actor, isHrOrAdmin, isSupervisor);
        if (!perms.CanModifyMetadata)
            return ServiceResult.Fail("You do not have permission to modify metadata for this document (or it is immutable/locked).");

        if (string.IsNullOrWhiteSpace(input.Title))
            return ServiceResult.Fail("Document Title cannot be empty.");

        var prevSnapshot = JsonSerializer.Serialize(new
        {
            doc.Title,
            doc.Classification,
            doc.AccessScope,
            doc.ReferenceCode,
            doc.Tags
        });

        doc.Title = input.Title.Trim();
        doc.Description = input.Description?.Trim();
        doc.ReferenceCode = input.ReferenceCode?.Trim();
        doc.Classification = string.IsNullOrWhiteSpace(input.Classification) ? doc.Classification : input.Classification;
        doc.AccessScope = string.IsNullOrWhiteSpace(input.AccessScope) ? doc.AccessScope : input.AccessScope;
        doc.DepartmentStructureId = input.DepartmentStructureId;
        doc.DocumentDate = input.DocumentDate;
        doc.EffectiveFromDate = input.EffectiveFromDate;
        doc.ExpiryDate = input.ExpiryDate;
        doc.Tags = input.Tags?.Trim();
        doc.CustomMetadataJson = input.CustomMetadataJson;
        if (isHrOrAdmin && input.RetentionPolicyId.HasValue)
        {
            doc.RetentionPolicyId = input.RetentionPolicyId;
        }
        doc.UpdatedAtUtc = DateTime.UtcNow;
        doc.UpdatedByUserId = actor.UserId;

        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;
        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = doc.DocumentId,
            DocumentVersionId = doc.CurrentVersionId,
            OrganisationId = org,
            EventType = "MetadataUpdated",
            EventDescription = $"Updated document metadata (Classification={doc.Classification}, Scope={doc.AccessScope}).",
            PreviousStateJson = prevSnapshot,
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actorName,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Document metadata updated.");
    }

    public async Task<DmsDocumentDownloadDto> DownloadDocumentVersionAsync(long documentId, int? versionNumber = null, bool isPreview = false)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .Include(d => d.Versions)
            .Include(d => d.Permissions)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (doc is null || (!actor.IsSuperAdministrator && doc.OrganisationId != org))
        {
            return new DmsDocumentDownloadDto
            {
                Success = false,
                ErrorMessage = "Document not found or access denied."
            };
        }

        var perms = EvaluateEffectivePermissions(doc, actor, isHrOrAdmin, isSupervisor);
        if (isPreview && !perms.CanPreview)
        {
            return new DmsDocumentDownloadDto
            {
                Success = false,
                ErrorMessage = "You do not have permission to preview this document."
            };
        }
        if (!isPreview && !perms.CanDownload)
        {
            return new DmsDocumentDownloadDto
            {
                Success = false,
                ErrorMessage = "You do not have permission to download this document."
            };
        }

        var targetVersion = versionNumber.HasValue
            ? doc.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber.Value)
            : doc.Versions.FirstOrDefault(v => v.IsCurrentVersion) ?? doc.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        if (targetVersion is null)
        {
            return new DmsDocumentDownloadDto
            {
                Success = false,
                ErrorMessage = "Requested document version was not found."
            };
        }

        var readResult = await storage.RetrieveAsync(targetVersion.StoredObjectId, doc.OrganisationId);
        if (!readResult.Success)
        {
            return new DmsDocumentDownloadDto
            {
                Success = false,
                ErrorMessage = readResult.ErrorMessage ?? "Could not retrieve binary from private storage."
            };
        }

        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;
        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = doc.DocumentId,
            DocumentVersionId = targetVersion.DocumentVersionId,
            OrganisationId = doc.OrganisationId,
            EventType = isPreview ? "Previewed" : "Downloaded",
            EventDescription = $"{(isPreview ? "Previewed" : "Downloaded")} version {targetVersion.VersionLabel} ({targetVersion.OriginalFileName}) [Integrity Verified: SHA-256].",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actorName,
            OccurredAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return new DmsDocumentDownloadDto
        {
            Success = true,
            DocumentId = doc.DocumentId,
            VersionNumber = targetVersion.VersionNumber,
            DocumentNumber = doc.DocumentNumber,
            FileName = targetVersion.OriginalFileName,
            ContentType = readResult.ContentType,
            FileExtension = readResult.FileExtension,
            FileSizeBytes = readResult.FileSizeBytes,
            Sha256Hash = readResult.Sha256Hash,
            IntegrityVerified = readResult.IntegrityVerified,
            Content = readResult.Content
        };
    }

    // =========================================================================
    // 3. MULTI-RECORD LINKING, SECURITY ACEs & LIFECYCLE GOVERNANCE
    // =========================================================================

    public async Task<ServiceResult> LinkDocumentToRecordAsync(DmsDocumentLinkInput input)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        if (string.IsNullOrWhiteSpace(input.TargetEntityType) || input.TargetEntityId <= 0)
            return ServiceResult.Fail("Target entity type and ID are required.");

        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .Include(d => d.Links)
            .Include(d => d.Permissions)
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId && d.OrganisationId == org && !d.IsDeleted);

        if (doc is null)
            return ServiceResult.Fail("Document not found.");

        var perms = EvaluateEffectivePermissions(doc, actor, isHrOrAdmin, isSupervisor);
        if (!perms.CanModifyMetadata && !perms.CanShare)
            return ServiceResult.Fail("You do not have permission to link this document to another business record.");

        var targetType = input.TargetEntityType.Trim();
        var existing = doc.Links.FirstOrDefault(l => l.TargetEntityType.Equals(targetType, StringComparison.OrdinalIgnoreCase) &&
                                                     l.TargetEntityId == input.TargetEntityId);

        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;

        if (existing != null)
        {
            existing.IsActive = true;
            existing.LinkRelationship = string.IsNullOrWhiteSpace(input.LinkRelationship) ? "Primary" : input.LinkRelationship;
            existing.TargetReferenceCode = input.TargetReferenceCode?.Trim();
            existing.TargetDisplayName = input.TargetDisplayName?.Trim();
            existing.Notes = input.Notes?.Trim();
            existing.LinkedAtUtc = DateTime.UtcNow;
            existing.LinkedByUserId = actor.UserId;
            existing.LinkedByUserName = actorName;
        }
        else
        {
            db.DmsDocumentLinks.Add(new DmsDocumentLink
            {
                DocumentId = doc.DocumentId,
                OrganisationId = org,
                TargetEntityType = targetType,
                TargetEntityId = input.TargetEntityId,
                TargetReferenceCode = input.TargetReferenceCode?.Trim(),
                TargetDisplayName = input.TargetDisplayName?.Trim(),
                LinkRelationship = string.IsNullOrWhiteSpace(input.LinkRelationship) ? "Primary" : input.LinkRelationship,
                Notes = input.Notes?.Trim(),
                IsActive = true,
                LinkedAtUtc = DateTime.UtcNow,
                LinkedByUserId = actor.UserId,
                LinkedByUserName = actorName
            });
        }

        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = doc.DocumentId,
            DocumentVersionId = doc.CurrentVersionId,
            OrganisationId = org,
            EventType = "Linked",
            EventDescription = $"Linked document to {targetType} #{input.TargetEntityId} ({input.TargetDisplayName ?? input.TargetReferenceCode ?? "Record"}) without duplicating binary.",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actorName,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Document linked to {targetType} #{input.TargetEntityId}.");
    }

    public async Task<ServiceResult> UnlinkDocumentFromRecordAsync(long documentLinkId)
    {
        var (actor, org, isHrOrAdmin, _) = await ScopeAsync();
        if (!isHrOrAdmin)
            return ServiceResult.Fail("Only HR Administrators can remove business record links.");

        await using var db = await OpenDbAsync(org);
        var link = await db.DmsDocumentLinks
            .Include(l => l.Document)
            .FirstOrDefaultAsync(l => l.DocumentLinkId == documentLinkId && l.OrganisationId == org);

        if (link is null)
            return ServiceResult.Fail("Document link not found.");

        if (link.Document.HasActiveLegalHold)
            return ServiceResult.Fail("Cannot unlink records while document is under an active Legal Hold.");

        link.IsActive = false;
        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;

        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = link.DocumentId,
            OrganisationId = org,
            EventType = "Unlinked",
            EventDescription = $"Unlinked document from {link.TargetEntityType} #{link.TargetEntityId}.",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actorName,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Document link removed.");
    }

    public async Task<ServiceResult> GrantOrRevokePermissionAsync(DmsDocumentPermissionInput input)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .Include(d => d.Permissions)
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId && d.OrganisationId == org && !d.IsDeleted);

        if (doc is null)
            return ServiceResult.Fail("Document not found.");

        var perms = EvaluateEffectivePermissions(doc, actor, isHrOrAdmin, isSupervisor);
        if (!perms.CanShare)
            return ServiceResult.Fail("You do not have permission to modify access control entries for this document.");

        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;

        db.DmsDocumentPermissions.Add(new DmsDocumentPermission
        {
            DocumentId = doc.DocumentId,
            OrganisationId = org,
            PrincipalType = input.PrincipalType.Trim(),
            PrincipalId = input.PrincipalId,
            PrincipalName = input.PrincipalName?.Trim(),
            PermissionLevel = input.PermissionLevel.Trim(),
            ExpiresAtUtc = input.ExpiresAtUtc,
            GrantReason = input.GrantReason?.Trim(),
            IsActive = true,
            GrantedAtUtc = DateTime.UtcNow,
            GrantedByUserId = actor.UserId,
            GrantedByUserName = actorName
        });

        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = doc.DocumentId,
            OrganisationId = org,
            EventType = "Shared",
            EventDescription = $"Granted '{input.PermissionLevel}' permission to {input.PrincipalType} '{input.PrincipalName ?? input.PrincipalId?.ToString()}'. Reason: {input.GrantReason}",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actorName,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Document access policy updated.");
    }

    public async Task<ServiceResult> ExecuteLifecycleActionAsync(DmsDocumentLifecycleActionInput input)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .Include(d => d.DocumentType)
            .Include(d => d.Versions)
            .Include(d => d.Permissions)
            .Include(d => d.LegalHolds)
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId && d.OrganisationId == org && !d.IsDeleted);

        if (doc is null)
            return ServiceResult.Fail("Document not found.");

        var perms = EvaluateEffectivePermissions(doc, actor, isHrOrAdmin, isSupervisor);
        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;
        var currentVer = doc.Versions.FirstOrDefault(v => v.IsCurrentVersion) ?? doc.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        switch (input.Action)
        {
            case "CheckOut":
                if (!perms.CanAddVersion)
                    return ServiceResult.Fail("You do not have permission to check out this document.");
                if (doc.IsImmutable || doc.IsOfficialRecord)
                    return ServiceResult.Fail("Immutable official records cannot be checked out.");
                if (doc.IsLockedForEdit && doc.CheckedOutByUserId != actor.UserId)
                    return ServiceResult.Fail($"Already checked out by {doc.CheckedOutByUserName}.");

                doc.IsLockedForEdit = true;
                doc.CheckedOutByUserId = actor.UserId;
                doc.CheckedOutByUserName = actorName;
                doc.CheckedOutAtUtc = DateTime.UtcNow;
                doc.CheckOutNote = input.Comments?.Trim();
                break;

            case "CheckIn":
                if (!doc.IsLockedForEdit)
                    return ServiceResult.Fail("Document is not currently checked out.");
                if (doc.CheckedOutByUserId != actor.UserId && !isHrOrAdmin)
                    return ServiceResult.Fail("Only the user who checked out the document or an HR Administrator can check it in.");

                doc.IsLockedForEdit = false;
                doc.CheckedOutByUserId = null;
                doc.CheckedOutByUserName = null;
                doc.CheckedOutAtUtc = null;
                doc.CheckOutNote = null;
                break;

            case "Verify":
                if (!perms.CanReview)
                    return ServiceResult.Fail("You do not have permission to verify this document.");
                doc.IsVerified = true;
                doc.VerifiedByUserId = actor.UserId;
                doc.VerifiedByUserName = actorName;
                doc.VerifiedAtUtc = DateTime.UtcNow;
                doc.VerificationNotes = input.Comments?.Trim();
                if (doc.Status == "PendingVerification")
                    doc.Status = doc.DocumentType.RequiresApproval ? "PendingApproval" : "Active";
                break;

            case "SubmitForApproval":
                if (!perms.CanSubmitForApproval)
                    return ServiceResult.Fail("You do not have permission to submit this document for approval.");
                doc.Status = "PendingApproval";
                break;

            case "Approve":
            case "Publish":
                if (!perms.CanApprove)
                    return ServiceResult.Fail("You do not have permission to approve or publish this document.");
                doc.ApprovedByUserId = actor.UserId;
                doc.ApprovedByUserName = actorName;
                doc.ApprovedAtUtc = DateTime.UtcNow;
                doc.IsVerified = true;
                if (doc.DocumentType.IsOfficialRecordWhenApproved || input.Action == "Publish")
                {
                    doc.IsOfficialRecord = true;
                    doc.IsImmutable = true;
                    doc.Status = input.Action == "Publish" ? "Published" : "OfficialRecord";
                    if (currentVer != null)
                        currentVer.IsOfficialSnapshot = true;
                }
                else
                {
                    doc.Status = "Approved";
                }
                break;

            case "Reject":
                if (!perms.CanApprove && !perms.CanReview)
                    return ServiceResult.Fail("You do not have permission to reject this document.");
                doc.Status = "Draft";
                break;

            case "Sign":
                if (!perms.CanSign || currentVer is null)
                    return ServiceResult.Fail("You do not have permission to sign this document.");
                var sigInput = $"{doc.DocumentNumber}|{currentVer.VersionNumber}|{currentVer.Sha256Hash}|{actor.UserId}|{DateTime.UtcNow:O}";
                var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sigInput))).ToLowerInvariant();
                db.DmsDocumentSignatures.Add(new DmsDocumentSignature
                {
                    DocumentId = doc.DocumentId,
                    DocumentVersionId = currentVer.DocumentVersionId,
                    OrganisationId = org,
                    SignerUserId = actor.UserId,
                    SignerName = actorName,
                    SignerTitle = string.IsNullOrWhiteSpace(input.SignerTitle) ? (actor.ActiveJob?.PositionName ?? "Authorised Signatory") : input.SignerTitle.Trim(),
                    SignatureRole = string.IsNullOrWhiteSpace(input.SignatureRole) ? "Approver" : input.SignatureRole.Trim(),
                    SignatureStatus = "Signed",
                    ContentSha256AtSigning = currentVer.Sha256Hash,
                    SignatureDigest = digest,
                    Remarks = input.Comments?.Trim(),
                    SignedAtUtc = DateTime.UtcNow
                });
                break;

            case "RestoreVersion":
                if (!perms.CanAddVersion)
                    return ServiceResult.Fail("You do not have permission to restore a previous version.");
                if (doc.IsImmutable || doc.IsOfficialRecord)
                    return ServiceResult.Fail("Immutable official records cannot be reverted.");
                if (!input.TargetVersionNumber.HasValue)
                    return ServiceResult.Fail("Target version number is required.");

                var verToRestore = doc.Versions.FirstOrDefault(v => v.VersionNumber == input.TargetVersionNumber.Value);
                if (verToRestore is null)
                    return ServiceResult.Fail($"Version {input.TargetVersionNumber.Value} was not found.");

                foreach (var v in doc.Versions)
                    v.IsCurrentVersion = false;

                var restoredVerNum = doc.Versions.Max(v => v.VersionNumber) + 1;
                var restoredVer = new DmsDocumentVersion
                {
                    DocumentId = doc.DocumentId,
                    VersionNumber = restoredVerNum,
                    VersionLabel = $"v{restoredVerNum}.0 (Restored from v{verToRestore.VersionNumber}.0)",
                    StoredObjectId = verToRestore.StoredObjectId,
                    OriginalFileName = verToRestore.OriginalFileName,
                    ContentType = verToRestore.ContentType,
                    FileExtension = verToRestore.FileExtension,
                    FileSizeBytes = verToRestore.FileSizeBytes,
                    Sha256Hash = verToRestore.Sha256Hash,
                    ChangeComments = $"Restored from historical version v{verToRestore.VersionNumber}.0. {input.Comments}".Trim(),
                    IsCurrentVersion = true,
                    IsOfficialSnapshot = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = actor.UserId,
                    CreatedByUserName = actorName
                };
                db.DmsDocumentVersions.Add(restoredVer);
                await db.SaveChangesAsync();
                doc.CurrentVersionNumber = restoredVerNum;
                doc.CurrentVersionId = restoredVer.DocumentVersionId;
                break;

            case "Revoke":
                if (!perms.CanApprove)
                    return ServiceResult.Fail("Only authorised HR approvers can revoke or supersede a record.");
                if (string.IsNullOrWhiteSpace(input.Comments))
                    return ServiceResult.Fail("A formal revocation/supersession reason is required.");
                doc.Status = input.SupersededByDocumentId.HasValue ? "Superseded" : "Revoked";
                doc.SupersededByDocumentId = input.SupersededByDocumentId;
                doc.RevokedReason = input.Comments.Trim();
                doc.RevokedAtUtc = DateTime.UtcNow;
                doc.RevokedByUserId = actor.UserId;
                break;

            case "Archive":
                if (!perms.CanArchive)
                    return ServiceResult.Fail("You do not have permission to archive this document.");
                doc.Status = "Archived";
                doc.ArchivedAtUtc = DateTime.UtcNow;
                doc.ArchivedByUserId = actor.UserId;
                break;

            case "Dispose":
                if (doc.HasActiveLegalHold || doc.LegalHolds.Any(h => h.IsActive))
                    return ServiceResult.Fail("DISPOSAL BLOCKED: This record is protected by an active Legal Hold.");
                if (doc.IsImmutable && !actor.IsSuperAdministrator && !actor.HasPermission(Permissions.DocumentDispose))
                    return ServiceResult.Fail("DISPOSAL BLOCKED: Immutable official records are protected from ordinary disposal.");
                if (!perms.CanDispose)
                    return ServiceResult.Fail("You do not have permission to dispose of this document.");
                if (string.IsNullOrWhiteSpace(input.Comments))
                    return ServiceResult.Fail("Formal disposal justification and authorization reference are required.");

                doc.Status = "Disposed";
                doc.DisposedAtUtc = DateTime.UtcNow;
                doc.DisposedByUserId = actor.UserId;
                doc.DisposalCertificateRef = $"DISP-{org}-{DateTime.UtcNow:yyyyMMddHHmmss}";
                break;

            default:
                return ServiceResult.Fail($"Unsupported lifecycle action '{input.Action}'.");
        }

        doc.UpdatedAtUtc = DateTime.UtcNow;
        doc.UpdatedByUserId = actor.UserId;

        db.DmsDocumentEvents.Add(new DmsDocumentEvent
        {
            DocumentId = doc.DocumentId,
            DocumentVersionId = doc.CurrentVersionId,
            OrganisationId = org,
            EventType = input.Action,
            EventDescription = $"Executed lifecycle action '{input.Action}' (New Status: {doc.Status}). {input.Comments}".Trim(),
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actorName,
            OccurredAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Document action '{input.Action}' completed.");
    }

    public async Task<ServiceResult> ManageLegalHoldAsync(DmsLegalHoldInput input)
    {
        var (actor, org, isHrOrAdmin, _) = await ScopeAsync();
        if (!isHrOrAdmin)
            return ServiceResult.Fail("Only HR Administrators or Legal Officers can place or release Legal Holds.");

        await using var db = await OpenDbAsync(org);

        var doc = await db.DmsDocuments
            .Include(d => d.LegalHolds)
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId && d.OrganisationId == org && !d.IsDeleted);

        if (doc is null)
            return ServiceResult.Fail("Document not found.");

        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;

        if (input.Release)
        {
            var hold = doc.LegalHolds.FirstOrDefault(h => h.IsActive && (!input.LegalHoldId.HasValue || h.LegalHoldId == input.LegalHoldId.Value));
            if (hold is null)
                return ServiceResult.Fail("No active legal hold found to release.");

            hold.IsActive = false;
            hold.ReleasedAtUtc = DateTime.UtcNow;
            hold.ReleasedByUserId = actor.UserId;
            hold.ReleasedByUserName = actorName;
            hold.ReleaseNotes = input.ReleaseNotes?.Trim();

            doc.HasActiveLegalHold = doc.LegalHolds.Any(h => h.IsActive && h.LegalHoldId != hold.LegalHoldId);

            db.DmsDocumentEvents.Add(new DmsDocumentEvent
            {
                DocumentId = doc.DocumentId,
                OrganisationId = org,
                EventType = "LegalHoldReleased",
                EventDescription = $"Released Legal Hold '{hold.HoldReference}' ({hold.CaseOrMatterName}). Notes: {hold.ReleaseNotes}",
                PerformedByUserId = actor.UserId,
                PerformedByUserName = actorName,
                OccurredAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.HoldReference) || string.IsNullOrWhiteSpace(input.CaseOrMatterName) || string.IsNullOrWhiteSpace(input.HoldReason))
                return ServiceResult.Fail("Hold Reference, Case/Matter Name, and Hold Reason are required.");

            var hold = new DmsLegalHold
            {
                DocumentId = doc.DocumentId,
                OrganisationId = org,
                HoldReference = input.HoldReference.Trim().ToUpperInvariant(),
                CaseOrMatterName = input.CaseOrMatterName.Trim(),
                HoldReason = input.HoldReason.Trim(),
                IsActive = true,
                PlacedAtUtc = DateTime.UtcNow,
                PlacedByUserId = actor.UserId,
                PlacedByUserName = actorName
            };
            db.DmsLegalHolds.Add(hold);
            doc.HasActiveLegalHold = true;

            db.DmsDocumentEvents.Add(new DmsDocumentEvent
            {
                DocumentId = doc.DocumentId,
                OrganisationId = org,
                EventType = "LegalHoldPlaced",
                EventDescription = $"Placed Legal Hold '{hold.HoldReference}' ({hold.CaseOrMatterName}): {hold.HoldReason}",
                PerformedByUserId = actor.UserId,
                PerformedByUserName = actorName,
                OccurredAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok(input.Release ? "Legal Hold released." : "Legal Hold placed on document.");
    }

    public async Task<List<DmsLegalHoldDto>> GetLegalHoldsAsync(int? organisationId = null, bool activeOnly = true)
    {
        var (_, org, isHrOrAdmin, _) = await ScopeAsync(organisationId);
        if (!isHrOrAdmin) return [];

        await using var db = await OpenDbAsync(org);

        var query = db.DmsLegalHolds
            .AsNoTracking()
            .Include(h => h.Document)
            .Where(h => h.OrganisationId == org);

        if (activeOnly)
            query = query.Where(h => h.IsActive);

        var rows = await query
            .OrderByDescending(h => h.PlacedAtUtc)
            .ToListAsync();

        return rows.Select(h => new DmsLegalHoldDto
        {
            LegalHoldId = h.LegalHoldId,
            DocumentId = h.DocumentId,
            DocumentNumber = h.Document.DocumentNumber,
            DocumentTitle = h.Document.Title,
            HoldReference = h.HoldReference,
            CaseOrMatterName = h.CaseOrMatterName,
            HoldReason = h.HoldReason,
            IsActive = h.IsActive,
            PlacedAtUtc = h.PlacedAtUtc,
            PlacedByUserName = h.PlacedByUserName,
            ReleasedAtUtc = h.ReleasedAtUtc,
            ReleasedByUserName = h.ReleasedByUserName,
            ReleaseNotes = h.ReleaseNotes
        }).ToList();
    }

    // =========================================================================
    // 4. TEMPLATES, VERIFIED PLACEHOLDER RESOLUTION & DOCUMENT WORKFLOW REQUESTS
    // =========================================================================

    public async Task<List<DmsDocumentTemplateDto>> GetTemplatesAsync(int? organisationId = null, bool activeOnly = true)
    {
        var (_, org, _, _) = await ScopeAsync(organisationId);
        await using var db = await OpenDbAsync(org);

        var query = db.DmsDocumentTemplates
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.DocumentType)
            .Where(t => t.OrganisationId == null || t.OrganisationId == org);

        if (activeOnly)
            query = query.Where(t => t.IsActive);

        var rows = await query
            .OrderBy(t => t.Code)
            .ThenByDescending(t => t.VersionNumber)
            .ToListAsync();

        return rows.Select(t => new DmsDocumentTemplateDto
        {
            DocumentTemplateId = t.DocumentTemplateId,
            OrganisationId = t.OrganisationId,
            CategoryId = t.CategoryId,
            CategoryName = t.Category?.Name,
            DmsDocumentTypeId = t.DmsDocumentTypeId,
            DocumentTypeName = t.DocumentType?.Name,
            Code = t.Code,
            Name = t.Name,
            Description = t.Description,
            VersionNumber = t.VersionNumber,
            DocumentTitlePattern = t.DocumentTitlePattern,
            HeaderText = t.HeaderText,
            BodyTemplate = t.BodyTemplate,
            FooterText = t.FooterText,
            SignatoryName = t.SignatoryName,
            SignatoryTitle = t.SignatoryTitle,
            IncludeOrganisationLetterhead = t.IncludeOrganisationLetterhead,
            IncludePositionHistoryTable = t.IncludePositionHistoryTable,
            IncludeQrVerification = t.IncludeQrVerification,
            IsEmployeeRequestable = t.IsEmployeeRequestable,
            IsActive = t.IsActive
        }).ToList();
    }

    public async Task<ServiceResult> SaveTemplateAsync(DmsDocumentTemplateDto input)
    {
        var (actor, org, isHrOrAdmin, _) = await ScopeAsync(input.OrganisationId);
        if (!isHrOrAdmin)
            return ServiceResult.Fail("Only HR Administrators can create or version document templates.");

        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.BodyTemplate))
            return ServiceResult.Fail("Template Code, Name, and Body Template are required.");

        // Validate that template only uses whitelisted safe placeholders
        var invalidTokens = ValidateSafeTemplatePlaceholders(input.BodyTemplate + " " + input.DocumentTitlePattern);
        if (invalidTokens.Count > 0)
        {
            return ServiceResult.Fail($"Template contains unauthorised or unsafe placeholder tokens: {string.Join(", ", invalidTokens)}. Allowed placeholders: {{{{EmployeeName}}}}, {{{{NationalId}}}}, {{{{StaffNumber}}}}, {{{{CurrentPosition}}}}, {{{{DepartmentName}}}}, {{{{OrganisationName}}}}, {{{{EmploymentStartDate}}}}, {{{{ServiceDuration}}}}, {{{{DocumentNumber}}}}, {{{{IssueDate}}}}, {{{{Purpose}}}}, {{{{AddressedTo}}}}, {{{{SignatoryName}}}}, {{{{SignatoryTitle}}}}.");
        }

        await using var db = await OpenDbAsync(org);

        var code = input.Code.Trim().ToUpperInvariant();
        var latestVersion = await db.DmsDocumentTemplates
            .Where(t => (t.OrganisationId == org || t.OrganisationId == null) && t.Code == code)
            .MaxAsync(t => (int?)t.VersionNumber) ?? 0;

        // Every save creates a preserved versioned template (Requirement 11)
        var newVersion = new DmsDocumentTemplate
        {
            OrganisationId = org,
            CategoryId = input.CategoryId,
            DmsDocumentTypeId = input.DmsDocumentTypeId,
            Code = code,
            Name = input.Name.Trim(),
            Description = input.Description?.Trim(),
            VersionNumber = latestVersion + 1,
            DocumentTitlePattern = string.IsNullOrWhiteSpace(input.DocumentTitlePattern) ? input.Name.Trim() : input.DocumentTitlePattern.Trim(),
            HeaderText = input.HeaderText?.Trim(),
            BodyTemplate = input.BodyTemplate.Trim(),
            FooterText = input.FooterText?.Trim(),
            SignatoryName = input.SignatoryName?.Trim() ?? "Director of Human Resources",
            SignatoryTitle = input.SignatoryTitle?.Trim() ?? "Human Resources Division",
            IncludeOrganisationLetterhead = input.IncludeOrganisationLetterhead,
            IncludePositionHistoryTable = input.IncludePositionHistoryTable,
            IncludeQrVerification = input.IncludeQrVerification,
            IsEmployeeRequestable = input.IsEmployeeRequestable,
            IsActive = input.IsActive,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actor.UserId
        };

        db.DmsDocumentTemplates.Add(newVersion);
        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Saved template '{newVersion.Code}' as version v{newVersion.VersionNumber}.");
    }

    public async Task<DmsTemplatePreviewDto> ResolveTemplatePreviewAsync(
        int documentTemplateId,
        int? subjectIndividualId = null,
        string? purpose = null,
        string? addressedTo = null)
    {
        var (actor, org, isHrOrAdmin, _) = await ScopeAsync();
        var targetIndividualId = subjectIndividualId is > 0 ? subjectIndividualId.Value : actor.IndividualId;

        if (targetIndividualId != actor.IndividualId && !isHrOrAdmin)
            throw new UnauthorizedAccessException("You cannot resolve verified HRM data for another employee.");

        await using var db = await OpenDbAsync(org);

        var tpl = await db.DmsDocumentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.DocumentTemplateId == documentTemplateId)
            ?? await db.DmsDocumentTemplates.AsNoTracking().OrderByDescending(t => t.VersionNumber).FirstAsync();

        var ind = await db.Individuals
            .AsNoTracking()
            .Include(i => i.Staff)
            .Include(i => i.Idcards)
            .Include(i => i.Passports)
            .FirstOrDefaultAsync(i => i.BusinessEntityId == targetIndividualId);

        var orgName = (await db.Organisations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.BusinessEntityID == org))?.OrganisationName ?? "Official Organisation";

        var jobs = await db.Jobs
            .AsNoTracking()
            .Include(j => j.OrganisationStructure)
            .Include(j => j.JobPositions).ThenInclude(jp => jp.Position)
            .Where(j => j.IndividualID == targetIndividualId && j.OrganisationID == org)
            .OrderBy(j => j.JoinedDate)
            .ToListAsync();

        var fullName = ind != null ? FullName(ind) : actor.FullName;
        var nationalId = ind?.Idcards.FirstOrDefault()?.IdcardNumber ?? ind?.Passports.FirstOrDefault()?.PassportNumber ?? "Verified-ID";
        var staffNumber = ind?.Staff?.EmployeeNumber ?? $"EMP-{targetIndividualId}";
        var earliestJoin = jobs.Count > 0 ? jobs.Min(j => j.JoinedDate) : TodayMaldives;
        var currentJob = jobs.LastOrDefault();
        var deptName = currentJob?.OrganisationStructure?.Name ?? actor.ActiveJob?.DepartmentName ?? "General Administration";
        var currentPos = currentJob?.JobPositions.OrderBy(p => p.FromDate).LastOrDefault()?.Position?.Name ?? actor.ActiveJob?.PositionName ?? "Staff Member";

        var daysTotal = Math.Max(1, (int)(TodayMaldives - earliestJoin.Date).TotalDays);
        var years = daysTotal / 365;
        var months = (daysTotal % 365) / 30;
        var durationStr = years > 0 ? $"{years} year(s), {months} month(s)" : $"{months} month(s)";

        var docNumber = $"DMS-ISS-{org}-{DateTime.UtcNow:yyyyMMdd}-{targetIndividualId}";
        var issueDateStr = TodayMaldives.ToString("dd MMM yyyy");
        var safePurpose = string.IsNullOrWhiteSpace(purpose) ? "Official Verification" : purpose.Trim();
        var safeAddressedTo = string.IsNullOrWhiteSpace(addressedTo) ? "To Whom It May Concern" : addressedTo.Trim();
        var sigName = tpl.SignatoryName ?? "Director of Human Resources";
        var sigTitle = tpl.SignatoryTitle ?? "Human Resources Division";

        var tokenMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["EmployeeName"] = fullName,
            ["NationalId"] = nationalId,
            ["StaffNumber"] = staffNumber,
            ["CurrentPosition"] = currentPos,
            ["DepartmentName"] = deptName,
            ["OrganisationName"] = orgName,
            ["EmploymentStartDate"] = earliestJoin.ToString("dd MMM yyyy"),
            ["ServiceDuration"] = durationStr,
            ["DocumentNumber"] = docNumber,
            ["IssueDate"] = issueDateStr,
            ["Purpose"] = safePurpose,
            ["AddressedTo"] = safeAddressedTo,
            ["SignatoryName"] = sigName,
            ["SignatoryTitle"] = sigTitle
        };

        string ReplacePlaceholders(string templateText) =>
            PlaceholderRegex.Replace(templateText ?? string.Empty, match =>
            {
                var key = match.Groups[1].Value;
                return tokenMap.TryGetValue(key, out var val) ? val : match.Value;
            });

        var resolvedTitle = ReplacePlaceholders(tpl.DocumentTitlePattern);
        var resolvedBody = ReplacePlaceholders(tpl.BodyTemplate);

        var posHistory = new List<DmsTemplatePositionRowDto>();
        foreach (var j in jobs)
        {
            var dName = j.OrganisationStructure?.Name ?? deptName;
            foreach (var jp in j.JobPositions.OrderBy(p => p.FromDate))
            {
                posHistory.Add(new DmsTemplatePositionRowDto
                {
                    PositionTitle = jp.Position?.Name ?? "Position",
                    DepartmentName = dName,
                    StartDate = jp.FromDate.ToString("dd MMM yyyy"),
                    EndDate = jp.ToDate?.ToString("dd MMM yyyy") ?? "Present"
                });
            }
        }

        var verificationToken = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var payloadHash = PrivateSqlDocumentStorageProvider.ComputeSha256Hex(
            Encoding.UTF8.GetBytes($"{docNumber}|{fullName}|{resolvedBody}|{issueDateStr}"));

        return new DmsTemplatePreviewDto
        {
            DocumentTemplateId = tpl.DocumentTemplateId,
            TemplateName = tpl.Name,
            VersionNumber = tpl.VersionNumber,
            OrganisationName = orgName,
            DocumentNumber = docNumber,
            IssueDateFormatted = issueDateStr,
            ResolvedTitle = resolvedTitle,
            HeaderText = tpl.HeaderText,
            ResolvedBodyText = resolvedBody,
            FooterText = tpl.FooterText,
            SignatoryName = sigName,
            SignatoryTitle = sigTitle,
            EmployeeFullName = fullName,
            EmployeeNationalId = nationalId,
            StaffNumber = staffNumber,
            CurrentPositionTitle = currentPos,
            CurrentDepartmentName = deptName,
            EmploymentStartDate = earliestJoin.ToString("dd MMM yyyy"),
            VerificationToken = verificationToken,
            Sha256Hash = payloadHash,
            IncludeOrganisationLetterhead = tpl.IncludeOrganisationLetterhead,
            IncludePositionHistoryTable = tpl.IncludePositionHistoryTable,
            IncludeQrVerification = tpl.IncludeQrVerification,
            PositionHistory = posHistory
        };
    }

    public async Task<List<DmsDocumentRequestDto>> GetMyDocumentRequestsAsync()
    {
        var (actor, org, _, _) = await ScopeAsync();
        if (actor.IndividualId <= 0) return [];

        await using var db = await OpenDbAsync(org);
        var rows = await db.DmsDocumentRequests
            .AsNoTracking()
            .Include(r => r.Request)
            .Include(r => r.Organisation)
            .Include(r => r.ApplicantIndividual).ThenInclude(i => i.Staff)
            .Include(r => r.DocumentType)
            .Include(r => r.DocumentTemplate)
            .Where(r => r.ApplicantIndividualId == actor.IndividualId && r.OrganisationId == org)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        return rows.Select(MapRequestToDto).ToList();
    }

    public async Task<List<DmsDocumentRequestDto>> GetAdminDocumentRequestsAsync(int? organisationId = null, string? statusFilter = null)
    {
        var (_, org, isHrOrAdmin, isSupervisor) = await ScopeAsync(organisationId);
        if (!isHrOrAdmin && !isSupervisor) return [];

        await using var db = await OpenDbAsync(org);
        var query = db.DmsDocumentRequests
            .AsNoTracking()
            .Include(r => r.Request)
            .Include(r => r.Organisation)
            .Include(r => r.ApplicantIndividual).ThenInclude(i => i.Staff)
            .Include(r => r.DocumentType)
            .Include(r => r.DocumentTemplate)
            .Where(r => r.OrganisationId == org);

        if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        var rows = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        return rows.Select(MapRequestToDto).ToList();
    }

    public async Task<ServiceResult> SubmitDocumentRequestAsync(DmsDocumentRequestSubmitInput input)
    {
        var (actor, org, isHrOrAdmin, _) = await ScopeAsync();
        var applicantId = input.ApplicantIndividualId is > 0 && isHrOrAdmin
            ? input.ApplicantIndividualId.Value
            : actor.IndividualId;

        if (applicantId <= 0)
            return ServiceResult.Fail("An authenticated employee profile is required to submit a document request.");

        if (string.IsNullOrWhiteSpace(input.Purpose))
            return ServiceResult.Fail("Please enter the purpose for requesting this official document.");

        await using var db = await OpenDbAsync(org);

        var reqTypeId = await EnsureDocumentRequestTypeAsync(db);
        var opLog = await logs.CreateAsync(
            db,
            SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,
            $"Submitted DHR-DMS official document request ({input.RequestKind}) for individual {applicantId}.");

        var centralRequest = await requests.CreateAsync(db, new CreateRequestDto
        {
            RequestTypeId = reqTypeId,
            ApplicantBusinessEntityId = applicantId,
            OrganisationBusinessEntityId = org,
            OperationLogId = opLog.OperationLogId
        });

        var tpl = input.DocumentTemplateId.HasValue
            ? await db.DmsDocumentTemplates.FirstOrDefaultAsync(t => t.DocumentTemplateId == input.DocumentTemplateId.Value)
            : await db.DmsDocumentTemplates.OrderByDescending(t => t.VersionNumber).FirstOrDefaultAsync();

        var docReq = new DmsDocumentRequest
        {
            RequestId = centralRequest.RequestId,
            OrganisationId = org,
            ApplicantIndividualId = applicantId,
            SubjectJobId = actor.ActiveJob?.JobId,
            DmsDocumentTypeId = input.DmsDocumentTypeId ?? tpl?.DmsDocumentTypeId,
            DocumentTemplateId = tpl?.DocumentTemplateId,
            RequestKind = string.IsNullOrWhiteSpace(input.RequestKind) ? "EmploymentVerificationLetter" : input.RequestKind.Trim(),
            Purpose = input.Purpose.Trim(),
            AddressedTo = string.IsNullOrWhiteSpace(input.AddressedTo) ? "To Whom It May Concern" : input.AddressedTo.Trim(),
            EmployeeNotes = input.EmployeeNotes?.Trim(),
            IncludePositionHistory = input.IncludePositionHistory,
            IncludeSalaryDetails = input.IncludeSalaryDetails,
            Status = "Submitted",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actor.UserId
        };

        db.DmsDocumentRequests.Add(docReq);
        await db.SaveChangesAsync();

        return ServiceResult.Ok($"Official document request '{centralRequest.ReferenceNumber}' submitted for HR review.");
    }

    public async Task<ServiceResult> ProcessDocumentRequestActionAsync(DmsDocumentRequestActionInput input)
    {
        var (actor, org, isHrOrAdmin, isSupervisor) = await ScopeAsync();
        await using var db = await OpenDbAsync(org);

        var req = await db.DmsDocumentRequests
            .Include(r => r.Request)
            .Include(r => r.DocumentType)
            .Include(r => r.DocumentTemplate)
            .FirstOrDefaultAsync(r => r.DocumentRequestId == input.DocumentRequestId && r.OrganisationId == org);

        if (req is null)
            return ServiceResult.Fail("Document request not found.");

        var actorName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName;

        if (input.Action.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
        {
            if (req.ApplicantIndividualId != actor.IndividualId && !isHrOrAdmin)
                return ServiceResult.Fail("Only the applicant or HR can cancel this request.");
            if (req.Status == "Issued")
                return ServiceResult.Fail("An already issued official document request cannot be cancelled.");

            req.Status = "Cancelled";
            req.UpdatedAtUtc = DateTime.UtcNow;
            req.Request.RequestStateId = SharedConfig.RequestStates.CANCELLED;
            req.Request.LastStateChangedDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return ServiceResult.Ok("Document request cancelled.");
        }

        if (!isHrOrAdmin && !isSupervisor)
            return ServiceResult.Fail("Only authorised HR reviewers/approvers can process official document requests.");

        switch (input.Action)
        {
            case "StartReview":
                req.Status = "UnderReview";
                req.ReviewedByUserId = actor.UserId;
                req.ReviewedByUserName = actorName;
                req.ReviewedAtUtc = DateTime.UtcNow;
                req.ReviewComments = input.Comments?.Trim();
                req.Request.RequestStateId = SharedConfig.RequestStates.PENDING_REVIEW;
                break;

            case "ReturnForCorrection":
                if (string.IsNullOrWhiteSpace(input.Comments))
                    return ServiceResult.Fail("Please provide comments explaining what clarification is needed.");
                req.Status = "ReturnedForCorrection";
                req.ReviewedByUserId = actor.UserId;
                req.ReviewedByUserName = actorName;
                req.ReviewedAtUtc = DateTime.UtcNow;
                req.ReviewComments = input.Comments.Trim();
                req.Request.RequestStateId = SharedConfig.RequestStates.PENDING_STAFF_COMPLETION;
                break;

            case "Reject":
                if (!isHrOrAdmin)
                    return ServiceResult.Fail("Only HR Approvers can reject an official document request.");
                if (string.IsNullOrWhiteSpace(input.Comments))
                    return ServiceResult.Fail("Rejection reason is required.");
                req.Status = "Rejected";
                req.ReviewComments = input.Comments.Trim();
                req.ReviewedByUserId = actor.UserId;
                req.ReviewedByUserName = actorName;
                req.ReviewedAtUtc = DateTime.UtcNow;
                req.Request.RequestStateId = SharedConfig.RequestStates.REJECTED;
                req.Request.LastStateChangedDate = DateTime.UtcNow;
                break;

            case "ApproveAndIssue":
                // Requirement 10: Do not issue official documents without the applicable approval
                if (!isHrOrAdmin && !actor.HasPermission(Permissions.DocumentApprove))
                    return ServiceResult.Fail("Official documents cannot be issued without HR Approval permission.");

                var preview = await ResolveTemplatePreviewAsync(
                    req.DocumentTemplateId ?? 0,
                    req.ApplicantIndividualId,
                    req.Purpose,
                    req.AddressedTo);

                if (!string.IsNullOrWhiteSpace(input.CustomBodyOverride))
                {
                    preview.ResolvedBodyText = input.CustomBodyOverride.Trim();
                }
                if (!string.IsNullOrWhiteSpace(input.SignatoryName))
                {
                    preview.SignatoryName = input.SignatoryName.Trim();
                }
                if (!string.IsNullOrWhiteSpace(input.SignatoryTitle))
                {
                    preview.SignatoryTitle = input.SignatoryTitle.Trim();
                }

                var snapshotJson = JsonSerializer.Serialize(preview);
                var pdfBytes = input.GeneratedPdfBytes is { Length: > 0 }
                    ? input.GeneratedPdfBytes
                    : BuildFallbackMinimalPdfBytes(preview);

                var docType = req.DocumentType
                              ?? await db.DmsDocumentTypes.FirstOrDefaultAsync(t => t.Code == "EMPLOYMENT_VERIFICATION_LETTER")
                              ?? await db.DmsDocumentTypes.FirstAsync();

                var stored = await storage.StoreAsync(new DocumentStorageWriteRequest
                {
                    OrganisationId = org,
                    OriginalFileName = $"{preview.DocumentNumber}.pdf",
                    DeclaredContentType = "application/pdf",
                    Content = pdfBytes,
                    PermittedExtensionsCsv = ".pdf",
                    MaxFileSizeMb = 15,
                    ActorUserId = actor.UserId
                });

                if (!stored.Success)
                    return ServiceResult.Fail(stored.ErrorMessage ?? "Failed to store generated official PDF.");

                var issuedDoc = new DmsDocument
                {
                    DocumentNumber = preview.DocumentNumber,
                    OrganisationId = org,
                    SubjectIndividualId = req.ApplicantIndividualId,
                    SubjectJobId = req.SubjectJobId,
                    CategoryId = docType.CategoryId,
                    DmsDocumentTypeId = docType.DmsDocumentTypeId,
                    Title = preview.ResolvedTitle,
                    Description = $"Issued from Official Document Request {req.Request.ReferenceNumber} ({req.Purpose}).",
                    ReferenceCode = req.Request.ReferenceNumber,
                    Classification = "Confidential",
                    AccessScope = "EmployeeAndHr",
                    Status = "OfficialRecord",
                    CurrentVersionNumber = 1,
                    IsOfficialRecord = true,
                    IsImmutable = true,
                    IsVerified = true,
                    VerifiedByUserId = actor.UserId,
                    VerifiedByUserName = actorName,
                    VerifiedAtUtc = DateTime.UtcNow,
                    ApprovedByUserId = actor.UserId,
                    ApprovedByUserName = actorName,
                    ApprovedAtUtc = DateTime.UtcNow,
                    DocumentDate = DateTime.UtcNow,
                    LinkedRequestId = req.RequestId,
                    RetentionPolicyId = docType.RetentionPolicyId,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = actor.UserId,
                    CreatedByUserName = actorName
                };

                db.DmsDocuments.Add(issuedDoc);
                await db.SaveChangesAsync();

                var issuedVer = new DmsDocumentVersion
                {
                    DocumentId = issuedDoc.DocumentId,
                    VersionNumber = 1,
                    VersionLabel = "v1.0 (Official Issued)",
                    StoredObjectId = stored.StoredObjectId,
                    OriginalFileName = stored.SafeFileName,
                    ContentType = "application/pdf",
                    FileExtension = ".pdf",
                    FileSizeBytes = stored.FileSizeBytes,
                    Sha256Hash = stored.Sha256Hash,
                    ChangeComments = $"Approved and issued against request {req.Request.ReferenceNumber}.",
                    ContentSnapshotJson = snapshotJson,
                    IsCurrentVersion = true,
                    IsOfficialSnapshot = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = actor.UserId,
                    CreatedByUserName = actorName
                };

                db.DmsDocumentVersions.Add(issuedVer);
                await db.SaveChangesAsync();

                issuedDoc.CurrentVersionId = issuedVer.DocumentVersionId;

                // Link to both Staff and Request records
                db.DmsDocumentLinks.Add(new DmsDocumentLink
                {
                    DocumentId = issuedDoc.DocumentId,
                    OrganisationId = org,
                    TargetEntityType = "Staff",
                    TargetEntityId = req.ApplicantIndividualId,
                    TargetReferenceCode = preview.StaffNumber,
                    TargetDisplayName = preview.EmployeeFullName,
                    LinkRelationship = "GeneratedOutput",
                    LinkedAtUtc = DateTime.UtcNow,
                    LinkedByUserId = actor.UserId,
                    LinkedByUserName = actorName
                });

                db.DmsDocumentLinks.Add(new DmsDocumentLink
                {
                    DocumentId = issuedDoc.DocumentId,
                    OrganisationId = org,
                    TargetEntityType = "Request",
                    TargetEntityId = req.RequestId,
                    TargetReferenceCode = req.Request.ReferenceNumber,
                    TargetDisplayName = $"Document Request {req.Request.ReferenceNumber}",
                    LinkRelationship = "GeneratedOutput",
                    LinkedAtUtc = DateTime.UtcNow,
                    LinkedByUserId = actor.UserId,
                    LinkedByUserName = actorName
                });

                var sigDigest = Convert.ToHexString(SHA256.HashData(
                    Encoding.UTF8.GetBytes($"{issuedDoc.DocumentNumber}|{stored.Sha256Hash}|{actor.UserId}"))).ToLowerInvariant();

                db.DmsDocumentSignatures.Add(new DmsDocumentSignature
                {
                    DocumentId = issuedDoc.DocumentId,
                    DocumentVersionId = issuedVer.DocumentVersionId,
                    OrganisationId = org,
                    SignerUserId = actor.UserId,
                    SignerName = preview.SignatoryName,
                    SignerTitle = preview.SignatoryTitle,
                    SignatureRole = "IssuingAuthority",
                    SignatureStatus = "Signed",
                    ContentSha256AtSigning = stored.Sha256Hash,
                    SignatureDigest = sigDigest,
                    Remarks = input.Comments?.Trim(),
                    SignedAtUtc = DateTime.UtcNow
                });

                db.DmsDocumentEvents.Add(new DmsDocumentEvent
                {
                    DocumentId = issuedDoc.DocumentId,
                    DocumentVersionId = issuedVer.DocumentVersionId,
                    OrganisationId = org,
                    EventType = "Approved",
                    EventDescription = $"Approved and issued immutable official document '{issuedDoc.DocumentNumber}' for request '{req.Request.ReferenceNumber}'.",
                    PerformedByUserId = actor.UserId,
                    PerformedByUserName = actorName,
                    OccurredAtUtc = DateTime.UtcNow
                });

                req.Status = "Issued";
                req.ResolvedSnapshotJson = snapshotJson;
                req.GeneratedBodyText = preview.ResolvedBodyText;
                req.IssuedDocumentId = issuedDoc.DocumentId;
                req.IssuedDocumentNumber = issuedDoc.DocumentNumber;
                req.VerificationToken = preview.VerificationToken;
                req.ApprovedByUserId = actor.UserId;
                req.ApprovedByUserName = actorName;
                req.ApprovedAtUtc = DateTime.UtcNow;
                req.IssuedAtUtc = DateTime.UtcNow;
                req.ReviewComments = input.Comments?.Trim();
                req.Request.RequestStateId = SharedConfig.RequestStates.APPROVED;
                req.Request.LastStateChangedDate = DateTime.UtcNow;
                break;

            default:
                return ServiceResult.Fail($"Unsupported request action '{input.Action}'.");
        }

        req.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Document request action '{input.Action}' completed.");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static DmsDocumentSummaryDto MapToSummaryDto(DmsDocument d, string orgName)
    {
        var curVer = d.Versions.FirstOrDefault(v => v.IsCurrentVersion)
                     ?? d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        return new DmsDocumentSummaryDto
        {
            DocumentId = d.DocumentId,
            DocumentNumber = d.DocumentNumber,
            OrganisationId = d.OrganisationId,
            OrganisationName = orgName,
            DepartmentStructureId = d.DepartmentStructureId,
            SubjectIndividualId = d.SubjectIndividualId,
            SubjectEmployeeName = d.SubjectIndividual != null ? FullName(d.SubjectIndividual) : null,
            SubjectStaffNumber = d.SubjectIndividual?.Staff?.EmployeeNumber,
            CategoryId = d.CategoryId,
            CategoryName = d.Category?.Name ?? "General",
            TargetDomain = d.Category?.TargetDomain ?? "General",
            DmsDocumentTypeId = d.DmsDocumentTypeId,
            DocumentTypeName = d.DocumentType?.Name ?? "Document",
            Title = d.Title,
            Description = d.Description,
            ReferenceCode = d.ReferenceCode,
            Classification = d.Classification,
            AccessScope = d.AccessScope,
            Status = d.Status,
            CurrentVersionNumber = d.CurrentVersionNumber,
            CurrentFileName = curVer?.OriginalFileName ?? "document.pdf",
            CurrentFileExtension = curVer?.FileExtension ?? ".pdf",
            CurrentContentType = curVer?.ContentType ?? "application/pdf",
            CurrentFileSizeBytes = curVer?.FileSizeBytes ?? 0,
            CurrentSha256Hash = curVer?.Sha256Hash ?? string.Empty,
            IsOfficialRecord = d.IsOfficialRecord,
            IsImmutable = d.IsImmutable,
            IsLockedForEdit = d.IsLockedForEdit,
            CheckedOutByUserName = d.CheckedOutByUserName,
            IsVerified = d.IsVerified,
            VerifiedByUserName = d.VerifiedByUserName,
            VerifiedAtUtc = d.VerifiedAtUtc,
            ApprovedByUserName = d.ApprovedByUserName,
            ApprovedAtUtc = d.ApprovedAtUtc,
            HasActiveLegalHold = d.HasActiveLegalHold,
            DocumentDate = d.DocumentDate,
            ExpiryDate = d.ExpiryDate,
            RetentionDueDateUtc = d.RetentionDueDateUtc,
            Tags = d.Tags,
            LinksCount = d.Links.Count(l => l.IsActive),
            SignaturesCount = d.Signatures.Count,
            CreatedAtUtc = d.CreatedAtUtc,
            CreatedByUserName = d.CreatedByUserName
        };
    }

    private static DmsDocumentRequestDto MapRequestToDto(DmsDocumentRequest r) => new()
    {
        DocumentRequestId = r.DocumentRequestId,
        RequestId = r.RequestId,
        ReferenceNumber = r.Request?.ReferenceNumber ?? $"REQ-{r.RequestId}",
        OrganisationId = r.OrganisationId,
        OrganisationName = r.Organisation?.OrganisationName ?? $"Organisation #{r.OrganisationId}",
        ApplicantIndividualId = r.ApplicantIndividualId,
        ApplicantName = FullName(r.ApplicantIndividual),
        ApplicantStaffNumber = r.ApplicantIndividual?.Staff?.EmployeeNumber ?? $"EMP-{r.ApplicantIndividualId}",
        DmsDocumentTypeId = r.DmsDocumentTypeId,
        DocumentTypeName = r.DocumentType?.Name,
        DocumentTemplateId = r.DocumentTemplateId,
        TemplateName = r.DocumentTemplate?.Name,
        RequestKind = r.RequestKind,
        Purpose = r.Purpose,
        AddressedTo = r.AddressedTo,
        EmployeeNotes = r.EmployeeNotes,
        IncludePositionHistory = r.IncludePositionHistory,
        IncludeSalaryDetails = r.IncludeSalaryDetails,
        Status = r.Status,
        ResolvedSnapshotJson = r.ResolvedSnapshotJson,
        GeneratedBodyText = r.GeneratedBodyText,
        IssuedDocumentId = r.IssuedDocumentId,
        IssuedDocumentNumber = r.IssuedDocumentNumber,
        VerificationToken = r.VerificationToken,
        ReviewedByUserName = r.ReviewedByUserName,
        ReviewedAtUtc = r.ReviewedAtUtc,
        ReviewComments = r.ReviewComments,
        ApprovedByUserName = r.ApprovedByUserName,
        ApprovedAtUtc = r.ApprovedAtUtc,
        IssuedAtUtc = r.IssuedAtUtc,
        CreatedAtUtc = r.CreatedAtUtc
    };

    private static List<string> ValidateSafeTemplatePlaceholders(string templateText)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EmployeeName",
            "NationalId",
            "StaffNumber",
            "CurrentPosition",
            "DepartmentName",
            "OrganisationName",
            "EmploymentStartDate",
            "ServiceDuration",
            "DocumentNumber",
            "IssueDate",
            "Purpose",
            "AddressedTo",
            "SignatoryName",
            "SignatoryTitle"
        };

        var invalid = new List<string>();
        foreach (Match m in PlaceholderRegex.Matches(templateText ?? string.Empty))
        {
            var token = m.Groups[1].Value;
            if (!allowed.Contains(token) && !invalid.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                invalid.Add(token);
            }
        }
        return invalid;
    }

    /// <summary>
    /// Builds a valid minimal PDF-1.4 document when server-side fallback generation is invoked without UI MigraDoc bytes.
    /// </summary>
    private static byte[] BuildFallbackMinimalPdfBytes(DmsTemplatePreviewDto preview)
    {
        var safeText = (preview.ResolvedTitle + " - " + preview.DocumentNumber + " - " + preview.EmployeeFullName)
            .Replace("(", "[")
            .Replace(")", "]");

        var pdf = $"""
            %PDF-1.4
            1 0 obj
            << /Type /Catalog /Pages 2 0 R >>
            endobj
            2 0 obj
            << /Type /Pages /Kids [3 0 R] /Count 1 >>
            endobj
            3 0 obj
            << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>
            endobj
            4 0 obj
            << /Length {safeText.Length + 45} >>
            stream
            BT /F1 11 Tf 50 780 Td ({safeText}) Tj ET
            endstream
            endobj
            5 0 obj
            << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>
            endobj
            xref
            0 6
            0000000000 65535 f 
            trailer
            << /Size 6 /Root 1 0 R >>
            startxref
            0
            %%EOF
            """;

        return Encoding.ASCII.GetBytes(pdf);
    }
}
