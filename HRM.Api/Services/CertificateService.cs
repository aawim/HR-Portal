using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HRM.Authorization;
using HRM.Constants;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Certificates;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Api.Services;

public sealed class CertificateService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService logs,
    IRequestService requests,
    ILogger<CertificateService> logger)
    : CertificateServiceContracts.IOperations
{
    private static DateTime TodayMaldives => DateTime.UtcNow.AddHours(5).Date;

    private async Task<(UserContextDto Actor, int Org, bool IsHrOrAdmin)> ScopeAsync(int? selectedOrgId = null)
    {
        var actor = await access.RequireContextAsync();
        var currentOrg = await access.GetCurrentOrganisationIdAsync();
        var isSuper = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var org = isSuper && selectedOrgId is > 0 ? selectedOrgId.Value : currentOrg;
        if (org <= 0)
            throw new UnauthorizedAccessException("Please select an organisation context.");

        var isHrOrAdmin = isSuper || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager ||
                          actor.IsOrganisationAdmin || await access.HasPermissionAsync("Admin") ||
                          await access.HasPermissionAsync(Permissions.CertificateReview) ||
                          await access.HasPermissionAsync(Permissions.CertificateApprove);

        return (actor, org, isHrOrAdmin);
    }

    private static string FullName(Individual i) =>
        string.Join(" ", new[] { i.FirstNameEnglish, i.MiddleNameEnglish, i.LastNameEnglish }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));

    public async Task<List<CertificateRequestSummaryDto>> GetMyRequestsAsync()
    {
        var actor = await access.GetContextAsync();
        if (actor == null || actor.IndividualId <= 0) return [];

        await using var db = await factory.CreateDbContextAsync();
        var currentOrg = await access.GetCurrentOrganisationIdAsync();

        var rows = await db.CertificateRequests
            .AsNoTracking()
            .Include(r => r.Request)
            .Include(r => r.Individual)
            .Include(r => r.IssuedCertificates)
            .Where(r => r.IndividualId == actor.IndividualId && (currentOrg <= 0 || r.OrganisationId == currentOrg))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rows.Select(MapToSummary).ToList();
    }

    public async Task<List<IssuedCertificateDetailDto>> GetMyIssuedCertificatesAsync()
    {
        var actor = await access.GetContextAsync();
        if (actor == null || actor.IndividualId <= 0) return [];

        await using var db = await factory.CreateDbContextAsync();
        var currentOrg = await access.GetCurrentOrganisationIdAsync();

        var rows = await db.IssuedCertificates
            .AsNoTracking()
            .Include(c => c.CertificateRequest)
            .Include(c => c.Individual)
            .Where(c => c.IndividualId == actor.IndividualId && (currentOrg <= 0 || c.OrganisationId == currentOrg))
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync();

        var result = new List<IssuedCertificateDetailDto>();
        foreach (var c in rows)
        {
            var snap = DeserializeSnapshot(c.SnapshotJson);
            result.Add(new IssuedCertificateDetailDto
            {
                IssuedCertificateId = c.IssuedCertificateId,
                CertificateRequestId = c.CertificateRequestId,
                CertificateNumber = c.CertificateNumber,
                VerificationToken = c.VerificationToken,
                IndividualId = c.IndividualId,
                EmployeeName = FullName(c.Individual),
                CertificateType = c.CertificateType,
                IssueDate = c.IssueDate,
                SignatoryName = c.SignatoryName,
                SignatoryTitle = c.SignatoryTitle,
                IssuingOfficerName = c.IssuingOfficerName,
                Status = c.Status,
                RevokedReason = c.RevokedReason,
                RevokedAt = c.RevokedAt,
                Sha256Hash = c.Sha256Hash,
                HasPdf = c.PdfContent != null && c.PdfContent.Length > 0,
                Snapshot = snap
            });
        }
        return result;
    }

    public async Task<CertificateVerifiedEmploymentHistoryDto> GetVerifiedEmploymentHistoryAsync(int? individualId = null)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        var targetIndividualId = individualId.HasValue && individualId.Value > 0 ? individualId.Value : actor.IndividualId;

        if (targetIndividualId != actor.IndividualId && !isHrOrAdmin)
            throw new UnauthorizedAccessException("You are not authorised to view employment history for another staff member.");

        await using var db = await factory.CreateDbContextAsync();

        var individual = await db.Individuals
            .AsNoTracking()
            .Include(i => i.Staff)
            .FirstOrDefaultAsync(i => i.BusinessEntityId == targetIndividualId)
            ?? throw new InvalidOperationException("Individual record not found.");

        var jobs = await db.Jobs
            .AsNoTracking()
            .Include(j => j.OrganisationStructure)
            .Include(j => j.JobPositions)
                .ThenInclude(jp => jp.Position)
            .Where(j => j.IndividualID == targetIndividualId && j.OrganisationID == org)
            .OrderBy(j => j.JoinedDate)
            .ToListAsync();

        var employeeNumber = individual.Staff?.EmployeeNumber ?? targetIndividualId.ToString();
        var fullName = FullName(individual);
        var orgName = (await db.Organisations.AsNoTracking().FirstOrDefaultAsync(o => o.BusinessEntityID == org))?.OrganisationName ?? "Official Organisation";

        if (jobs.Count == 0)
        {
            return new CertificateVerifiedEmploymentHistoryDto
            {
                IndividualId = targetIndividualId,
                EmployeeNumber = employeeNumber,
                FullName = fullName,
                OrganisationName = orgName,
                JoinedDate = TodayMaldives,
                IsCurrentlyEmployed = false,
                FormattedServiceDuration = "0 days"
            };
        }

        var earliestJoined = jobs.Min(j => j.JoinedDate);
        var hasActiveJob = jobs.Any(j => j.JobStateId == 4 && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value.Date >= TodayMaldives));
        DateTime? latestTerminated = hasActiveJob ? null : jobs.Max(j => j.TerminatedDate);

        var durationEnd = latestTerminated ?? TodayMaldives;
        var (years, months, days) = CalculatePreciseServiceDuration(earliestJoined, durationEnd);
        var durationText = FormatDuration(years, months, days);

        var currentJob = jobs.FirstOrDefault(j => j.JobStateId == 4 && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value.Date >= TodayMaldives)) ?? jobs.Last();
        var department = currentJob.OrganisationStructure?.Name ?? "General Operations";

        var currentJobPosition = currentJob.JobPositions.FirstOrDefault(jp => jp.JobPositionStateId == 4 && (!jp.ToDate.HasValue || jp.ToDate.Value.Date >= TodayMaldives)) ?? currentJob.JobPositions.LastOrDefault();
        var currentJobTitle = currentJobPosition?.Position?.Name ?? "Staff Member";
        var currentSalary = currentJob.BasicSalary;

        // Collect all verified position assignments
        var allPositions = new List<CertificatePositionItemDto>();
        foreach (var j in jobs)
        {
            var dept = j.OrganisationStructure?.Name ?? department;
            foreach (var jp in j.JobPositions.OrderBy(p => p.FromDate))
            {
                var isCurrent = jp.JobPositionStateId == 4 && (!jp.ToDate.HasValue || jp.ToDate.Value.Date >= TodayMaldives);
                var responsibilities = ExtractResponsibilities(jp.Position?.Descriptions);

                allPositions.Add(new CertificatePositionItemDto
                {
                    JobPositionId = jp.JobPositionId,
                    PositionId = jp.PositionId,
                    PositionTitle = jp.Position?.Name ?? "Designation",
                    Department = dept,
                    FromDate = jp.FromDate,
                    ToDate = jp.ToDate,
                    IsCurrent = isCurrent,
                    OfficialResponsibilities = responsibilities
                });
            }
        }

        // Collect verified project participation
        var projects = new List<CertificateProjectItemDto>();
        try
        {
            var projectMembers = await db.Set<ProjectMember>()
                .AsNoTracking()
                .Include(pm => pm.Project)
                .Where(pm => pm.StaffId == targetIndividualId && (pm.Project == null || pm.Project.OrganisationId == org))
                .OrderBy(pm => pm.AssignedDate)
                .ToListAsync();

            foreach (var pm in projectMembers)
            {
                if (pm.Project == null) continue;
                projects.Add(new CertificateProjectItemDto
                {
                    ProjectId = pm.ProjectId,
                    ProjectName = pm.Project.Name,
                    ProjectReference = pm.Project.Reference,
                    Role = string.IsNullOrWhiteSpace(pm.Role) ? "Team Member" : pm.Role,
                    AssignedDate = pm.AssignedDate,
                    EndDate = pm.ExpectedEndDate ?? pm.Project.ActualEndDate,
                    ProjectStatus = pm.Project.Status,
                    Remarks = pm.Remarks
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not query PR_ProjectMembers for individual {IndividualId}", targetIndividualId);
        }

        return new CertificateVerifiedEmploymentHistoryDto
        {
            IndividualId = targetIndividualId,
            EmployeeNumber = employeeNumber,
            FullName = fullName,
            JoinedDate = earliestJoined,
            TerminatedDate = latestTerminated,
            IsCurrentlyEmployed = hasActiveJob,
            TotalServiceYears = years,
            TotalServiceMonths = months,
            TotalServiceDays = days,
            FormattedServiceDuration = durationText,
            OrganisationName = orgName,
            DepartmentName = department,
            CurrentJobTitle = currentJobTitle,
            CurrentBasicSalary = currentSalary,
            Positions = allPositions,
            Projects = projects
        };
    }

    public async Task<ServiceResult> SubmitCertificateRequestAsync(CertificateRequestInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();

        var targetIndividualId = input.IndividualId > 0 ? input.IndividualId : actor.IndividualId;
        if (targetIndividualId != actor.IndividualId && !isHrOrAdmin)
            return ServiceResult.Fail("Employees are not permitted to submit certificate requests for other individuals.");

        if (string.IsNullOrWhiteSpace(input.Purpose))
            return ServiceResult.Fail("Purpose of the certificate request is required.");

        await using var db = await factory.CreateDbContextAsync();

        var individual = await db.Individuals
            .AsNoTracking()
            .Include(i => i.Staff)
            .FirstOrDefaultAsync(i => i.BusinessEntityId == targetIndividualId);

        if (individual == null)
            return ServiceResult.Fail("Staff record not found.");

        // Create unified Request record
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,
            $"Employment certificate request submitted for staff #{targetIndividualId} ({input.CertificateType}).");

        var unifiedReq = await requests.CreateAsync(db, new CreateRequestDto
        {
            RequestTypeId = SharedConfig.RequestTypes.EMPLOYMENT_CERTIFICATE_REQUEST,
            ApplicantBusinessEntityId = targetIndividualId,
            OrganisationBusinessEntityId = org,
            EffectiveDate = TodayMaldives,
            OperationLogId = audit.OperationLogId
        });

        // Set state to PENDING_HR_VERIFICATION
        unifiedReq.RequestStateId = SharedConfig.RequestStates.PENDING_HR_VERIFICATION;
        unifiedReq.ReferenceNumber = $"CR-{org}-{unifiedReq.RequestId:D5}";

        var certRequest = new CertificateRequest
        {
            RequestId = unifiedReq.RequestId,
            IndividualId = targetIndividualId,
            OrganisationId = org,
            CertificateType = string.IsNullOrWhiteSpace(input.CertificateType) ? "StandardService" : input.CertificateType,
            Purpose = input.Purpose.Trim(),
            PurposeDetails = input.PurposeDetails?.Trim(),
            RequestScope = string.IsNullOrWhiteSpace(input.RequestScope) ? "FullCareer" : input.RequestScope,
            SpecificPeriodStart = input.SpecificPeriodStart,
            SpecificPeriodEnd = input.SpecificPeriodEnd,
            SelectedPositionIdsJson = input.SelectedPositionIds != null ? JsonSerializer.Serialize(input.SelectedPositionIds) : null,
            SelectedProjectIdsJson = input.SelectedProjectIds != null ? JsonSerializer.Serialize(input.SelectedProjectIds) : null,
            IncludeResponsibilities = input.IncludeResponsibilities,
            IncludeProjects = input.IncludeProjects,
            IncludeSalary = input.IncludeSalary,
            AddressedTo = string.IsNullOrWhiteSpace(input.AddressedTo) ? "To Whom It May Concern" : input.AddressedTo.Trim(),
            EmployeeComments = input.EmployeeComments?.Trim(),
            Status = "Submitted",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = actor.UserId
        };

        db.CertificateRequests.Add(certRequest);

        // Audit log
        db.CertificateAuditLogs.Add(new CertificateAuditLog
        {
            CertificateRequest = certRequest,
            OrganisationId = org,
            Action = "Submitted",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actor.FullName,
            Details = $"Certificate request submitted for {certRequest.CertificateType}. Purpose: {certRequest.Purpose}",
            Timestamp = DateTime.UtcNow
        });

        // Send notification to HR officers (Strictly distinct recipients, strictly excluding actor.UserId to prevent duplicate self-notification)
        await NotifyHrOfficersAsync(db, org, actor.UserId, $"New {certRequest.CertificateType} request submitted by {FullName(individual)} (#{individual.Staff?.EmployeeNumber ?? targetIndividualId.ToString()}).", $"/admin/hr/requests");

        await db.SaveChangesAsync();

        return ServiceResult.Ok("Your certificate request has been submitted successfully for HR review.");
    }

    public async Task<ServiceResult> CancelCertificateRequestAsync(int certificateRequestId)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var certReq = await db.CertificateRequests
            .Include(r => r.Request)
            .FirstOrDefaultAsync(r => r.CertificateRequestId == certificateRequestId && r.OrganisationId == org);

        if (certReq == null)
            return ServiceResult.Fail("Certificate request not found.");

        if (certReq.IndividualId != actor.IndividualId && !isHrOrAdmin)
            return ServiceResult.Fail("You are not authorised to cancel this request.");

        if (certReq.Status is "Approved" or "Issued")
            return ServiceResult.Fail("Approved or issued certificates cannot be cancelled. Contact HR to request revocation if necessary.");

        certReq.Status = "Cancelled";
        certReq.UpdatedAt = DateTime.UtcNow;
        certReq.UpdatedByUserId = actor.UserId;

        if (certReq.Request != null)
        {
            certReq.Request.RequestStateId = SharedConfig.RequestStates.CANCELLED;
            certReq.Request.LastStateChangedDate = DateTime.UtcNow;
            certReq.Request.LastStateChangedByUserId = actor.UserId;
            certReq.Request.StateChangeRemarks = "Cancelled by applicant.";
        }

        db.CertificateAuditLogs.Add(new CertificateAuditLog
        {
            CertificateRequestId = certReq.CertificateRequestId,
            OrganisationId = org,
            Action = "Cancelled",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actor.FullName,
            Details = "Request cancelled by user.",
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Certificate request cancelled.");
    }

    public async Task<List<CertificateRequestSummaryDto>> GetAdminPendingRequestsAsync(int? organisationId = null, string? statusFilter = null)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync(organisationId);
        if (!isHrOrAdmin)
            throw new UnauthorizedAccessException("HR administrative access is required to view certificate queues.");

        await using var db = await factory.CreateDbContextAsync();

        var query = db.CertificateRequests
            .AsNoTracking()
            .Include(r => r.Request)
            .Include(r => r.Individual)
                .ThenInclude(i => i.Staff)
            .Include(r => r.IssuedCertificates)
            .Where(r => r.OrganisationId == org);

        if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (statusFilter.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => r.Status == "Submitted" || r.Status == "UnderReview");
            else
                query = query.Where(r => r.Status == statusFilter);
        }

        var rows = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return rows.Select(MapToSummary).ToList();
    }

    public async Task<CertificateRequestDetailDto> GetRequestDetailAsync(int certificateRequestId)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();

        var certReq = await db.CertificateRequests
            .AsNoTracking()
            .Include(r => r.Request)
            .Include(r => r.Individual)
                .ThenInclude(i => i.Staff)
            .Include(r => r.IssuedCertificates)
            .Include(r => r.AuditLogs)
            .FirstOrDefaultAsync(r => r.CertificateRequestId == certificateRequestId && r.OrganisationId == org)
            ?? throw new InvalidOperationException("Certificate request not found.");

        if (certReq.IndividualId != actor.IndividualId && !isHrOrAdmin)
            throw new UnauthorizedAccessException("You are not authorised to inspect this certificate request.");

        var history = await GetVerifiedEmploymentHistoryAsync(certReq.IndividualId);

        var summary = MapToSummary(certReq);

        IssuedCertificateDetailDto? issuedDto = null;
        var activeIssued = certReq.IssuedCertificates.OrderByDescending(c => c.IssueDate).FirstOrDefault();
        if (activeIssued != null)
        {
            var snap = DeserializeSnapshot(activeIssued.SnapshotJson);
            issuedDto = new IssuedCertificateDetailDto
            {
                IssuedCertificateId = activeIssued.IssuedCertificateId,
                CertificateRequestId = activeIssued.CertificateRequestId,
                CertificateNumber = activeIssued.CertificateNumber,
                VerificationToken = activeIssued.VerificationToken,
                IndividualId = activeIssued.IndividualId,
                EmployeeName = FullName(certReq.Individual),
                CertificateType = activeIssued.CertificateType,
                IssueDate = activeIssued.IssueDate,
                SignatoryName = activeIssued.SignatoryName,
                SignatoryTitle = activeIssued.SignatoryTitle,
                IssuingOfficerName = activeIssued.IssuingOfficerName,
                Status = activeIssued.Status,
                RevokedReason = activeIssued.RevokedReason,
                RevokedAt = activeIssued.RevokedAt,
                Sha256Hash = activeIssued.Sha256Hash,
                HasPdf = activeIssued.PdfContent != null && activeIssued.PdfContent.Length > 0,
                Snapshot = snap
            };
        }

        // Draft preview snapshot
        var draftSnapshot = BuildSnapshot(certReq, history, actor.FullName, "Human Resources Officer", org);

        var audits = certReq.AuditLogs.OrderByDescending(a => a.Timestamp).Select(a => new CertificateAuditLogDto
        {
            CertificateAuditLogId = a.CertificateAuditLogId,
            Action = a.Action,
            PerformedByUserName = a.PerformedByUserName,
            Details = a.Details,
            Timestamp = a.Timestamp
        }).ToList();

        return new CertificateRequestDetailDto
        {
            Summary = summary,
            EmploymentHistory = history,
            CurrentDraftSnapshot = draftSnapshot,
            IssuedCertificate = issuedDto,
            AuditTrail = audits
        };
    }

    public async Task<ServiceResult> ReviewActionAsync(CertificateReviewActionInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin)
            return ServiceResult.Fail("HR administrative permissions are required to review certificate requests.");

        await using var db = await factory.CreateDbContextAsync();

        var certReq = await db.CertificateRequests
            .Include(r => r.Request)
            .Include(r => r.Individual)
                .ThenInclude(i => i.Staff)
            .Include(r => r.IssuedCertificates)
            .FirstOrDefaultAsync(r => r.CertificateRequestId == input.CertificateRequestId && r.OrganisationId == org);

        if (certReq == null)
            return ServiceResult.Fail("Certificate request not found.");

        var action = input.Action.Trim();

        // Apply corrections if supplied by HR
        if (!string.IsNullOrWhiteSpace(input.CorrectedAddressedTo))
            certReq.AddressedTo = input.CorrectedAddressedTo.Trim();
        if (input.IncludeResponsibilities.HasValue)
            certReq.IncludeResponsibilities = input.IncludeResponsibilities.Value;
        if (input.IncludeProjects.HasValue)
            certReq.IncludeProjects = input.IncludeProjects.Value;
        if (input.IncludeSalary.HasValue)
            certReq.IncludeSalary = input.IncludeSalary.Value;
        if (input.SpecificPeriodStart.HasValue)
            certReq.SpecificPeriodStart = input.SpecificPeriodStart.Value;
        if (input.SpecificPeriodEnd.HasValue)
            certReq.SpecificPeriodEnd = input.SpecificPeriodEnd.Value;

        if (action.Equals("StartReview", StringComparison.OrdinalIgnoreCase))
        {
            certReq.Status = "UnderReview";
            certReq.ReviewedByUserId = actor.UserId;
            certReq.ReviewedAt = DateTime.UtcNow;
            if (certReq.Request != null)
                certReq.Request.RequestStateId = SharedConfig.RequestStates.PENDING_REVIEW;

            db.CertificateAuditLogs.Add(new CertificateAuditLog
            {
                CertificateRequestId = certReq.CertificateRequestId,
                OrganisationId = org,
                Action = "UnderReview",
                PerformedByUserId = actor.UserId,
                PerformedByUserName = actor.FullName,
                Details = "HR review initiated.",
                Timestamp = DateTime.UtcNow
            });
        }
        else if (action.Equals("Return", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(input.Reason))
                return ServiceResult.Fail("Please provide a reason when returning the request for clarification.");

            certReq.Status = "ReturnedForClarification";
            certReq.ReturnedReason = input.Reason.Trim();
            certReq.ReviewedByUserId = actor.UserId;
            certReq.ReviewedAt = DateTime.UtcNow;
            if (certReq.Request != null)
            {
                certReq.Request.RequestStateId = SharedConfig.RequestStates.PENDING_STAFF_COMPLETION;
                certReq.Request.StateChangeRemarks = certReq.ReturnedReason;
            }

            db.CertificateAuditLogs.Add(new CertificateAuditLog
            {
                CertificateRequestId = certReq.CertificateRequestId,
                OrganisationId = org,
                Action = "Returned",
                PerformedByUserId = actor.UserId,
                PerformedByUserName = actor.FullName,
                Details = $"Returned for clarification: {certReq.ReturnedReason}",
                Timestamp = DateTime.UtcNow
            });

            // Single notification to employee (if employee is not the acting user)
            await NotifyEmployeeAsync(db, certReq.IndividualId, org, actor.UserId,
                $"Your certificate request #{certReq.Request?.ReferenceNumber ?? certReq.CertificateRequestId.ToString()} was returned for clarification: {certReq.ReturnedReason}",
                "/online/certificates");
        }
        else if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(input.Reason))
                return ServiceResult.Fail("Please provide a reason for rejecting the certificate request.");

            certReq.Status = "Rejected";
            certReq.RejectionReason = input.Reason.Trim();
            certReq.ReviewedByUserId = actor.UserId;
            certReq.ReviewedAt = DateTime.UtcNow;
            if (certReq.Request != null)
            {
                certReq.Request.RequestStateId = SharedConfig.RequestStates.REJECTED;
                certReq.Request.StateChangeRemarks = certReq.RejectionReason;
            }

            db.CertificateAuditLogs.Add(new CertificateAuditLog
            {
                CertificateRequestId = certReq.CertificateRequestId,
                OrganisationId = org,
                Action = "Rejected",
                PerformedByUserId = actor.UserId,
                PerformedByUserName = actor.FullName,
                Details = $"Request rejected: {certReq.RejectionReason}",
                Timestamp = DateTime.UtcNow
            });

            // Single notification to employee
            await NotifyEmployeeAsync(db, certReq.IndividualId, org, actor.UserId,
                $"Your certificate request #{certReq.Request?.ReferenceNumber ?? certReq.CertificateRequestId.ToString()} was rejected. Reason: {certReq.RejectionReason}",
                "/online/certificates");
        }
        else if (action.Equals("Approve", StringComparison.OrdinalIgnoreCase) || action.Equals("Issue", StringComparison.OrdinalIgnoreCase))
        {
            certReq.Status = "Issued";
            certReq.ApprovedByUserId = actor.UserId;
            certReq.ApprovedAt = DateTime.UtcNow;
            certReq.ReviewedByUserId = actor.UserId;
            certReq.ReviewedAt = DateTime.UtcNow;

            if (certReq.Request != null)
            {
                certReq.Request.RequestStateId = SharedConfig.RequestStates.COMPLETED;
                certReq.Request.LastStateChangedDate = DateTime.UtcNow;
                certReq.Request.LastStateChangedByUserId = actor.UserId;
            }

            var history = await GetVerifiedEmploymentHistoryAsync(certReq.IndividualId);
            var signatoryName = !string.IsNullOrWhiteSpace(input.CorrectedSignatoryName) ? input.CorrectedSignatoryName.Trim() : actor.FullName;
            var signatoryTitle = !string.IsNullOrWhiteSpace(input.CorrectedSignatoryTitle) ? input.CorrectedSignatoryTitle.Trim() : "Director of Human Resources";

            // Generate unique certificate number
            var year = DateTime.UtcNow.Year;
            var certNumber = $"CERT-{year}-{certReq.CertificateRequestId:D5}";
            var verificationToken = Guid.NewGuid().ToString("N");

            var snapshot = BuildSnapshot(certReq, history, signatoryName, signatoryTitle, org, certNumber, verificationToken);
            var snapshotJson = JsonSerializer.Serialize(snapshot);
            var sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));
            snapshot.Sha256Hash = sha256;

            var issuedCert = new IssuedCertificate
            {
                CertificateRequestId = certReq.CertificateRequestId,
                CertificateNumber = certNumber,
                VerificationToken = verificationToken,
                IndividualId = certReq.IndividualId,
                OrganisationId = org,
                CertificateType = certReq.CertificateType,
                IssueDate = TodayMaldives,
                IssuingOfficerUserId = actor.UserId,
                IssuingOfficerName = actor.FullName,
                IssuingOfficerTitle = "Human Resources Officer",
                SignatoryName = signatoryName,
                SignatoryTitle = signatoryTitle,
                SnapshotJson = JsonSerializer.Serialize(snapshot),
                Sha256Hash = sha256,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            db.IssuedCertificates.Add(issuedCert);

            db.CertificateAuditLogs.Add(new CertificateAuditLog
            {
                CertificateRequestId = certReq.CertificateRequestId,
                IssuedCertificateId = issuedCert.IssuedCertificateId,
                OrganisationId = org,
                Action = "Issued",
                PerformedByUserId = actor.UserId,
                PerformedByUserName = actor.FullName,
                Details = $"Official certificate {certNumber} generated and issued.",
                Timestamp = DateTime.UtcNow
            });

            // Single notification to employee (Only if employee is not the issuing HR officer)
            await NotifyEmployeeAsync(db, certReq.IndividualId, org, actor.UserId,
                $"Your official {certReq.CertificateType} ({certNumber}) has been approved and issued. You may now view and download your certificate.",
                "/online/certificates");
        }
        else
        {
            return ServiceResult.Fail($"Unrecognized review action: '{action}'.");
        }

        certReq.UpdatedAt = DateTime.UtcNow;
        certReq.UpdatedByUserId = actor.UserId;

        await db.SaveChangesAsync();
        return ServiceResult.Ok($"Action '{action}' executed successfully.");
    }

    public async Task<ServiceResult> RevokeCertificateAsync(CertificateRevokeInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin)
            return ServiceResult.Fail("Authorised HR permissions are required to revoke issued certificates.");

        if (string.IsNullOrWhiteSpace(input.Reason))
            return ServiceResult.Fail("Reason for revocation is required.");

        await using var db = await factory.CreateDbContextAsync();

        var cert = await db.IssuedCertificates
            .Include(c => c.CertificateRequest)
            .FirstOrDefaultAsync(c => c.IssuedCertificateId == input.IssuedCertificateId && c.OrganisationId == org);

        if (cert == null)
            return ServiceResult.Fail("Certificate not found.");

        if (cert.Status == "Revoked")
            return ServiceResult.Fail("Certificate is already marked as revoked.");

        cert.Status = "Revoked";
        cert.RevokedReason = input.Reason.Trim();
        cert.RevokedAt = DateTime.UtcNow;
        cert.RevokedByUserId = actor.UserId;

        db.CertificateAuditLogs.Add(new CertificateAuditLog
        {
            CertificateRequestId = cert.CertificateRequestId,
            IssuedCertificateId = cert.IssuedCertificateId,
            OrganisationId = org,
            Action = "Revoked",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actor.FullName,
            Details = $"Certificate {cert.CertificateNumber} revoked: {cert.RevokedReason}",
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await NotifyEmployeeAsync(db, cert.IndividualId, org, actor.UserId,
            $"Certificate {cert.CertificateNumber} has been officially revoked by HR. Reason: {cert.RevokedReason}",
            "/online/certificates");

        return ServiceResult.Ok($"Certificate {cert.CertificateNumber} has been revoked.");
    }

    public async Task<ServiceResult> ReissueCertificateAsync(CertificateReissueInput input)
    {
        var (actor, org, isHrOrAdmin) = await ScopeAsync();
        if (!isHrOrAdmin)
            return ServiceResult.Fail("Authorised HR permissions are required to reissue certificates.");

        if (string.IsNullOrWhiteSpace(input.Reason))
            return ServiceResult.Fail("Reason for reissue / supersession is required.");

        await using var db = await factory.CreateDbContextAsync();

        var oldCert = await db.IssuedCertificates
            .Include(c => c.CertificateRequest)
            .FirstOrDefaultAsync(c => c.IssuedCertificateId == input.IssuedCertificateId && c.OrganisationId == org);

        if (oldCert == null)
            return ServiceResult.Fail("Original certificate not found.");

        var certReq = oldCert.CertificateRequest;
        if (certReq == null)
            return ServiceResult.Fail("Linked certificate request not found.");

        // Mark original as Superseded
        oldCert.Status = "Superseded";
        oldCert.RevokedReason = $"Superseded by reissue: {input.Reason.Trim()}";
        oldCert.RevokedAt = DateTime.UtcNow;
        oldCert.RevokedByUserId = actor.UserId;

        var history = await GetVerifiedEmploymentHistoryAsync(certReq.IndividualId);
        var signatoryName = !string.IsNullOrWhiteSpace(input.CorrectedSignatoryName) ? input.CorrectedSignatoryName.Trim() : oldCert.SignatoryName;
        var signatoryTitle = !string.IsNullOrWhiteSpace(input.CorrectedSignatoryTitle) ? input.CorrectedSignatoryTitle.Trim() : oldCert.SignatoryTitle;

        var year = DateTime.UtcNow.Year;
        var newCertNumber = $"CERT-{year}-{certReq.CertificateRequestId:D5}-R1";
        var verificationToken = Guid.NewGuid().ToString("N");

        var snapshot = BuildSnapshot(certReq, history, signatoryName, signatoryTitle, org, newCertNumber, verificationToken);
        var snapshotJson = JsonSerializer.Serialize(snapshot);
        var sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));
        snapshot.Sha256Hash = sha256;

        var newCert = new IssuedCertificate
        {
            CertificateRequestId = certReq.CertificateRequestId,
            CertificateNumber = newCertNumber,
            VerificationToken = verificationToken,
            IndividualId = certReq.IndividualId,
            OrganisationId = org,
            CertificateType = certReq.CertificateType,
            IssueDate = TodayMaldives,
            IssuingOfficerUserId = actor.UserId,
            IssuingOfficerName = actor.FullName,
            IssuingOfficerTitle = "Human Resources Officer",
            SignatoryName = signatoryName,
            SignatoryTitle = signatoryTitle,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
            Sha256Hash = sha256,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        db.IssuedCertificates.Add(newCert);
        await db.SaveChangesAsync();

        oldCert.SupersededByCertificateId = newCert.IssuedCertificateId;

        db.CertificateAuditLogs.Add(new CertificateAuditLog
        {
            CertificateRequestId = certReq.CertificateRequestId,
            IssuedCertificateId = newCert.IssuedCertificateId,
            OrganisationId = org,
            Action = "Superseded",
            PerformedByUserId = actor.UserId,
            PerformedByUserName = actor.FullName,
            Details = $"Certificate {oldCert.CertificateNumber} superseded by {newCert.CertificateNumber}. Reason: {input.Reason}",
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await NotifyEmployeeAsync(db, certReq.IndividualId, org, actor.UserId,
            $"A superseded / revised certificate ({newCert.CertificateNumber}) has been issued for you.",
            "/online/certificates");

        return ServiceResult.Ok($"New certificate {newCert.CertificateNumber} issued successfully.");
    }

    public async Task<CertificateVerificationResultDto> VerifyCertificateAsync(string tokenOrNumber)
    {
        if (string.IsNullOrWhiteSpace(tokenOrNumber))
            return new CertificateVerificationResultDto { IsValid = false, Status = "NotFound" };

        var term = tokenOrNumber.Trim();
        await using var db = await factory.CreateDbContextAsync();

        var cert = await db.IssuedCertificates
            .AsNoTracking()
            .Include(c => c.Individual)
            .Include(c => c.Organisation)
            .FirstOrDefaultAsync(c => c.VerificationToken == term || c.CertificateNumber == term);

        if (cert == null)
            return new CertificateVerificationResultDto { IsValid = false, Status = "NotFound" };

        var snapshot = DeserializeSnapshot(cert.SnapshotJson);
        string? supersededByNumber = null;
        if (cert.SupersededByCertificateId.HasValue)
        {
            supersededByNumber = (await db.IssuedCertificates.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IssuedCertificateId == cert.SupersededByCertificateId.Value))?.CertificateNumber;
        }

        // Log audit verification hit
        try
        {
            db.CertificateAuditLogs.Add(new CertificateAuditLog
            {
                IssuedCertificateId = cert.IssuedCertificateId,
                OrganisationId = cert.OrganisationId,
                Action = "Verified",
                PerformedByUserId = 0,
                PerformedByUserName = "Public Verification Visitor",
                Details = $"Certificate {cert.CertificateNumber} verified via secure portal.",
                Timestamp = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not record verification audit hit for certificate {CertNo}", cert.CertificateNumber);
        }

        return new CertificateVerificationResultDto
        {
            IsValid = cert.Status == "Active",
            Status = cert.Status, // "Active", "Revoked", "Superseded"
            CertificateNumber = cert.CertificateNumber,
            CertificateType = cert.CertificateType,
            EmployeeFullName = snapshot.FullName,
            OrganisationName = snapshot.OrganisationName,
            EmploymentStatus = snapshot.IsCurrentlyEmployed ? "Currently Employed" : "Former Employee",
            EmploymentStartDate = snapshot.EmploymentStartDate,
            EmploymentEndDate = snapshot.EmploymentEndDate,
            FormattedDuration = snapshot.FormattedDuration,
            PrimaryJobTitle = snapshot.PrimaryDesignation,
            IssueDate = cert.IssueDate,
            SignatoryName = cert.SignatoryName,
            SignatoryTitle = cert.SignatoryTitle,
            RevokedAt = cert.RevokedAt,
            RevokedReason = cert.RevokedReason,
            SupersededByCertificateNumber = supersededByNumber,
            VerificationTimestamp = DateTime.UtcNow
        };
    }

    public async Task<CertificateSnapshotDto> GetCertificateSnapshotAsync(int issuedCertificateId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var cert = await db.IssuedCertificates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IssuedCertificateId == issuedCertificateId)
            ?? throw new InvalidOperationException("Certificate not found.");

        return DeserializeSnapshot(cert.SnapshotJson);
    }

    public async Task<byte[]?> GetIssuedCertificatePdfAsync(int issuedCertificateId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var cert = await db.IssuedCertificates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IssuedCertificateId == issuedCertificateId);

        return cert?.PdfContent;
    }

    public async Task<ServiceResult> SaveIssuedCertificatePdfAsync(int issuedCertificateId, byte[] pdfBytes)
    {
        if (pdfBytes == null || pdfBytes.Length == 0)
            return ServiceResult.Fail("No PDF bytes supplied.");

        await using var db = await factory.CreateDbContextAsync();
        var cert = await db.IssuedCertificates.FirstOrDefaultAsync(c => c.IssuedCertificateId == issuedCertificateId);
        if (cert == null) return ServiceResult.Fail("Certificate not found.");

        cert.PdfContent = pdfBytes;
        cert.PdfGeneratedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<List<CertificateAuditLogDto>> GetAuditLogsAsync(int? certificateRequestId = null, int? issuedCertificateId = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.CertificateAuditLogs.AsNoTracking();

        if (certificateRequestId.HasValue && certificateRequestId.Value > 0)
            query = query.Where(a => a.CertificateRequestId == certificateRequestId.Value);
        if (issuedCertificateId.HasValue && issuedCertificateId.Value > 0)
            query = query.Where(a => a.IssuedCertificateId == issuedCertificateId.Value);

        var rows = await query.OrderByDescending(a => a.Timestamp).Take(50).ToListAsync();
        return rows.Select(a => new CertificateAuditLogDto
        {
            CertificateAuditLogId = a.CertificateAuditLogId,
            Action = a.Action,
            PerformedByUserName = a.PerformedByUserName,
            Details = a.Details,
            Timestamp = a.Timestamp
        }).ToList();
    }

    // ==========================================
    // Internal Helper Methods
    // ==========================================

    private static CertificateRequestSummaryDto MapToSummary(CertificateRequest r)
    {
        var issued = r.IssuedCertificates?.OrderByDescending(c => c.IssueDate).FirstOrDefault();
        return new CertificateRequestSummaryDto
        {
            CertificateRequestId = r.CertificateRequestId,
            RequestId = r.RequestId,
            ReferenceNumber = r.Request?.ReferenceNumber ?? $"CR-{r.OrganisationId}-{r.CertificateRequestId:D5}",
            IndividualId = r.IndividualId,
            EmployeeNumber = r.Individual?.Staff?.EmployeeNumber,
            EmployeeName = r.Individual != null ? FullName(r.Individual) : $"Staff #{r.IndividualId}",
            DepartmentName = "Operations",
            CurrentDesignation = "Staff Member",
            CertificateType = r.CertificateType,
            Purpose = r.Purpose,
            RequestScope = r.RequestScope,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            AddressedTo = r.AddressedTo,
            IssuedCertificateId = issued?.IssuedCertificateId,
            CertificateNumber = issued?.CertificateNumber,
            VerificationToken = issued?.VerificationToken,
            ReturnReason = r.ReturnedReason,
            RejectionReason = r.RejectionReason
        };
    }

    private static CertificateSnapshotDto DeserializeSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new CertificateSnapshotDto();
        try
        {
            return JsonSerializer.Deserialize<CertificateSnapshotDto>(json) ?? new CertificateSnapshotDto();
        }
        catch
        {
            return new CertificateSnapshotDto();
        }
    }

    private static CertificateSnapshotDto BuildSnapshot(
        CertificateRequest req,
        CertificateVerifiedEmploymentHistoryDto history,
        string signatoryName,
        string signatoryTitle,
        int orgId,
        string? certNumber = null,
        string? verificationToken = null)
    {
        var isDetailed = req.CertificateType.Equals("DetailedExperience", StringComparison.OrdinalIgnoreCase);
        var certTitle = isDetailed ? "CERTIFICATE OF WORK EXPERIENCE" : "CERTIFICATE OF SERVICE & EMPLOYMENT";

        var filteredPositions = history.Positions;
        if (req.RequestScope.Equals("SpecificPeriod", StringComparison.OrdinalIgnoreCase) && req.SpecificPeriodStart.HasValue)
        {
            filteredPositions = filteredPositions
                .Where(p => p.FromDate >= req.SpecificPeriodStart.Value &&
                           (!req.SpecificPeriodEnd.HasValue || (p.ToDate ?? TodayMaldives) <= req.SpecificPeriodEnd.Value))
                .ToList();
        }

        var positionSnapshots = filteredPositions.Select(p => new CertificatePositionSnapshotItemDto
        {
            Title = p.PositionTitle,
            Department = p.Department,
            PeriodText = $"{p.FromDate:dd MMM yyyy} to {(p.ToDate.HasValue ? p.ToDate.Value.ToString("dd MMM yyyy") : "Present")}",
            Responsibilities = req.IncludeResponsibilities ? p.OfficialResponsibilities : []
        }).ToList();

        var projectSnapshots = (req.IncludeProjects ? history.Projects : new List<CertificateProjectItemDto>())
            .Select(pr => new CertificateProjectSnapshotItemDto
            {
                ProjectName = pr.ProjectName,
                Reference = pr.ProjectReference,
                Role = pr.Role,
                PeriodText = $"{pr.AssignedDate:dd MMM yyyy} to {(pr.EndDate.HasValue ? pr.EndDate.Value.ToString("dd MMM yyyy") : "Present")}",
                Remarks = pr.Remarks
            }).ToList();

        var intro = $"This is an official verification to certify that {history.FullName} (Employee No: {history.EmployeeNumber}) is currently serving or has served in good standing as an authorized personnel of {history.OrganisationName}.";

        var serviceStatement = history.IsCurrentlyEmployed
            ? $"{history.FullName} commenced employment on {history.JoinedDate:dd MMMM yyyy} and continues in active service to date, having completed a cumulative total service duration of {history.FormattedServiceDuration}. The current substantive title held is {history.CurrentJobTitle} within the {history.DepartmentName}."
            : $"{history.FullName} commenced employment on {history.JoinedDate:dd MMMM yyyy} and concluded official service on {(history.TerminatedDate.HasValue ? history.TerminatedDate.Value.ToString("dd MMMM yyyy") : "concluded tenure")}, with a total verified service duration of {history.FormattedServiceDuration}. The final substantive title held was {history.CurrentJobTitle}.";

        var concluding = $"This official certificate is issued at the specific request of the named employee for the stated purpose of: {req.Purpose}{(string.IsNullOrWhiteSpace(req.PurposeDetails) ? "" : $" ({req.PurposeDetails})")}. Any prospective recipient or verification authority may validate the authenticity of this document using the digital verification token or QR code printed below.";

        var token = verificationToken ?? "DRAFT";
        var number = certNumber ?? $"DRAFT-{req.CertificateRequestId}";

        return new CertificateSnapshotDto
        {
            CertificateNumber = number,
            VerificationToken = token,
            VerificationUrl = $"/verify/certificate/{token}",
            CertificateType = req.CertificateType,
            CertificateTitle = certTitle,
            IssueDate = TodayMaldives,
            AddressedTo = string.IsNullOrWhiteSpace(req.AddressedTo) ? "TO WHOM IT MAY CONCERN" : req.AddressedTo.ToUpperInvariant(),
            OrganisationId = orgId,
            OrganisationName = history.OrganisationName,
            OrganisationRegistrationNumber = "GOV-MV-HRM",
            HeaderNotes = "Republic of Maldives â€¢ Civil & Public Service Human Resource Management",
            IndividualId = history.IndividualId,
            EmployeeNumber = history.EmployeeNumber,
            FullName = history.FullName,
            EmploymentStartDate = history.JoinedDate,
            EmploymentEndDate = history.TerminatedDate,
            IsCurrentlyEmployed = history.IsCurrentlyEmployed,
            FormattedDuration = history.FormattedServiceDuration,
            PrimaryDepartment = history.DepartmentName,
            PrimaryDesignation = history.CurrentJobTitle,
            BasicSalary = req.IncludeSalary ? history.CurrentBasicSalary : null,
            IntroductoryText = intro,
            ServiceStatement = serviceStatement,
            ConcludingText = concluding,
            Positions = positionSnapshots,
            Projects = projectSnapshots,
            SignatoryName = signatoryName,
            SignatoryTitle = signatoryTitle,
            IssuingOfficerName = signatoryName,
            IssuingOfficerTitle = "Human Resources Officer"
        };
    }

    private static (int Years, int Months, int Days) CalculatePreciseServiceDuration(DateTime start, DateTime end)
    {
        if (end < start) return (0, 0, 0);

        int years = end.Year - start.Year;
        int months = end.Month - start.Month;
        int days = end.Day - start.Day;

        if (days < 0)
        {
            months--;
            var previousMonthDays = DateTime.DaysInMonth(
                end.Month == 1 ? end.Year - 1 : end.Year,
                end.Month == 1 ? 12 : end.Month - 1);
            days += previousMonthDays;
        }

        if (months < 0)
        {
            years--;
            months += 12;
        }

        return (Math.Max(0, years), Math.Max(0, months), Math.Max(0, days));
    }

    private static string FormatDuration(int years, int months, int days)
    {
        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} {(years == 1 ? "year" : "years")}");
        if (months > 0) parts.Add($"{months} {(months == 1 ? "month" : "months")}");
        if (days > 0 || parts.Count == 0) parts.Add($"{days} {(days == 1 ? "day" : "days")}");
        return string.Join(", ", parts);
    }

    private static List<string> ExtractResponsibilities(string? desc)
    {
        if (string.IsNullOrWhiteSpace(desc)) return [];
        return desc.Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().TrimStart('-', '\u2022', '*', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ' '))
            .Where(s => s.Length > 5)
            .Distinct()
            .Take(8)
            .ToList();
    }

    // Strict notification de-duplication:
    // Only sends to distinct recipients, strictly excluding the actor (to prevent duplicate self-notification).
    private static async Task NotifyHrOfficersAsync(HrmTeContext db, int orgId, int actorUserId, string message, string url)
    {
        var hrUsers = await db.Users
            .AsNoTracking()
            .Where(u => ((u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == orgId) || u.AdminPortalAccessOrganisationId == orgId) && u.IsActive)
            .Select(u => new { u.UserId, u.BusinessEntityID })
            .ToListAsync();

        var recipients = hrUsers
            .Where(u => u.UserId != actorUserId)
            .DistinctBy(u => u.UserId)
            .Take(10)
            .ToList();

        foreach (var recipient in recipients)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = recipient.UserId,
                IndividualId = recipient.BusinessEntityID,
                OrganisationId = orgId,
                Kind = "Certificate",
                Message = message,
                Url = url,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static async Task NotifyEmployeeAsync(HrmTeContext db, int individualId, int orgId, int actorUserId, string message, string url)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.BusinessEntityID == individualId);
        if (user == null || user.UserId == actorUserId) return; // Strict rule: No self-notification!

        db.StaffNotifications.Add(new StaffNotification
        {
            UserId = user.UserId,
            IndividualId = individualId,
            OrganisationId = orgId,
            Kind = "Certificate",
            Message = message,
            Url = url,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
