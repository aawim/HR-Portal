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
        var currentRun = await db.PayrollRuns.Include(r => r.PayrollPeriod)
            .SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId)
            ?? throw new InvalidOperationException("Payroll run not found in this organisation.");
        if (currentRun.RunType != PayrollRunType.RegularMonthly)
            throw new InvalidOperationException("Supplementary, arrears and final-settlement runs need incremental earnings and same-month tax aggregation; automatic full monthly calculation is not supported for these run types.");
        if (await db.PayrollRuns.AnyAsync(r => r.OrganisationId == organisationId && r.PayrollRunId != payrollRunId &&
            r.PayrollPeriod!.PeriodYear == periodStart.Year && r.PayrollPeriod.PeriodMonth == periodStart.Month &&
            r.Status >= PayrollRunStatus.Finalised && r.Status <= PayrollRunStatus.Closed))
            throw new InvalidOperationException("A finalised payroll already exists for this month. Do not pay the full monthly salary again.");

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
            .ThenInclude(i => i!.Country)
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
        var individualIds = jobs.Select(j => j.IndividualID).Distinct().ToList();
        var identityCards = await db.Idcards.AsNoTracking().Where(c => individualIds.Contains(c.BusinessEntityId))
            .OrderByDescending(c => c.ExpiryDate).ThenByDescending(c => c.IdcardId).ToListAsync();
        var passports = await db.Passports.AsNoTracking().Where(c => individualIds.Contains(c.BusinessEntityId))
            .OrderByDescending(c => c.ExpiryDate).ThenByDescending(c => c.PassportId).ToListAsync();

        // 3. Compensation and position framework info
        var compensations = await db.EmployeeCompensations
            .AsNoTracking()
            .Where(c => c.OrganisationId == organisationId && jobIds.Contains(c.JobId) && c.IsActive &&
                        c.EffectiveFrom <= periodEnd && (!c.EffectiveTo.HasValue || c.EffectiveTo.Value >= periodStart))
            .ToListAsync();

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

        var statutoryPolicy = await PayrollStatutoryPolicyLoader.LoadAsync(db, organisationId, periodEnd);
        var previousEmployees = await db.PayrollEmployees.AsNoTracking()
            .Include(e => e.Lines).Include(e => e.PayrollRun).ThenInclude(r => r!.PayrollPeriod)
            .Where(e => e.PayrollRun!.OrganisationId == organisationId &&
                e.PayrollRun.Status >= PayrollRunStatus.Finalised && e.PayrollRun.Status <= PayrollRunStatus.Closed &&
                e.PayrollRun.PayrollPeriod!.PeriodYear == periodStart.Year &&
                e.PayrollRun.PayrollPeriod.PeriodMonth < periodStart.Month).ToListAsync();
        decimal overtimeMultiplier = statutoryPolicy.NormalOvertimeMultiplier;
        decimal holidayOvertimeMultiplier = statutoryPolicy.HolidayOvertimeMultiplier;
        decimal overtimeHourlyDivisor = statutoryPolicy.OvertimeHourlyDivisor;

        var holidayDates = await db.OrganisationCalendarExceptions
            .AsNoTracking()
            .Where(e => e.OrganisationBusinessEntityId == organisationId && e.IsActive && e.IsApproved &&
                        e.EffectiveDate <= periodEndDt && e.EffectiveEndDate >= periodStartDt &&
                        e.CalendarImpactType == "MarkAsNonWorkingDay")
            .Select(e => new { e.EffectiveDate, e.EffectiveEndDate })
            .ToListAsync();

        // 5. Approved Overtime
        var overtimeClaims = await db.OvertimeRequests
            .AsNoTracking()
            .Include(o => o.Request)
            .Where(o => o.OrganisationId == organisationId && jobIds.Contains(o.JobId) &&
                        o.Request.RequestStateId == 4 && !o.PaidAtUtc.HasValue &&
                        o.WorkDate >= periodStartDt && o.WorkDate <= periodEndDt &&
                        !db.OvertimePayrollItems.Any(i => i.RequestId == o.RequestId))
            .ToListAsync();

        // 5b. Independent Staff Group OT rate multiplier overrides (effective-dated per OT work date)
        var staffGroupOtRates = db.Model.FindEntityType(typeof(StaffGroupMember)) != null && overtimeClaims.Count > 0
            ? await db.StaffGroupMembers.AsNoTracking()
                .Where(m => m.OrganisationId == organisationId && jobIds.Contains(m.JobId) &&
                            m.StaffGroup.OvertimeRateMultiplier.HasValue && m.StaffGroup.OvertimeRateMultiplier > 0 &&
                            m.StaffGroup.EffectiveFrom <= periodEndDt &&
                            (!m.StaffGroup.EffectiveTo.HasValue || m.StaffGroup.EffectiveTo >= periodStartDt))
                .Select(m => new
                {
                    m.JobId,
                    m.IndividualId,
                    m.IsActive,
                    m.JoinedAtUtc,
                    m.LeftAtUtc,
                    m.StaffGroup.EffectiveFrom,
                    m.StaffGroup.EffectiveTo,
                    m.StaffGroup.IsDissolved,
                    m.StaffGroup.DissolvedAtUtc,
                    Multiplier = m.StaffGroup.OvertimeRateMultiplier!.Value
                })
                .ToListAsync()
            : [];

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

        // 7b. Staff Goods Loan Installments
        var goodsLoanInstallments = await (from inst in db.GoodsLoanInstallments.AsNoTracking()
                                           join loan in db.GoodsLoans.AsNoTracking() on inst.LoanID equals loan.LoanID
                                           join store in db.GoodsLoanStores.AsNoTracking() on loan.StoreID equals store.StoreID
                                           where loan.OrganisationBusinessEntityID == organisationId
                                                 && jobIds.Contains(loan.JobID)
                                                 && (loan.Status == "ACTIVE" || loan.Status == "APPROVED")
                                                 && inst.DueMonth <= periodEndDt
                                                 && inst.Status != "PAID" && inst.Status != "CANCELLED"
                                           select new
                                           {
                                               Installment = inst,
                                               Loan = loan,
                                               Store = store
                                           }).ToListAsync();

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

            var employeeCompensations = compensations.Where(c => c.JobId == job.JobId).OrderByDescending(c => c.EffectiveFrom).ToList();
            var comp = employeeCompensations.FirstOrDefault();
            var validationErrors = new List<string>();
            if (employeeCompensations.Count > 1 || (comp != null && comp.EffectiveFrom > periodStart && job.JoinedDate < comp.EffectiveFrom.ToDateTime(TimeOnly.MinValue)))
                validationErrors.Add("Compensation changes within the period require salary segment calculation; review before payroll.");
            var taxProfiles = statutoryPolicy.EmployeeTaxProfiles?.Where(p => p.IndividualId == job.IndividualID &&
                p.EffectiveFrom <= periodEnd && (!p.EffectiveTo.HasValue || p.EffectiveTo >= periodEnd)).ToList() ?? [];
            if (taxProfiles.Count > 1) validationErrors.Add("Overlapping employee tax declarations.");
            var taxProfile = taxProfiles.FirstOrDefault();
            var openingThrough = taxProfile?.OpeningBalanceThrough;
            if (openingThrough.HasValue && (openingThrough.Value.Year != periodStart.Year || openingThrough >= periodStart))
                validationErrors.Add("Tax opening balances must be in the payroll tax year and end before this payroll month.");
            var taxHistory = previousEmployees.Where(e => e.IndividualId == job.IndividualID &&
                    (!openingThrough.HasValue || e.PayrollRun!.PayrollPeriod!.PeriodMonth > openingThrough.Value.Month))
                .GroupBy(e => e.PayrollRun!.PayrollPeriod!.PeriodMonth)
                .Select(g => new PayrollTaxMonth(g.Key, g.Sum(PayrollReportValues.TaxableBase),
                    g.Sum(e => e.Lines.Where(l => l.ComponentCode == "TAX_WHT").Sum(l => l.CalculatedAmount)))).ToList();
            var firstHistoryMonth = openingThrough?.AddDays(1).Month ?? 1;
            bool historyComplete = Enumerable.Range(firstHistoryMonth, Math.Max(0, periodStart.Month - firstHistoryMonth))
                .All(month => taxHistory.Any(h => h.Month == month));
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
                    IsPensionable: statutoryPolicy.AllowancesArePensionable,
                    IsProratable: true,
                    FromDate: DateOnly.FromDateTime(a.FromDate),
                    ToDate: a.ToDate.HasValue ? DateOnly.FromDateTime(a.ToDate.Value) : null
                ));
            }

            // OT claims (Employment Act Sec. 37-38: 1.25x normal working day, 1.50x Friday & official public holiday,
            // plus any active Independent Staff Group OvertimeRateMultiplier on the OT work date)
            var empOt = new List<InputOvertimeItem>();
            foreach (var ot in overtimeClaims.Where(x => x.JobId == job.JobId))
            {
                var otDate = ot.WorkDate.Date;
                bool isHolidayOrFriday = otDate.DayOfWeek == DayOfWeek.Friday ||
                    holidayDates.Any(h => otDate >= h.EffectiveDate.Date && otDate <= h.EffectiveEndDate.Date);
                decimal appliedMultiplier = isHolidayOrFriday ? holidayOvertimeMultiplier : overtimeMultiplier;
                var groupMultiplier = staffGroupOtRates
                    .Where(g => (g.JobId == job.JobId || g.IndividualId == job.IndividualID) &&
                                g.EffectiveFrom.Date <= otDate &&
                                (!g.EffectiveTo.HasValue || g.EffectiveTo.Value.Date >= otDate) &&
                                (!g.IsDissolved || (g.DissolvedAtUtc.HasValue && g.DissolvedAtUtc.Value.Date >= otDate)) &&
                                (g.IsActive || (g.LeftAtUtc.HasValue && g.LeftAtUtc.Value.Date >= otDate)))
                    .Select(g => (decimal?)g.Multiplier)
                    .Max();
                if (groupMultiplier.HasValue && groupMultiplier.Value > appliedMultiplier)
                    appliedMultiplier = groupMultiplier.Value;

                decimal hourlyRate = basicSalary > 0 && overtimeHourlyDivisor > 0
                    ? (basicSalary / overtimeHourlyDivisor) * appliedMultiplier
                    : 0m;
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
                var employmentStart = DateOnly.FromDateTime(job.JoinedDate);
                var employmentEnd = job.TerminatedDate.HasValue ? DateOnly.FromDateTime(job.TerminatedDate.Value) : periodEnd;
                if (from < employmentStart) from = employmentStart;
                if (to > employmentEnd) to = employmentEnd;
                if (to < from) continue;
                int days = to.DayNumber - from.DayNumber + 1;

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

            // Staff Goods Loan deductions
            foreach (var glInst in goodsLoanInstallments.Where(x => x.Loan.JobID == job.JobId))
            {
                var outstanding = glInst.Installment.ScheduledAmount - glInst.Installment.PaidAmount;
                if (outstanding <= 0) continue;
                empDeductions.Add(new InputDeductionAgreementItem(
                    InstalmentId: glInst.Installment.InstallmentID,
                    AgreementId: glInst.Loan.LoanID,
                    Reference: $"{glInst.Loan.LoanNumber} ({glInst.Store.StoreName})",
                    Category: "GoodsLoan",
                    DueAmount: outstanding
                ));
            }

            // Cash adjustments, including Ramadan allowance, are taxable remuneration.
            var empAdjustments = new List<InputAdjustmentItem>();
            foreach (var adj in adjustments.Where(x => x.JobId == job.JobId))
            {
                empAdjustments.Add(new InputAdjustmentItem(
                    AdjustmentId: adj.PayrollAdjustmentId,
                    ComponentCode: adj.ComponentCode,
                    Description: adj.Description,
                    Amount: adj.Amount,
                    IsAddition: adj.IsAddition,
                    IsTaxable: true
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

            bool isMaldivian = individual?.Country == null ||
                string.Equals(individual.Country.Isoalpha2Code, "MV", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(individual.Country.NameEnglish, "Maldives", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(individual.Country.NationalityEnglish, "Maldivian", StringComparison.OrdinalIgnoreCase);

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
                BankName: comp?.BankName ?? string.Empty,
                BankBranch: comp?.BankBranch ?? string.Empty,
                BankAccountNumber: comp?.BankAccountNumber ?? string.Empty,
                AccountHolderName: comp?.AccountHolderName ?? employeeName,
                JoinedDate: DateOnly.FromDateTime(job.JoinedDate),
                TerminatedDate: job.TerminatedDate.HasValue ? DateOnly.FromDateTime(job.TerminatedDate.Value) : null,
                Allowances: empAllowances,
                OvertimeClaims: empOt,
                UnpaidLeaves: empLeaves,
                Deductions: empDeductions,
                Adjustments: empAdjustments,
                AttendanceDeductions: empAttDeductions,
                IsMaldivianCitizen: isMaldivian,
                DateOfBirth: individual?.DateOfBirth.HasValue == true ? DateOnly.FromDateTime(individual.DateOfBirth.Value) : null,
                FrozenPolicy: statutoryPolicy with { EmployeeTaxProfiles = null },
                TaxContext: new PayrollTaxContext(taxProfile, taxHistory, historyComplete),
                NationalityVerified: individual?.Country != null,
                ValidationErrors: validationErrors,
                BankIdentityNumber: isMaldivian ? identityCards.FirstOrDefault(c => c.BusinessEntityId == job.IndividualID)?.IdcardNumber ?? "" :
                    passports.FirstOrDefault(c => c.BusinessEntityId == job.IndividualID)?.PassportNumber ?? ""
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
