using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.AttendanceDeductions;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.AttendanceDeductions;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Attendance.Deductions;

public sealed class AttendanceDeductionService : IAttendanceDeductionService
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly ILogger<AttendanceDeductionService> _logger;
    private readonly IUserAccessService _access;

    public AttendanceDeductionService(
        IDbContextFactory<HrmTeContext> factory,
        ILogger<AttendanceDeductionService> logger,
        IUserAccessService access)
    {
        _factory = factory;
        _logger = logger;
        _access = access;
    }

    public async Task<AttendanceDeductionPolicy> GetEffectivePolicyAsync(int organisationId, DateOnly workDate)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var policy = await db.AttendanceDeductionPolicies
            .AsNoTracking()
            .Where(p => p.OrganisationId == organisationId && p.IsActive &&
                        p.EffectiveFrom <= workDate &&
                        (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= workDate))
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (policy != null) return policy;

        // Default Maldives Civil Service Policy Baseline (Civil Service Reg Art 149 & 160)
        return new AttendanceDeductionPolicy
        {
            OrganisationId = organisationId,
            PolicyCode = "MCS-STD-2026",
            PolicyName = "Maldives Civil Service Standard Attendance Policy",
            LegalReference = "Civil Service Regulation Art 149 & 160; July 2026 Amendment",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            IsActive = true,
            IsVerified = true,
            SalaryDivisor = 30,
            OfficialMinutesSource = "ScheduledWorkPlan",
            DefaultDailyWorkingMinutes = 360,
            ArrivalBufferMinutes = 30,
            BufferEligibilityCategory = "AllNonShift",
            BufferMakeUpRequired = true,
            BufferMakeUpWindow = "SameWeek",
            BufferExceededRule = BufferExceededRules.RequireReview,
            LateDeductionFormula = "PerMinuteBasic",
            AbsenceDeductionFormula = "BasicPlusServiceAllowance",
            DeductServiceAllowanceOnAbsence = true,
            ServiceAllowanceComponentCode = "SERV_ALLOW",
            DeductFromPensionableSalary = true,
            RoundingMethod = "AwayFromZero",
            RequiresVerification = true
        };
    }

    public async Task<AttendanceAdjustment?> EvaluateDailyDeductionAsync(
        int organisationId, int individualId, int jobId, DateOnly workDate, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var policy = await GetEffectivePolicyAsync(organisationId, workDate);
        var workDateDt = workDate.ToDateTime(TimeOnly.MinValue);

        // 1. Resolve employee job and compensation
        var job = await db.Jobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobId == jobId && j.OrganisationID == organisationId && j.IndividualID == individualId, ct);

        if (job == null) return null;

        var comp = await db.EmployeeCompensations.AsNoTracking()
            .Where(c => c.JobId == jobId && c.IsActive &&
                        c.EffectiveFrom <= workDate &&
                        (!c.EffectiveTo.HasValue || c.EffectiveTo.Value >= workDate))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        decimal monthlyBasicSalary = comp?.BasicSalary ?? (job.BasicSalary.HasValue ? Convert.ToDecimal(job.BasicSalary.Value) : 0m);
        decimal monthlyServiceAllowance = 0m;

        if (policy.DeductServiceAllowanceOnAbsence)
        {
            var serviceAllowance = await (from a in db.StaffAllowances.AsNoTracking()
                                          join t in db.AllowanceTypes.AsNoTracking() on a.AllowanceTypeId equals t.Id
                                           where a.JobId == jobId && a.OrganisationId == organisationId &&
                                                 (EF.Functions.Like(t.Name, "%Service%") || EF.Functions.Like(t.Name, $"%{policy.ServiceAllowanceComponentCode}%")) &&
                                                a.FromDate <= workDateDt && (!a.ToDate.HasValue || a.ToDate.Value >= workDateDt)
                                           select a.Amount).SumAsync(ct);
            monthlyServiceAllowance = serviceAllowance;
        }

        // 2. Load attendance facts & obligations for the date
        var facts = await AttendanceScheduleFacts.LoadAsync(db, individualId, workDateDt, workDateDt, organisationId, ct);
        var officialFact = facts.FirstOrDefault(f => f.RequiresAttendance && f.Purpose == "Official");

        // If no required official schedule, no deduction applies
        if (officialFact == null)
        {
            await ClearPendingAdjustmentAsync(db, organisationId, jobId, workDate, "No required official schedule exists for this date.", ct);
            return null;
        }

        int officialWorkingMinutes = (int)(officialFact.ScheduledEnd - officialFact.ScheduledStart).TotalMinutes;
        if (officialWorkingMinutes <= 0) officialWorkingMinutes = policy.DefaultDailyWorkingMinutes;

        // 3. Validation: Check Approved Leaves
        bool hasApprovedLeave = await db.Leaves.AsNoTracking().AnyAsync(l =>
            l.Job.IndividualID == individualId && l.Job.OrganisationID == organisationId &&
            l.LeaveStateId == 4 && l.FromDate <= workDateDt && l.ToDate >= workDateDt, ct);

        if (hasApprovedLeave)
        {
            await ClearPendingAdjustmentAsync(db, organisationId, jobId, workDate, "Covered by approved leave.", ct);
            return null; // Covered by approved leave
        }

        // 4. Validation: Check Official Duty / Off-Site assignments
        bool hasOfficialDuty = facts.Any(f => f.Purpose == "Duty" && f.CheckIn.HasValue);

        // 5. Check Approved Time Change Requests (e.g. approved Late Check-In or Early Check-Out)
        var approvedTimeChange = await db.LateCheckInRequests.AsNoTracking().FirstOrDefaultAsync(r =>
            r.IndividualId == individualId && r.OrganisationId == organisationId &&
            r.WorkAssignmentId == officialFact.WorkAssignmentId && r.Request.RequestStateId == 4, ct);

        // 6. Evaluate Attendance Outcome
        string outcome = AttendanceOutcomeClassifications.OnTime;
        string adjustmentType = AttendanceDeductionTypes.LateArrival;
        int scheduledMinutes = officialWorkingMinutes;
        int actualMinutes = 0;
        int deductibleMinutes = 0;
        decimal estimatedDeduction = 0m;
        decimal basicDeduction = 0m;
        decimal serviceDeduction = 0m;
        bool isAssumptionVerified = policy.IsVerified;

        // Case A: Full-Day Unauthorised Absence (No check-in, no check-out, no duty, no leave)
        if (!officialFact.CheckIn.HasValue && !officialFact.CheckOut.HasValue)
        {
            if (hasOfficialDuty)
            {
                outcome = AttendanceOutcomeClassifications.OfficialDuty;
                await ClearPendingAdjustmentAsync(db, organisationId, jobId, workDate, "Covered by official duty.", ct);
                return null;
            }

            outcome = AttendanceOutcomeClassifications.UnauthorisedAbsence;
            adjustmentType = AttendanceDeductionTypes.FullDayAbsence;
            deductibleMinutes = scheduledMinutes;

            // Civil Service Regulation Article 160(a):
            // Daily Basic Salary = Monthly Basic Salary / 30
            // Daily Service Allowance = Monthly Service Allowance / 30
            basicDeduction = decimal.Round(monthlyBasicSalary / policy.SalaryDivisor, 2, MidpointRounding.AwayFromZero);
            if (policy.DeductServiceAllowanceOnAbsence && monthlyServiceAllowance > 0)
            {
                serviceDeduction = decimal.Round(monthlyServiceAllowance / policy.SalaryDivisor, 2, MidpointRounding.AwayFromZero);
            }
            estimatedDeduction = basicDeduction + serviceDeduction;
        }
        // Case B: Missing Clock Event (Only check-in OR only check-out recorded)
        else if (officialFact.CheckIn.HasValue ^ officialFact.CheckOut.HasValue)
        {
            outcome = AttendanceOutcomeClassifications.MissingClockEvent;
            adjustmentType = AttendanceDeductionTypes.PartialDayAbsence;
            deductibleMinutes = 0; // Requires HR review before assigning financial deduction
            isAssumptionVerified = false;
        }
        // Case C: Normal or Late Check-In
        else if (officialFact.CheckIn.HasValue)
        {
            var clockIn = officialFact.CheckIn.Value;
            var scheduledStart = officialFact.ScheduledStart;

            actualMinutes = (int)(officialFact.CheckOut!.Value - clockIn).TotalMinutes;
            if (actualMinutes < 0) actualMinutes = 0;

            if (clockIn > scheduledStart)
            {
                int rawLateMinutes = (int)Math.Ceiling((clockIn - scheduledStart).TotalMinutes);

                // Check approved time-change explanation
                if (approvedTimeChange != null && approvedTimeChange.Kind == "LateCheckIn")
                {
                    outcome = AttendanceOutcomeClassifications.Late_Excused;
                    deductibleMinutes = 0;
                }
                // July 2026 Arrival Buffer Rule (e.g. <= 30 minutes late)
                else if (rawLateMinutes <= policy.ArrivalBufferMinutes &&
                         (!policy.BufferEligibilityCategory.Equals("AllNonShift", StringComparison.OrdinalIgnoreCase) || !officialFact.IsShiftOrDuty))
                {
                    // Check if make-up time is required and whether it was satisfied
                    if (!policy.BufferMakeUpRequired)
                    {
                        outcome = AttendanceOutcomeClassifications.EligibleBuffer_Completed;
                        deductibleMinutes = 0;
                    }
                    else
                    {
                        // Check same-day make up (actual presence >= scheduled)
                        bool sameDayMadeUp = actualMinutes >= scheduledMinutes;
                        if (sameDayMadeUp)
                        {
                            outcome = AttendanceOutcomeClassifications.EligibleBuffer_Completed;
                            deductibleMinutes = 0;
                        }
                        else
                        {
                            // If week is still active, pending make-up; if past week without make-up, incomplete
                            var weekStart = workDateDt.AddDays(-(int)workDateDt.DayOfWeek);
                            var weekEnd = weekStart.AddDays(7);
                            var makeupResult = await WeeklyAttendanceMakeup.LoadLateArrivalAsync(
                                db, individualId, organisationId, weekStart, weekEnd, policy.ArrivalBufferMinutes, ct);

                            if (makeupResult.RemainingHours <= 0)
                            {
                                outcome = AttendanceOutcomeClassifications.EligibleBuffer_Completed;
                                deductibleMinutes = 0;
                            }
                            else if (DateTime.UtcNow.AddHours(5) < weekEnd)
                            {
                                outcome = AttendanceOutcomeClassifications.EligibleBuffer_MakeUpPending;
                                deductibleMinutes = 0;
                            }
                            else
                            {
                                outcome = AttendanceOutcomeClassifications.EligibleBuffer_MakeUpIncomplete;
                                deductibleMinutes = rawLateMinutes;
                                isAssumptionVerified = false; // Requires supervisor / HR review
                            }
                        }
                    }
                }
                // Exceeded Arrival Buffer (e.g. 31+ minutes late)
                else
                {
                    outcome = AttendanceOutcomeClassifications.Late_Verified;
                    adjustmentType = AttendanceDeductionTypes.LateArrival;

                    if (policy.BufferExceededRule == BufferExceededRules.DeductAllMinutes)
                    {
                        deductibleMinutes = rawLateMinutes;
                    }
                    else if (policy.BufferExceededRule == BufferExceededRules.DeductExcessOnly)
                    {
                        deductibleMinutes = rawLateMinutes - policy.ArrivalBufferMinutes;
                    }
                    else
                    {
                        // BufferExceededRules.RequireReview: Prevent unverified deduction
                        deductibleMinutes = rawLateMinutes;
                        isAssumptionVerified = false;
                        outcome = AttendanceOutcomeClassifications.PendingVerification;
                    }

                    // Civil Service Lateness Formula:
                    // Daily Salary = Monthly Basic / 30
                    // Per-Minute Salary = Daily Salary / Official Working Minutes
                    // Late Deduction = Per-Minute Salary * Deductible Minutes
                    decimal dailySalary = monthlyBasicSalary / policy.SalaryDivisor;
                    decimal perMinuteSalary = officialWorkingMinutes > 0 ? (dailySalary / officialWorkingMinutes) : 0m;
                    basicDeduction = decimal.Round(perMinuteSalary * deductibleMinutes, 2, MidpointRounding.AwayFromZero);
                    estimatedDeduction = basicDeduction;
                }

                if (deductibleMinutes > 0 && basicDeduction <= 0)
                {
                    decimal dailySalary = monthlyBasicSalary / policy.SalaryDivisor;
                    decimal perMinuteSalary = officialWorkingMinutes > 0 ? dailySalary / officialWorkingMinutes : 0m;
                    basicDeduction = decimal.Round(perMinuteSalary * deductibleMinutes, 2, MidpointRounding.AwayFromZero);
                    estimatedDeduction = basicDeduction;
                }
            }
            else
            {
                outcome = AttendanceOutcomeClassifications.OnTime;
            }

            // Check for Early Departure
            if (officialFact.CheckOut.HasValue && officialFact.CheckOut.Value < officialFact.ScheduledEnd)
            {
                int earlyMinutes = (int)Math.Ceiling((officialFact.ScheduledEnd - officialFact.CheckOut.Value).TotalMinutes);
                if (earlyMinutes > 0 && approvedTimeChange?.Kind != "EarlyCheckOut")
                {
                    decimal dailySalary = monthlyBasicSalary / policy.SalaryDivisor;
                    decimal perMinuteSalary = officialWorkingMinutes > 0 ? dailySalary / officialWorkingMinutes : 0m;
                    decimal earlyDeduction = decimal.Round(perMinuteSalary * earlyMinutes, 2, MidpointRounding.AwayFromZero);
                    if (deductibleMinutes > 0 || estimatedDeduction > 0)
                    {
                        outcome = AttendanceOutcomeClassifications.PendingVerification;
                        adjustmentType = AttendanceDeductionTypes.PartialDayAbsence;
                        deductibleMinutes += earlyMinutes;
                        basicDeduction += earlyDeduction;
                        estimatedDeduction = basicDeduction;
                        isAssumptionVerified = false;
                    }
                    else
                    {
                        outcome = AttendanceOutcomeClassifications.EarlyDeparture;
                        adjustmentType = AttendanceDeductionTypes.EarlyDeparture;
                        deductibleMinutes = earlyMinutes;
                        basicDeduction = earlyDeduction;
                        estimatedDeduction = basicDeduction;
                    }
                }
            }
        }

        // If outcome is completely on-time / completed buffer with 0 deductible minutes, do not create a deduction
        if (deductibleMinutes <= 0 && estimatedDeduction <= 0 &&
            outcome is AttendanceOutcomeClassifications.OnTime or AttendanceOutcomeClassifications.EligibleBuffer_Completed)
        {
            await ClearPendingAdjustmentAsync(db, organisationId, jobId, workDate, "Attendance is complete and no deduction applies.", ct);
            return null;
        }

        // 7. Persist or update the attendance adjustment record (Idempotent)
        string sourceRef = $"ATT-{organisationId}-{jobId}-{workDate:yyyyMMdd}-{adjustmentType}";

        var existing = await db.AttendanceAdjustments.FirstOrDefaultAsync(a =>
            a.OrganisationId == organisationId && a.JobId == jobId &&
            a.WorkDate == workDate && a.AdjustmentType == adjustmentType, ct);

        var stale = await db.AttendanceAdjustments.Where(a =>
            a.OrganisationId == organisationId && a.JobId == jobId && a.WorkDate == workDate &&
            a.AdjustmentType != adjustmentType && a.PayrollProcessingStatus == AttendanceAdjustmentPayrollStatuses.Pending)
            .ToListAsync(ct);
        foreach (var row in stale) VoidAdjustment(row, "Replaced by recalculated attendance outcome.");

        if (existing == null)
        {
            existing = new AttendanceAdjustment
            {
                OrganisationId = organisationId,
                IndividualId = individualId,
                JobId = jobId,
                WorkDate = workDate,
                AdjustmentType = adjustmentType,
                ScheduledMinutes = scheduledMinutes,
                ActualMinutes = actualMinutes,
                DeductibleMinutes = deductibleMinutes,
                EstimatedDeductionAmount = estimatedDeduction,
                BasicSalaryDeductionAmount = basicDeduction,
                ServiceAllowanceDeductionAmount = serviceDeduction,
                SourceAttendanceReference = sourceRef,
                SourceObligationId = officialFact.WorkAssignmentId,
                AttendanceOutcome = outcome,
                VerificationStatus = isAssumptionVerified && !policy.RequiresVerification
                    ? AttendanceAdjustmentVerificationStatuses.Verified
                    : AttendanceAdjustmentVerificationStatuses.PendingVerification,
                PolicyId = policy.AttendanceDeductionPolicyId,
                PolicyCode = policy.PolicyCode,
                PolicyVersion = policy.PolicyCode + "-v1",
                RegulatorySource = policy.LegalReference,
                IsRegulatoryAssumptionVerified = isAssumptionVerified,
                PayrollProcessingStatus = AttendanceAdjustmentPayrollStatuses.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.AttendanceAdjustments.Add(existing);
        }
        else if (existing.PayrollProcessingStatus == AttendanceAdjustmentPayrollStatuses.Pending)
        {
            // Update candidate values if payroll hasn't consumed them yet
            existing.ScheduledMinutes = scheduledMinutes;
            existing.ActualMinutes = actualMinutes;
            existing.DeductibleMinutes = deductibleMinutes;
            existing.EstimatedDeductionAmount = estimatedDeduction;
            existing.BasicSalaryDeductionAmount = basicDeduction;
            existing.ServiceAllowanceDeductionAmount = serviceDeduction;
            existing.AttendanceOutcome = outcome;
            existing.IsRegulatoryAssumptionVerified = isAssumptionVerified;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private static async Task ClearPendingAdjustmentAsync(HrmTeContext db, int organisationId, int jobId,
        DateOnly workDate, string reason, CancellationToken ct)
    {
        var rows = await db.AttendanceAdjustments.Where(a =>
            a.OrganisationId == organisationId && a.JobId == jobId && a.WorkDate == workDate &&
            a.PayrollProcessingStatus == AttendanceAdjustmentPayrollStatuses.Pending).ToListAsync(ct);
        foreach (var row in rows) VoidAdjustment(row, reason);
        if (rows.Count > 0) await db.SaveChangesAsync(ct);
    }

    private static void VoidAdjustment(AttendanceAdjustment row, string reason)
    {
        row.DeductibleMinutes = 0;
        row.EstimatedDeductionAmount = 0m;
        row.BasicSalaryDeductionAmount = 0m;
        row.ServiceAllowanceDeductionAmount = 0m;
        row.VerificationStatus = AttendanceAdjustmentVerificationStatuses.Excused;
        row.ReviewerComments = reason;
        row.UpdatedAtUtc = DateTime.UtcNow;
    }

    public async Task<List<AttendanceAdjustment>> EvaluateRangeAsync(
        int organisationId, int? individualId, int? jobId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var jobsQuery = db.Jobs.AsNoTracking().Where(j => j.OrganisationID == organisationId && j.JobStateId == 4);
        if (jobId.HasValue) jobsQuery = jobsQuery.Where(j => j.JobId == jobId.Value);
        if (individualId.HasValue) jobsQuery = jobsQuery.Where(j => j.IndividualID == individualId.Value);

        var activeJobs = await jobsQuery.Select(j => new { j.JobId, j.IndividualID }).ToListAsync(ct);
        var results = new List<AttendanceAdjustment>();

        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            foreach (var j in activeJobs)
            {
                try
                {
                    var adj = await EvaluateDailyDeductionAsync(organisationId, j.IndividualID, j.JobId, d, ct);
                    if (adj != null) results.Add(adj);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed evaluating attendance deduction for Job {JobId} on {Date}", j.JobId, d);
                }
            }
        }

        return results;
    }

    public async Task<List<AttendanceAdjustmentDto>> GetAdjustmentsAsync(
        int organisationId, DateOnly? fromDate = null, DateOnly? toDate = null, string? verificationStatus = null, string? payrollStatus = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.AttendanceAdjustments.AsNoTracking()
            .Where(a => a.OrganisationId == organisationId);

        if (fromDate.HasValue) query = query.Where(a => a.WorkDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.WorkDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(verificationStatus)) query = query.Where(a => a.VerificationStatus == verificationStatus);
        if (!string.IsNullOrWhiteSpace(payrollStatus)) query = query.Where(a => a.PayrollProcessingStatus == payrollStatus);

        var rows = await query
            .OrderByDescending(a => a.WorkDate)
            .Take(500)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return new List<AttendanceAdjustmentDto>();
        }

        var indIds = rows.Select(r => r.IndividualId).Distinct().ToList();
        var individuals = indIds.Count > 0
            ? await db.Individuals.AsNoTracking()
                .Where(i => indIds.Contains(i.BusinessEntityId))
                .Select(i => new { i.BusinessEntityId, FullName = (i.FirstNameEnglish + " " + i.LastNameEnglish).Trim() })
                .ToDictionaryAsync(i => i.BusinessEntityId, i => i.FullName)
            : new Dictionary<int, string>();

        var userIds = rows.Where(r => r.VerifiedByUserId.HasValue).Select(r => r.VerifiedByUserId!.Value).Distinct().ToList();
        var users = userIds.Count > 0
            ? await db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, FullName = u.FullName ?? u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.FullName)
            : new Dictionary<int, string>();

        return rows.Select(r => new AttendanceAdjustmentDto(
            AttendanceAdjustmentId: r.AttendanceAdjustmentId,
            OrganisationId: r.OrganisationId,
            IndividualId: r.IndividualId,
            EmployeeName: individuals.GetValueOrDefault(r.IndividualId, $"Employee #{r.IndividualId}"),
            EmployeeNumber: $"EMP-{r.IndividualId}",
            DepartmentName: "General",
            JobId: r.JobId,
            WorkDate: r.WorkDate,
            AdjustmentType: r.AdjustmentType,
            ScheduledMinutes: r.ScheduledMinutes,
            ActualMinutes: r.ActualMinutes,
            DeductibleMinutes: r.DeductibleMinutes,
            EstimatedDeductionAmount: r.EstimatedDeductionAmount,
            BasicSalaryDeductionAmount: r.BasicSalaryDeductionAmount,
            ServiceAllowanceDeductionAmount: r.ServiceAllowanceDeductionAmount,
            SourceAttendanceReference: r.SourceAttendanceReference,
            AttendanceOutcome: r.AttendanceOutcome,
            VerificationStatus: r.VerificationStatus,
            VerifiedByUserId: r.VerifiedByUserId,
            VerifiedByUserName: r.VerifiedByUserId.HasValue ? users.GetValueOrDefault(r.VerifiedByUserId.Value) : null,
            VerifiedAtUtc: r.VerifiedAtUtc,
            ReviewerComments: r.ReviewerComments,
            PolicyCode: r.PolicyCode,
            RegulatorySource: r.RegulatorySource,
            IsRegulatoryAssumptionVerified: r.IsRegulatoryAssumptionVerified,
            PayrollProcessingStatus: r.PayrollProcessingStatus,
            PayrollRunId: r.PayrollRunId,
            ScheduleDetails: $"Scheduled: {r.ScheduledMinutes} mins",
            ClockEventDetails: $"Actual: {r.ActualMinutes} mins"
        )).ToList();
    }

    public async Task<ServiceResult> VerifyAdjustmentAsync(long adjustmentId, int userId, string comments)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var organisationId = await _access.GetCurrentOrganisationIdAsync();
        var adj = await db.AttendanceAdjustments.FirstOrDefaultAsync(a => a.AttendanceAdjustmentId == adjustmentId && a.OrganisationId == organisationId);
        if (adj == null) return ServiceResult.Fail("Attendance adjustment not found.");

        if (adj.PayrollProcessingStatus != AttendanceAdjustmentPayrollStatuses.Pending)
        {
            return ServiceResult.Fail("This adjustment has already been finalized or paid in payroll.");
        }

        string prev = adj.VerificationStatus;
        adj.VerificationStatus = AttendanceAdjustmentVerificationStatuses.Verified;
        adj.VerifiedByUserId = userId;
        adj.VerifiedAtUtc = DateTime.UtcNow;
        adj.ReviewerComments = comments;
        adj.UpdatedAtUtc = DateTime.UtcNow;

        db.AttendanceExceptionReviews.Add(new AttendanceExceptionReview
        {
            AttendanceAdjustmentId = adjustmentId,
            OrganisationId = adj.OrganisationId,
            Action = "Verify",
            Reason = comments,
            PerformedByUserId = userId,
            PerformedAtUtc = DateTime.UtcNow,
            PreviousStatus = prev,
            NewStatus = AttendanceAdjustmentVerificationStatuses.Verified
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Attendance deduction verified and approved for payroll processing.");
    }

    public async Task<ServiceResult> ExcuseAdjustmentAsync(long adjustmentId, int userId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("A valid justification is required to excuse an attendance exception.");

        await using var db = await _factory.CreateDbContextAsync();
        var organisationId = await _access.GetCurrentOrganisationIdAsync();
        var adj = await db.AttendanceAdjustments.FirstOrDefaultAsync(a => a.AttendanceAdjustmentId == adjustmentId && a.OrganisationId == organisationId);
        if (adj == null) return ServiceResult.Fail("Attendance adjustment not found.");

        if (adj.PayrollProcessingStatus != AttendanceAdjustmentPayrollStatuses.Pending)
        {
            return ServiceResult.Fail("Cannot excuse an adjustment that has already been finalized in payroll.");
        }

        string prev = adj.VerificationStatus;
        adj.VerificationStatus = AttendanceAdjustmentVerificationStatuses.Excused;
        adj.DeductibleMinutes = 0;
        adj.EstimatedDeductionAmount = 0m;
        adj.BasicSalaryDeductionAmount = 0m;
        adj.ServiceAllowanceDeductionAmount = 0m;
        adj.VerifiedByUserId = userId;
        adj.VerifiedAtUtc = DateTime.UtcNow;
        adj.ReviewerComments = $"Excused: {reason}";
        adj.UpdatedAtUtc = DateTime.UtcNow;

        db.AttendanceExceptionReviews.Add(new AttendanceExceptionReview
        {
            AttendanceAdjustmentId = adjustmentId,
            OrganisationId = adj.OrganisationId,
            Action = "Excuse",
            Reason = reason,
            PerformedByUserId = userId,
            PerformedAtUtc = DateTime.UtcNow,
            PreviousStatus = prev,
            NewStatus = AttendanceAdjustmentVerificationStatuses.Excused
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Attendance exception excused. Deductible minutes set to 0.");
    }

    public async Task<ServiceResult> RejectAdjustmentAsync(long adjustmentId, int userId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("Reason is required to reject an attendance adjustment.");

        await using var db = await _factory.CreateDbContextAsync();
        var organisationId = await _access.GetCurrentOrganisationIdAsync();
        var adj = await db.AttendanceAdjustments.FirstOrDefaultAsync(a => a.AttendanceAdjustmentId == adjustmentId && a.OrganisationId == organisationId);
        if (adj == null) return ServiceResult.Fail("Attendance adjustment not found.");

        if (adj.PayrollProcessingStatus != AttendanceAdjustmentPayrollStatuses.Pending)
        {
            return ServiceResult.Fail("Cannot reject an adjustment that has already been finalized in payroll.");
        }

        string prev = adj.VerificationStatus;
        adj.VerificationStatus = AttendanceAdjustmentVerificationStatuses.Rejected;
        adj.DeductibleMinutes = 0;
        adj.EstimatedDeductionAmount = 0m;
        adj.BasicSalaryDeductionAmount = 0m;
        adj.ServiceAllowanceDeductionAmount = 0m;
        adj.VerifiedByUserId = userId;
        adj.VerifiedAtUtc = DateTime.UtcNow;
        adj.ReviewerComments = $"Rejected: {reason}";
        adj.UpdatedAtUtc = DateTime.UtcNow;

        db.AttendanceExceptionReviews.Add(new AttendanceExceptionReview
        {
            AttendanceAdjustmentId = adjustmentId,
            OrganisationId = adj.OrganisationId,
            Action = "Reject",
            Reason = reason,
            PerformedByUserId = userId,
            PerformedAtUtc = DateTime.UtcNow,
            PreviousStatus = prev,
            NewStatus = AttendanceAdjustmentVerificationStatuses.Rejected
        });

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Attendance adjustment rejected.");
    }

    public async Task<List<AttendanceDeductionPolicyDto>> GetPoliciesAsync(int organisationId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var policies = await db.AttendanceDeductionPolicies.AsNoTracking()
            .Where(p => p.OrganisationId == organisationId)
            .OrderByDescending(p => p.EffectiveFrom)
            .ToListAsync();

        return policies.Select(p => new AttendanceDeductionPolicyDto(
            PolicyId: p.AttendanceDeductionPolicyId,
            OrganisationId: p.OrganisationId,
            PolicyCode: p.PolicyCode,
            PolicyName: p.PolicyName,
            LegalReference: p.LegalReference,
            EffectiveFrom: p.EffectiveFrom,
            EffectiveTo: p.EffectiveTo,
            IsActive: p.IsActive,
            IsVerified: p.IsVerified,
            SalaryDivisor: p.SalaryDivisor,
            OfficialMinutesSource: p.OfficialMinutesSource,
            DefaultDailyWorkingMinutes: p.DefaultDailyWorkingMinutes,
            ArrivalBufferMinutes: p.ArrivalBufferMinutes,
            BufferEligibilityCategory: p.BufferEligibilityCategory,
            BufferMakeUpRequired: p.BufferMakeUpRequired,
            BufferMakeUpWindow: p.BufferMakeUpWindow,
            BufferExceededRule: p.BufferExceededRule,
            LateDeductionFormula: p.LateDeductionFormula,
            AbsenceDeductionFormula: p.AbsenceDeductionFormula,
            DeductServiceAllowanceOnAbsence: p.DeductServiceAllowanceOnAbsence,
            ServiceAllowanceComponentCode: p.ServiceAllowanceComponentCode,
            DeductFromPensionableSalary: p.DeductFromPensionableSalary,
            RoundingMethod: p.RoundingMethod,
            RequiresVerification: p.RequiresVerification
        )).ToList();
    }

    public async Task<ServiceResult> SavePolicyAsync(AttendanceDeductionPolicy policy, int userId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if (policy.AttendanceDeductionPolicyId == 0)
        {
            policy.ApprovedByUserId = userId;
            policy.ApprovedAtUtc = DateTime.UtcNow;
            policy.CreatedAtUtc = DateTime.UtcNow;
            policy.UpdatedAtUtc = DateTime.UtcNow;
            db.AttendanceDeductionPolicies.Add(policy);
        }
        else
        {
            var existing = await db.AttendanceDeductionPolicies.FirstOrDefaultAsync(p => p.AttendanceDeductionPolicyId == policy.AttendanceDeductionPolicyId);
            if (existing == null) return ServiceResult.Fail("Policy not found.");

            existing.PolicyCode = policy.PolicyCode;
            existing.PolicyName = policy.PolicyName;
            existing.LegalReference = policy.LegalReference;
            existing.EffectiveFrom = policy.EffectiveFrom;
            existing.EffectiveTo = policy.EffectiveTo;
            existing.IsActive = policy.IsActive;
            existing.IsVerified = policy.IsVerified;
            existing.SalaryDivisor = policy.SalaryDivisor;
            existing.OfficialMinutesSource = policy.OfficialMinutesSource;
            existing.DefaultDailyWorkingMinutes = policy.DefaultDailyWorkingMinutes;
            existing.ArrivalBufferMinutes = policy.ArrivalBufferMinutes;
            existing.BufferEligibilityCategory = policy.BufferEligibilityCategory;
            existing.BufferMakeUpRequired = policy.BufferMakeUpRequired;
            existing.BufferMakeUpWindow = policy.BufferMakeUpWindow;
            existing.BufferExceededRule = policy.BufferExceededRule;
            existing.LateDeductionFormula = policy.LateDeductionFormula;
            existing.AbsenceDeductionFormula = policy.AbsenceDeductionFormula;
            existing.DeductServiceAllowanceOnAbsence = policy.DeductServiceAllowanceOnAbsence;
            existing.ServiceAllowanceComponentCode = policy.ServiceAllowanceComponentCode;
            existing.DeductFromPensionableSalary = policy.DeductFromPensionableSalary;
            existing.RoundingMethod = policy.RoundingMethod;
            existing.RequiresVerification = policy.RequiresVerification;
            existing.ApprovedByUserId = userId;
            existing.ApprovedAtUtc = DateTime.UtcNow;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Policy saved successfully.");
    }

    public async Task<AttendanceDeductionCalculationPreviewDto> PreviewCalculationAsync(
        int organisationId, int jobId, DateOnly workDate, int lateMinutes, string kind)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var policy = await GetEffectivePolicyAsync(organisationId, workDate);

        var comp = await db.EmployeeCompensations.AsNoTracking()
            .Where(c => c.JobId == jobId && c.IsActive &&
                        c.EffectiveFrom <= workDate &&
                        (!c.EffectiveTo.HasValue || c.EffectiveTo.Value >= workDate))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();

        decimal monthlyBasic = comp?.BasicSalary ?? 12000m;
        decimal dailySalary = decimal.Round(monthlyBasic / policy.SalaryDivisor, 2, MidpointRounding.AwayFromZero);
        decimal perMinuteSalary = policy.DefaultDailyWorkingMinutes > 0
            ? decimal.Round(dailySalary / policy.DefaultDailyWorkingMinutes, 6, MidpointRounding.AwayFromZero)
            : 0m;

        int deductibleMinutes = 0;
        string bufferOutcome = "OnTime";

        if (kind == AttendanceDeductionTypes.LateArrival)
        {
            if (lateMinutes <= policy.ArrivalBufferMinutes)
            {
                bufferOutcome = "Within July 2026 30-min buffer (make-up time required)";
                deductibleMinutes = 0;
            }
            else
            {
                bufferOutcome = $"Exceeded 30-min buffer ({lateMinutes} mins late)";
                deductibleMinutes = policy.BufferExceededRule == BufferExceededRules.DeductExcessOnly
                    ? lateMinutes - policy.ArrivalBufferMinutes
                    : lateMinutes;
            }
        }
        else
        {
            bufferOutcome = "Full-day absence";
            deductibleMinutes = policy.DefaultDailyWorkingMinutes;
        }

        decimal lateDeduction = decimal.Round(perMinuteSalary * deductibleMinutes, 2, MidpointRounding.AwayFromZero);
        decimal absenceDeduction = kind == AttendanceDeductionTypes.FullDayAbsence ? dailySalary : 0m;

        return new AttendanceDeductionCalculationPreviewDto(
            DeductibleMinutes: deductibleMinutes,
            DailySalary: dailySalary,
            PerMinuteSalary: perMinuteSalary,
            EstimatedLateDeduction: lateDeduction,
            EstimatedAbsenceDeduction: absenceDeduction,
            TotalEstimatedDeduction: lateDeduction + absenceDeduction,
            ReducesPensionBase: policy.DeductFromPensionableSalary,
            PolicyCode: policy.PolicyCode,
            LegalReference: policy.LegalReference,
            BufferOutcome: bufferOutcome
        );
    }

    public static (decimal dailySalary, decimal perMinuteSalary, decimal deductionAmount) CalculateLateness(
        decimal monthlyBasicSalary, int officialDailyWorkingMinutes, int deductibleMinutes, int salaryDivisor = 30)
    {
        decimal dailySalary = decimal.Round(monthlyBasicSalary / salaryDivisor, 2, MidpointRounding.AwayFromZero);
        decimal perMinuteSalary = officialDailyWorkingMinutes > 0
            ? decimal.Round(dailySalary / officialDailyWorkingMinutes, 6, MidpointRounding.AwayFromZero)
            : 0m;
        decimal deduction = decimal.Round(perMinuteSalary * deductibleMinutes, 2, MidpointRounding.AwayFromZero);
        return (dailySalary, perMinuteSalary, deduction);
    }

    public static (decimal dailyBasic, decimal dailyService, decimal totalDeduction) CalculateFullDayAbsence(
        decimal monthlyBasicSalary, decimal monthlyServiceAllowance, int salaryDivisor = 30)
    {
        decimal dailyBasic = decimal.Round(monthlyBasicSalary / salaryDivisor, 2, MidpointRounding.AwayFromZero);
        decimal dailyService = decimal.Round(monthlyServiceAllowance / salaryDivisor, 2, MidpointRounding.AwayFromZero);
        return (dailyBasic, dailyService, dailyBasic + dailyService);
    }
}
