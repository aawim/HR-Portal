using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Payroll;

public sealed class PayrollSnapshotService : IPayrollSnapshotService
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly ILogger<PayrollSnapshotService> _logger;

    public PayrollSnapshotService(IDbContextFactory<HrmTeContext> factory, ILogger<PayrollSnapshotService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<EmployeePayrollInputItem>> CreateOrGetRunSnapshotsAsync(
        int organisationId, int payrollRunId, DateOnly periodStart, DateOnly periodEnd)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // 1. Check if snapshots already exist for this run
        var existingSnapshots = await db.PayrollInputSnapshots
            .AsNoTracking()
            .Where(s => s.PayrollRunId == payrollRunId)
            .ToListAsync();

        if (existingSnapshots.Count > 0)
        {
            var deserialized = new List<EmployeePayrollInputItem>();
            var seenIndividuals = new HashSet<int>();
            foreach (var s in existingSnapshots)
            {
                var item = JsonSerializer.Deserialize<EmployeePayrollInputItem>(s.SnapshotJson);
                if (item != null && seenIndividuals.Add(item.IndividualId))
                {
                    deserialized.Add(item);
                }
            }
            return deserialized;
        }

        // 2. Query active jobs in the organisation
        var periodStartDt = periodStart.ToDateTime(TimeOnly.MinValue);
        var periodEndDt = periodEnd.ToDateTime(TimeOnly.MaxValue);

        var allJobs = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Individual)
            .Where(j => j.OrganisationID == organisationId &&
                        j.JobStateId == 4 && // Approved active job
                        j.JoinedDate <= periodEndDt &&
                        (!j.TerminatedDate.HasValue || j.TerminatedDate.Value >= periodStartDt))
            .ToListAsync();

        // Ensure 1 active job per individual (pick the most recently joined/updated job)
        var jobs = allJobs
            .GroupBy(j => j.IndividualID)
            .Select(g => g.OrderByDescending(j => j.JoinedDate).ThenByDescending(j => j.JobId).First())
            .ToList();

        var jobIds = jobs.Select(j => j.JobId).ToList();

        // 3. Compensation and position framework info
        var compensations = await db.EmployeeCompensations
            .AsNoTracking()
            .Where(c => c.OrganisationId == organisationId && jobIds.Contains(c.JobId) && c.IsActive &&
                        c.EffectiveFrom <= periodEnd && (!c.EffectiveTo.HasValue || c.EffectiveTo.Value >= periodStart))
            .ToDictionaryAsync(c => c.JobId);

        var stepHistories = await db.FrameworkJobStepHistories
            .AsNoTracking()
            .Where(s => jobIds.Contains(s.JobId) && s.EffectiveFrom <= periodEndDt && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= periodStartDt))
            .OrderByDescending(s => s.EffectiveFrom)
            .ToListAsync();

        var jobPositions = await db.JobPositions
            .AsNoTracking()
            .Include(jp => jp.Position)
            .Where(jp => jobIds.Contains(jp.JobId) && jp.JobPositionStateId == 4 && jp.FromDate <= periodEndDt && (!jp.ToDate.HasValue || jp.ToDate.Value >= periodStartDt))
            .ToListAsync();

        // 4. Allowances
        var staffAllowances = await db.StaffAllowances
            .AsNoTracking()
            .Where(a => a.OrganisationId == organisationId && jobIds.Contains(a.JobId) && a.FromDate <= periodEndDt && (!a.ToDate.HasValue || a.ToDate.Value >= periodStartDt))
            .ToListAsync();

        var allowanceTypes = await db.AllowanceTypes
            .AsNoTracking()
            .Where(t => t.OrganisationId == organisationId)
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        var overtimeMultiplier = await db.CalculationRules.AsNoTracking()
            .Where(r => r.OrganisationId == organisationId && r.IsActive && r.RuleCode == "OT_STANDARD" &&
                        r.EffectiveFrom <= periodEnd && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= periodStart))
            .OrderByDescending(r => r.EffectiveFrom)
            .Select(r => (decimal?)r.PercentageValue)
            .FirstOrDefaultAsync() ?? 1.50m;
        if (overtimeMultiplier <= 0) overtimeMultiplier = 1.50m;

        // 5. Approved Overtime
        var overtimeClaims = await db.OvertimeRequests
            .AsNoTracking()
            .Include(o => o.Request)
            .Where(o => o.OrganisationId == organisationId && jobIds.Contains(o.JobId) &&
                        o.Request.RequestStateId == 4 && !o.PaidAtUtc.HasValue &&
                        o.WorkDate >= periodStartDt && o.WorkDate <= periodEndDt &&
                        !db.OvertimePayrollItems.Any(i => i.RequestId == o.RequestId))
            .ToListAsync();

        // 6. Unpaid Leave
        var leaves = await db.Leaves
            .AsNoTracking()
            .Include(l => l.LeaveType)
            .Where(l => jobIds.Contains(l.JobId) && l.LeaveStateId == 4 && l.FromDate <= periodEndDt && l.ToDate >= periodStartDt && (!l.LeaveType.IncludePay || (l.LeaveType.PayPercentage.HasValue && l.LeaveType.PayPercentage < 100)))
            .ToListAsync();

        // 7. Deductions
        var instalments = await (from i in db.DeductionInstalments.AsNoTracking()
                                 join a in db.DeductionAgreements.AsNoTracking() on i.AgreementId equals a.Id
                                 where a.OrganisationId == organisationId && jobIds.Contains(a.JobId) && !i.IsSuperseded && i.DueDate <= periodEndDt
                                 select new { Instalment = i, Agreement = a }).ToListAsync();
        var instalmentIds = instalments.Select(x => x.Instalment.Id).ToList();
        var allocatedAmounts = instalmentIds.Count == 0
            ? new Dictionary<long, decimal>()
            : await db.DeductionPaymentAllocations.AsNoTracking()
                .Where(x => instalmentIds.Contains(x.InstalmentId))
                .GroupBy(x => x.InstalmentId)
                .Select(g => new { InstalmentId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.InstalmentId, x => x.Amount);

        // 8. Adjustments
        var adjustments = await db.PayrollAdjustments
            .AsNoTracking()
            .Where(adj => adj.OrganisationId == organisationId && jobIds.Contains(adj.JobId) && (adj.PayrollRunId == null || adj.PayrollRunId == payrollRunId))
            .ToListAsync();

        // 9. Verified Attendance Deductions (Maldives Civil Service lateness, full-day absence, partial/early absence)
        var attendanceAdjustments = await db.AttendanceAdjustments
            .Where(a => a.OrganisationId == organisationId &&
                        jobIds.Contains(a.JobId) &&
                        a.WorkDate >= periodStart && a.WorkDate <= periodEnd &&
                        a.VerificationStatus == "Verified" &&
                        (a.PayrollProcessingStatus == "Pending" || a.PayrollRunId == payrollRunId))
            .ToListAsync();

        var inputItems = new List<EmployeePayrollInputItem>();
        var snapshotsToSave = new List<PayrollInputSnapshot>();

        foreach (var job in jobs)
        {
            var individual = job.Individual;
            string employeeName = individual != null ? $"{individual.FirstNameEnglish} {individual.LastNameEnglish}".Trim() : $"Staff #{job.IndividualID}";
            string employeeNumber = $"EMP-{job.IndividualID}";

            var comp = compensations.GetValueOrDefault(job.JobId);
            var step = stepHistories.FirstOrDefault(s => s.JobId == job.JobId);
            var jp = jobPositions.FirstOrDefault(p => p.JobId == job.JobId);

            decimal basicSalary = comp?.BasicSalary ?? step?.BasicSalarySnapshot ?? job.BasicSalary ?? 0m;
            string? gradeCode = comp?.GradeCode;
            int? stepNumber = comp?.StepNumber;
            string departmentName = "General";
            string positionTitle = jp?.Position?.Name ?? "Employee";

            // Allowances for this employee
            var empAllowances = new List<InputAllowanceItem>();
            foreach (var a in staffAllowances.Where(x => x.JobId == job.JobId))
            {
                string name = allowanceTypes.GetValueOrDefault(a.AllowanceTypeId, "Allowance");
                empAllowances.Add(new InputAllowanceItem(
                    AllowanceId: a.Id,
                    ComponentCode: $"ALLW_{a.AllowanceTypeId}",
                    Name: name,
                    Type: SalaryComponentType.FixedAllowance,
                    Amount: a.Amount,
                    IsTaxable: true,
                    IsPensionable: true,
                    IsProratable: true,
                    FromDate: DateOnly.FromDateTime(a.FromDate),
                    ToDate: a.ToDate.HasValue ? DateOnly.FromDateTime(a.ToDate.Value) : null
                ));
            }

            // OT claims
            var empOt = new List<InputOvertimeItem>();
            foreach (var ot in overtimeClaims.Where(x => x.JobId == job.JobId))
            {
                decimal hourlyRate = basicSalary > 0 ? (basicSalary / 160m) * overtimeMultiplier : 0m;
                decimal otAmount = decimal.Round(hourlyRate * ((decimal)ot.Minutes / 60m), 2, MidpointRounding.AwayFromZero);
                empOt.Add(new InputOvertimeItem(
                    RequestId: ot.RequestId,
                    RequestReference: ot.Request?.ReferenceNumber ?? $"OT-{ot.RequestId}",
                    WorkDate: DateOnly.FromDateTime(ot.WorkDate),
                    Minutes: ot.Minutes,
                    HourlyRate: decimal.Round(hourlyRate, 2, MidpointRounding.AwayFromZero),
                    Amount: otAmount
                ));
            }

            // Leaves
            var empLeaves = new List<InputLeaveDeductionItem>();
            foreach (var l in leaves.Where(x => x.JobId == job.JobId))
            {
                var from = DateOnly.FromDateTime(l.FromDate) < periodStart ? periodStart : DateOnly.FromDateTime(l.FromDate);
                var to = DateOnly.FromDateTime(l.ToDate) > periodEnd ? periodEnd : DateOnly.FromDateTime(l.ToDate);
                int days = (to.Day - from.Day) + 1;
                if (days < 1) days = 1;

                decimal payRatio = l.LeaveType.PayPercentage.HasValue ? (decimal)l.LeaveType.PayPercentage.Value / 100m : 0m;
                decimal deductionRate = 1.0m - payRatio;

                empLeaves.Add(new InputLeaveDeductionItem(
                    LeaveId: l.LeaveId,
                    LeaveTypeName: l.LeaveType.Name,
                    FromDate: from,
                    ToDate: to,
                    CalendarDays: days,
                    DeductionRate: deductionRate
                ));
            }

            // Deductions
            var empDeductions = new List<InputDeductionAgreementItem>();
            foreach (var inst in instalments.Where(x => x.Agreement.JobId == job.JobId))
            {
                var outstanding = inst.Instalment.Amount - allocatedAmounts.GetValueOrDefault(inst.Instalment.Id);
                if (outstanding <= 0) continue;
                empDeductions.Add(new InputDeductionAgreementItem(
                    InstalmentId: inst.Instalment.Id,
                    AgreementId: inst.Agreement.Id,
                    Reference: inst.Agreement.Reference,
                    Category: inst.Agreement.Category,
                    DueAmount: outstanding
                ));
            }

            // Adjustments
            var empAdjustments = new List<InputAdjustmentItem>();
            foreach (var adj in adjustments.Where(x => x.JobId == job.JobId))
            {
                empAdjustments.Add(new InputAdjustmentItem(
                    AdjustmentId: adj.PayrollAdjustmentId,
                    ComponentCode: adj.ComponentCode,
                    Description: adj.Description,
                    Amount: adj.Amount,
                    IsAddition: adj.IsAddition
                ));
            }

            // Attendance Deductions
            var empAttDeductions = new List<InputAttendanceDeductionItem>();
            foreach (var att in attendanceAdjustments.Where(x => x.JobId == job.JobId))
            {
                if (att.BasicSalaryDeductionAmount > 0)
                {
                    empAttDeductions.Add(new InputAttendanceDeductionItem(
                        AttendanceAdjustmentId: att.AttendanceAdjustmentId,
                        SourceReference: att.SourceAttendanceReference,
                        WorkDate: att.WorkDate,
                        DeductibleMinutes: att.DeductibleMinutes,
                        ScheduledMinutes: att.ScheduledMinutes,
                        ActualMinutes: att.ActualMinutes,
                        DeductionType: att.AdjustmentType,
                        DeductionAmount: att.BasicSalaryDeductionAmount,
                        AffectsBasicSalary: true,
                        AffectsServiceAllowance: false,
                        PolicyCode: att.PolicyCode,
                        LegalReference: att.RegulatorySource));
                }
                if (att.ServiceAllowanceDeductionAmount > 0)
                {
                    empAttDeductions.Add(new InputAttendanceDeductionItem(
                        AttendanceAdjustmentId: att.AttendanceAdjustmentId,
                        SourceReference: att.SourceAttendanceReference,
                        WorkDate: att.WorkDate,
                        DeductibleMinutes: att.DeductibleMinutes,
                        ScheduledMinutes: att.ScheduledMinutes,
                        ActualMinutes: att.ActualMinutes,
                        DeductionType: "FullDayAbsence_ServiceAllowance",
                        DeductionAmount: att.ServiceAllowanceDeductionAmount,
                        AffectsBasicSalary: false,
                        AffectsServiceAllowance: true,
                        PolicyCode: att.PolicyCode,
                        LegalReference: att.RegulatorySource));
                }

                att.PayrollProcessingStatus = "Ingested";
                att.PayrollRunId = payrollRunId;
                att.UpdatedAtUtc = DateTime.UtcNow;
            }

            var inputItem = new EmployeePayrollInputItem(
                JobId: job.JobId,
                IndividualId: job.IndividualID,
                EmployeeNumber: employeeNumber,
                EmployeeName: employeeName,
                DepartmentName: departmentName,
                PositionTitle: positionTitle,
                GradeCode: gradeCode,
                StepNumber: stepNumber,
                BasicSalary: basicSalary,
                CurrencyTypeId: comp?.CurrencyTypeId ?? 1,
                BankName: comp?.BankName ?? "Bank of Maldives",
                BankBranch: comp?.BankBranch ?? "Male Branch",
                BankAccountNumber: comp?.BankAccountNumber ?? $"7701{job.IndividualID:D8}",
                AccountHolderName: comp?.AccountHolderName ?? employeeName,
                JoinedDate: DateOnly.FromDateTime(job.JoinedDate),
                TerminatedDate: job.TerminatedDate.HasValue ? DateOnly.FromDateTime(job.TerminatedDate.Value) : null,
                Allowances: empAllowances,
                OvertimeClaims: empOt,
                UnpaidLeaves: empLeaves,
                Deductions: empDeductions,
                Adjustments: empAdjustments,
                AttendanceDeductions: empAttDeductions
            );

            inputItems.Add(inputItem);

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(inputItem);
            var hash = Convert.ToHexString(SHA256.HashData(jsonBytes));

            snapshotsToSave.Add(new PayrollInputSnapshot
            {
                PayrollRunId = payrollRunId,
                JobId = job.JobId,
                IndividualId = job.IndividualID,
                SnapshotVersion = 1,
                SnapshotHash = hash,
                SnapshotJson = Encoding.UTF8.GetString(jsonBytes),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        db.PayrollInputSnapshots.AddRange(snapshotsToSave);
        await db.SaveChangesAsync();

        _logger.LogInformation("Saved {Count} frozen snapshots for PayrollRun {RunId}", snapshotsToSave.Count, payrollRunId);
        return inputItems;
    }

    public async Task<List<PayrollInputSnapshot>> GetSnapshotsForRunAsync(int payrollRunId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.PayrollInputSnapshots
            .AsNoTracking()
            .Where(s => s.PayrollRunId == payrollRunId)
            .ToListAsync();
    }
}
