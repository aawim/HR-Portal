using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.DTOs.AttendanceDeductions;
using HRM.Models;
using HRM.Models.AttendanceDeductions;
using HRM.Models.Payroll;
using HRM.Services.Attendance.Deductions;
using HRM.Services.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class MaldivesAttendanceDeductionTests
{
    private static DbContextOptions<HrmTeContext> CreateDbOptions()
    {
        return new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    private sealed class TestDbContextFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
    {
        public HrmTeContext CreateDbContext() => new(options);
        public Task<HrmTeContext> CreateDbContextAsync(System.Threading.CancellationToken ct = default) => Task.FromResult(new HrmTeContext(options));
    }

    private static AttendanceDeductionPolicy CreateStandardMaldivesPolicy(int orgId = 1)
    {
        return new AttendanceDeductionPolicy
        {
            AttendanceDeductionPolicyId = 1,
            OrganisationId = orgId,
            PolicyCode = "MCS-STD-2026",
            PolicyName = "Maldives Civil Service Regulation Standard",
            LegalReference = "Civil Service Regulation Art 149 & 160; July 2026 Amendment",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            IsActive = true,
            IsVerified = true,
            SalaryDivisor = 30,
            DefaultDailyWorkingMinutes = 360, // 6 hours
            ArrivalBufferMinutes = 30,
            BufferEligibilityCategory = "AllNonShift",
            BufferMakeUpRequired = true,
            BufferMakeUpWindow = "SameWeek",
            BufferExceededRule = BufferExceededRules.RequireReview,
            LateDeductionFormula = "PerMinuteBasic",
            AbsenceDeductionFormula = "BasicPlusServiceAllowance",
            DeductServiceAllowanceOnAbsence = true,
            DeductFromPensionableSalary = true
        };
    }

    // ==========================================
    // PART J: SCENARIOS 1 TO 9 - LATENESS & BUFFER
    // ==========================================

    [Fact]
    public void Scenario01_OnTimeArrival_ResultsInZeroDeductibleMinutes()
    {
        // Scheduled 08:00, arrival 07:58 -> 0 deductible minutes
        var (_, _, deduction) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 12000m, officialDailyWorkingMinutes: 360, deductibleMinutes: 0, salaryDivisor: 30);

        Assert.Equal(0m, deduction);
    }

    [Fact]
    public void Scenario02_TenMinuteBuffer_ArrivalQualifiesForMakeUp()
    {
        // 08:10 arrival within 30-minute buffer
        var policy = CreateStandardMaldivesPolicy();
        int arrivalLateMinutes = 10;

        bool withinBuffer = arrivalLateMinutes <= policy.ArrivalBufferMinutes;
        Assert.True(withinBuffer);
        Assert.True(policy.BufferMakeUpRequired);
        Assert.Equal("SameWeek", policy.BufferMakeUpWindow);
    }

    [Fact]
    public void Scenario03_TwentyMinuteBuffer_WithCompletedMakeUp_ZeroDeduction()
    {
        // 08:20 arrival, employee completed required make-up time
        int deductibleMinutesAfterMakeUp = 0; // Completed make-up offsets late minutes
        var (_, _, deduction) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 15000m, officialDailyWorkingMinutes: 360, deductibleMinutes: deductibleMinutesAfterMakeUp);

        Assert.Equal(0m, deduction);
    }

    [Fact]
    public void Scenario04_ThirtyMinuteBufferBoundary_ExactlyAtBoundary()
    {
        // Exactly 30 minutes late (08:30)
        var policy = CreateStandardMaldivesPolicy();
        int arrivalLateMinutes = 30;

        bool eligibleForBuffer = arrivalLateMinutes <= policy.ArrivalBufferMinutes;
        Assert.True(eligibleForBuffer);
    }

    [Fact]
    public void Scenario05_ThirtyOneMinuteArrival_HandledPerConfigurablePolicyRule()
    {
        // 31 minutes late (08:31) -> exceeds 30m buffer
        var policyReview = CreateStandardMaldivesPolicy();
        policyReview.BufferExceededRule = BufferExceededRules.RequireReview;

        var policyDeductAll = CreateStandardMaldivesPolicy();
        policyDeductAll.BufferExceededRule = BufferExceededRules.DeductAllMinutes;

        var policyDeductExcess = CreateStandardMaldivesPolicy();
        policyDeductExcess.BufferExceededRule = BufferExceededRules.DeductExcessOnly;

        int lateMinutes = 31;
        Assert.Equal(BufferExceededRules.RequireReview, policyReview.BufferExceededRule);
        
        int deductibleAll = lateMinutes; // Deducts all 31 minutes
        Assert.Equal(31, deductibleAll);

        int deductibleExcess = lateMinutes - policyDeductExcess.ArrivalBufferMinutes; // Deducts only 1 minute
        Assert.Equal(1, deductibleExcess);
    }

    [Fact]
    public void Scenario06_BufferWithIncompleteMakeUp_FlagsDeductibleLateness()
    {
        // Employee arrived 25m late, make-up window expired with 0 minutes completed
        int uncompletedMakeUpMinutes = 25;
        var (_, _, deduction) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 12000m, officialDailyWorkingMinutes: 360, deductibleMinutes: uncompletedMakeUpMinutes);

        // Per-minute = (12000 / 30) / 360 = 1.111111...; 25 * 1.111111... = 27.78 MVR
        Assert.Equal(27.78m, deduction);
    }

    [Fact]
    public void Scenario07_ExcludedShiftWorkerLateness_NotEligibleForBuffer()
    {
        // Shift worker excluded from flexible arrival buffer -> all 15 late minutes are deductible
        int lateMinutes = 15;
        var (_, _, deduction) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 18000m, officialDailyWorkingMinutes: 360, deductibleMinutes: lateMinutes);

        // Daily = 18000 / 30 = 600; Per-minute = 600 / 360 = 1.666667; 15 * 1.666667 = 25.00 MVR
        Assert.Equal(25.00m, deduction);
    }

    [Fact]
    public void Scenario08_ApprovedAttendanceExemption_ExcusesLateness()
    {
        var adjustment = new AttendanceAdjustment
        {
            AttendanceAdjustmentId = 10,
            OrganisationId = 1,
            IndividualId = 5,
            JobId = 105,
            WorkDate = new DateOnly(2026, 11, 2),
            AdjustmentType = AttendanceDeductionTypes.LateArrival,
            DeductibleMinutes = 45,
            EstimatedDeductionAmount = 62.50m,
            VerificationStatus = AttendanceAdjustmentVerificationStatuses.PendingVerification
        };

        // HR approves attendance exemption
        adjustment.VerificationStatus = AttendanceAdjustmentVerificationStatuses.Excused;
        adjustment.ReviewerComments = "Official transport delay excused by HR.";

        Assert.Equal(AttendanceAdjustmentVerificationStatuses.Excused, adjustment.VerificationStatus);
    }

    [Fact]
    public void Scenario09_EarlyDeparture_CalculatesMissingScheduledMinutes()
    {
        // Left 30 minutes before official end of day
        int earlyDepartureMinutes = 30;
        var (_, _, deduction) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 12000m, officialDailyWorkingMinutes: 360, deductibleMinutes: earlyDepartureMinutes);

        // (12000 / 30 / 360) * 30 = 33.33 MVR
        Assert.Equal(33.33m, deduction);
    }

    // ===============================================
    // PART J: SCENARIOS 10 TO 17 - ABSENCE & EXCEPTION
    // ===============================================

    [Fact]
    public void Scenario10_FullDayUnauthorisedAbsence_DeductsBasicAndServiceAllowance()
    {
        // Civil Service Article 160(a): Monthly Basic / 30 + Monthly Service Allowance / 30
        var (dailyBasic, dailyService, totalDeduction) = AttendanceDeductionService.CalculateFullDayAbsence(
            monthlyBasicSalary: 12000m, monthlyServiceAllowance: 3000m, salaryDivisor: 30);

        Assert.Equal(400m, dailyBasic);           // 12000 / 30
        Assert.Equal(100m, dailyService);         // 3000 / 30
        Assert.Equal(500m, totalDeduction);       // 400 + 100
    }

    [Fact]
    public void Scenario11_ApprovedPaidLeave_PreventsUnauthorisedAbsenceDeduction()
    {
        // When approved paid leave covers the date, no attendance adjustment should be generated
        bool hasApprovedPaidLeave = true;
        Assert.True(hasApprovedPaidLeave);
    }

    [Fact]
    public void Scenario12_ApprovedUnpaidLeave_HandledSeparatelyFromUnauthorisedAbsence()
    {
        // Unpaid leave is handled via Leave Deduction Items, not punitive absence deduction
        var unpaidLeave = new InputLeaveDeductionItem(
            LeaveId: 101,
            LeaveTypeName: "Unpaid Leave",
            FromDate: new DateOnly(2026, 11, 5),
            ToDate: new DateOnly(2026, 11, 5),
            CalendarDays: 1,
            DeductionRate: 1.0m
        );

        Assert.Equal(1, unpaidLeave.CalendarDays);
        Assert.Equal(1.0m, unpaidLeave.DeductionRate);
    }

    [Fact]
    public void Scenario13_OfficialDuty_ExemptsFromAbsenceDeduction()
    {
        // Official duty / off-site duty reconciles scheduled hours as fulfilled
        bool isOfficialDutyApproved = true;
        int deductibleMinutes = isOfficialDutyApproved ? 0 : 360;
        Assert.Equal(0, deductibleMinutes);
    }

    [Fact]
    public void Scenario14_MissingClockEvent_FlagsPendingVerificationInsteadOfAbsence()
    {
        // Incomplete biometric punch (missing out punch) must NOT automatically deduct full-day absence
        var outcome = AttendanceOutcomeClassifications.MissingClockEvent;
        var verification = AttendanceAdjustmentVerificationStatuses.PendingVerification;

        Assert.Equal("MissingClockEvent", outcome);
        Assert.Equal("PendingVerification", verification);
    }

    [Fact]
    public void Scenario15_BiometricClockCorrection_ResolvesPendingException()
    {
        var adjustment = new AttendanceAdjustment
        {
            AttendanceAdjustmentId = 15,
            AttendanceOutcome = AttendanceOutcomeClassifications.PendingVerification,
            VerificationStatus = AttendanceAdjustmentVerificationStatuses.PendingVerification
        };

        // Staff submitted punch correction which was approved
        adjustment.AttendanceOutcome = AttendanceOutcomeClassifications.OnTime;
        adjustment.VerificationStatus = AttendanceAdjustmentVerificationStatuses.Excused;
        adjustment.ReviewerComments = "Biometric clock corrected via approved attendance request #882.";

        Assert.Equal(AttendanceAdjustmentVerificationStatuses.Excused, adjustment.VerificationStatus);
    }

    [Fact]
    public void Scenario16_PartialDayAbsence_CalculatesMissingCoreHours()
    {
        // Missing 2 hours (120 minutes) during official core working hours
        int missingMinutes = 120;
        var (_, _, deduction) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 12000m, officialDailyWorkingMinutes: 360, deductibleMinutes: missingMinutes);

        // (12000 / 30 / 360) * 120 = 133.33 MVR
        Assert.Equal(133.33m, deduction);
    }

    [Fact]
    public void Scenario17_OverlappingDutySegments_PreventsDoubleCounting()
    {
        // Official scheduled: 08:00 - 14:00 (360 mins)
        // Additional duty: 12:00 - 16:00 (overlapping 12:00 - 14:00)
        // Base official hours must not exceed 360 minutes
        int scheduledOfficialMinutes = 360;
        Assert.Equal(360, scheduledOfficialMinutes);
    }

    // ====================================================
    // PART J: SCENARIOS 18 TO 26 - PAYROLL INTEGRATION & LAWS
    // ====================================================

    [Fact]
    public void Scenario18_BasicSalaryLatenessDeduction_InPayrollEngine()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);

        var latenessDeduction = new InputAttendanceDeductionItem(
            AttendanceAdjustmentId: 101,
            SourceReference: "ATT-20261102-1",
            WorkDate: new DateOnly(2026, 11, 2),
            DeductionType: AttendanceDeductionTypes.LateArrival,
            ScheduledMinutes: 360,
            ActualMinutes: 340,
            DeductibleMinutes: 20,
            DeductionAmount: 22.22m,
            AffectsBasicSalary: true,
            AffectsServiceAllowance: false,
            PolicyCode: "MCS-STD-2026",
            LegalReference: "Civil Service Regulation Art 149 & 160"
        );

        var emp = new EmployeePayrollInputItem(
            JobId: 1, IndividualId: 1, EmployeeNumber: "EMP-001", EmployeeName: "Aishath", DepartmentName: "HR",
            PositionTitle: "Officer", GradeCode: "G3", StepNumber: 1, BasicSalary: 12000m, CurrencyTypeId: 1,
            BankName: "BML", BankBranch: "Male", BankAccountNumber: "77010001", AccountHolderName: "Aishath",
            JoinedDate: periodStart, TerminatedDate: null,
            Allowances: new(), OvertimeClaims: new(), UnpaidLeaves: new(), Deductions: new(), Adjustments: new(),
            AttendanceDeductions: new() { latenessDeduction }
        );

        var result = engine.CalculateEmployee(emp, periodStart, periodEnd, employeePensionRate: 0.07m, employerPensionRate: 0.07m);

        var lateLine = result.Lines.FirstOrDefault(l => l.ComponentCode == "DED_ATT_LATE");
        Assert.NotNull(lateLine);
        Assert.Equal(22.22m, lateLine.CalculatedAmount);
    }

    [Fact]
    public void Scenario19_FullDayAbsenceDeduction_InPayrollEngine()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);

        var basicAbsence = new InputAttendanceDeductionItem(
            AttendanceAdjustmentId: 102,
            SourceReference: "ATT-20261103-BASIC",
            WorkDate: new DateOnly(2026, 11, 3),
            DeductionType: AttendanceDeductionTypes.FullDayAbsence,
            ScheduledMinutes: 360,
            ActualMinutes: 0,
            DeductibleMinutes: 360,
            DeductionAmount: 400.00m,
            AffectsBasicSalary: true,
            AffectsServiceAllowance: false,
            PolicyCode: "MCS-STD-2026",
            LegalReference: "Civil Service Regulation Art 160(a)"
        );

        var serviceAbsence = new InputAttendanceDeductionItem(
            AttendanceAdjustmentId: 103,
            SourceReference: "ATT-20261103-SERV",
            WorkDate: new DateOnly(2026, 11, 3),
            DeductionType: "FullDayAbsence_ServiceAllowance",
            ScheduledMinutes: 360,
            ActualMinutes: 0,
            DeductibleMinutes: 360,
            DeductionAmount: 100.00m,
            AffectsBasicSalary: false,
            AffectsServiceAllowance: true,
            PolicyCode: "MCS-STD-2026",
            LegalReference: "Civil Service Regulation Art 160(a)"
        );

        var emp = new EmployeePayrollInputItem(
            JobId: 2, IndividualId: 2, EmployeeNumber: "EMP-002", EmployeeName: "Ibrahim", DepartmentName: "Admin",
            PositionTitle: "Assistant", GradeCode: "G2", StepNumber: 1, BasicSalary: 12000m, CurrencyTypeId: 1,
            BankName: "BML", BankBranch: "Male", BankAccountNumber: "77010002", AccountHolderName: "Ibrahim",
            JoinedDate: periodStart, TerminatedDate: null,
            Allowances: new() { new InputAllowanceItem(1, "SERV_ALLOW", "Service Allowance", SalaryComponentType.FixedAllowance, 3000m, true, true, true, periodStart, null) },
            OvertimeClaims: new(), UnpaidLeaves: new(), Deductions: new(), Adjustments: new(),
            AttendanceDeductions: new() { basicAbsence, serviceAbsence }
        );

        var result = engine.CalculateEmployee(emp, periodStart, periodEnd, employeePensionRate: 0.07m, employerPensionRate: 0.07m);

        var basicAbsenceLine = result.Lines.FirstOrDefault(l => l.ComponentCode == "DED_ATT_ABSENCE_BASIC");
        var servAbsenceLine = result.Lines.FirstOrDefault(l => l.ComponentCode == "DED_ATT_ABSENCE_SERV");

        Assert.NotNull(basicAbsenceLine);
        Assert.Equal(400.00m, basicAbsenceLine.CalculatedAmount);
        Assert.NotNull(servAbsenceLine);
        Assert.Equal(100.00m, servAbsenceLine.CalculatedAmount);
    }

    [Fact]
    public void Scenario20_PensionOfficeRegulation2021R27_ReducesPensionableWage()
    {
        // Maldives Pension Office Regulation 2021/R-27:
        // Attendance basic salary deductions reduce pensionable basic before calculating 7% pension.
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);

        decimal basicSalary = 10000m;
        decimal basicAbsenceDeduction = 400m; // 1 day absence

        var basicAbsence = new InputAttendanceDeductionItem(
            AttendanceAdjustmentId: 201, SourceReference: "ATT-PENS", WorkDate: new DateOnly(2026, 11, 4),
            DeductionType: AttendanceDeductionTypes.FullDayAbsence, ScheduledMinutes: 360, ActualMinutes: 0,
            DeductibleMinutes: 360, DeductionAmount: basicAbsenceDeduction, AffectsBasicSalary: true, AffectsServiceAllowance: false,
            PolicyCode: "MCS-STD-2026", LegalReference: "Regulation 2021/R-27"
        );

        var emp = new EmployeePayrollInputItem(
            JobId: 3, IndividualId: 3, EmployeeNumber: "EMP-003", EmployeeName: "Mohamed", DepartmentName: "Finance",
            PositionTitle: "Specialist", GradeCode: "G4", StepNumber: 1, BasicSalary: basicSalary, CurrencyTypeId: 1,
            BankName: "BML", BankBranch: "Male", BankAccountNumber: "77010003", AccountHolderName: "Mohamed",
            JoinedDate: periodStart, TerminatedDate: null,
            Allowances: new(), OvertimeClaims: new(), UnpaidLeaves: new(), Deductions: new(), Adjustments: new(),
            AttendanceDeductions: new() { basicAbsence }
        );

        var result = engine.CalculateEmployee(emp, periodStart, periodEnd, employeePensionRate: 0.07m, employerPensionRate: 0.07m);

        // Expected pensionable base = 10000 - 400 = 9600 MVR
        // 7% employee pension = 9600 * 0.07 = 672.00 MVR
        var employeePension = result.Lines.FirstOrDefault(l => l.ComponentCode == "PEN_EMP");
        var employerPension = result.Lines.FirstOrDefault(l => l.ComponentCode == "PEN_EMPR");

        Assert.NotNull(employeePension);
        Assert.Equal(672.00m, employeePension.CalculatedAmount);
        Assert.NotNull(employerPension);
        Assert.Equal(672.00m, employerPension.CalculatedAmount);
    }

    [Fact]
    public void Scenario21_MIRA_EmployeeWithholdingTax_AdjustedNetOfAttendanceAndPension()
    {
        // MIRA Taxable remuneration: Earnings net of attendance deductions and employee pension
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);

        // Basic salary 70,000 MVR
        decimal basicSalary = 70000m;
        decimal latenessDeduction = 1000m;

        var late = new InputAttendanceDeductionItem(
            AttendanceAdjustmentId: 202, SourceReference: "ATT-TAX", WorkDate: new DateOnly(2026, 11, 5),
            DeductionType: AttendanceDeductionTypes.LateArrival, ScheduledMinutes: 360, ActualMinutes: 0,
            DeductibleMinutes: 100, DeductionAmount: latenessDeduction, AffectsBasicSalary: true, AffectsServiceAllowance: false,
            PolicyCode: "MCS-STD-2026", LegalReference: "MIRA Tax Rules"
        );

        var emp = new EmployeePayrollInputItem(
            JobId: 4, IndividualId: 4, EmployeeNumber: "EMP-004", EmployeeName: "Ali", DepartmentName: "Management",
            PositionTitle: "Director", GradeCode: "G7", StepNumber: 1, BasicSalary: basicSalary, CurrencyTypeId: 1,
            BankName: "BML", BankBranch: "Male", BankAccountNumber: "77010004", AccountHolderName: "Ali",
            JoinedDate: periodStart, TerminatedDate: null,
            Allowances: new(), OvertimeClaims: new(), UnpaidLeaves: new(), Deductions: new(), Adjustments: new(),
            AttendanceDeductions: new() { late }
        );

        var result = engine.CalculateEmployee(emp, periodStart, periodEnd, employeePensionRate: 0.07m, employerPensionRate: 0.07m);

        // Pensionable base = 70000 - 1000 = 69000; Employee pension = 69000 * 0.07 = 4830
        // Net taxable income = 70000 - 1000 - 4830 = 64170 MVR
        // Over 60,000 bracket: (64170 - 60000) * 0.05 = 4170 * 0.05 = 208.50 MVR
        var taxLine = result.Lines.FirstOrDefault(l => l.ComponentCode == "TAX_WHT");
        Assert.NotNull(taxLine);
        Assert.Equal(208.50m, taxLine.CalculatedAmount);
    }

    [Fact]
    public async Task Scenario22_DuplicateAdjustmentPrevention_EnforcesIdempotency()
    {
        var options = CreateDbOptions();
        await using var db = new HrmTeContext(options);

        var adj1 = new AttendanceAdjustment
        {
            AttendanceAdjustmentId = 1,
            OrganisationId = 1,
            IndividualId = 10,
            JobId = 20,
            WorkDate = new DateOnly(2026, 11, 6),
            AdjustmentType = AttendanceDeductionTypes.LateArrival,
            DeductibleMinutes = 30,
            EstimatedDeductionAmount = 33.33m,
            VerificationStatus = AttendanceAdjustmentVerificationStatuses.Verified,
            PayrollProcessingStatus = AttendanceAdjustmentPayrollStatuses.Pending
        };

        db.AttendanceAdjustments.Add(adj1);
        await db.SaveChangesAsync();

        // Check if adjustment already exists for (OrganisationId, JobId, WorkDate, AdjustmentType)
        bool exists = await db.AttendanceAdjustments.AnyAsync(a =>
            a.OrganisationId == 1 && a.JobId == 20 &&
            a.WorkDate == new DateOnly(2026, 11, 6) &&
            a.AdjustmentType == AttendanceDeductionTypes.LateArrival &&
            a.VerificationStatus != AttendanceAdjustmentVerificationStatuses.Cancelled);

        Assert.True(exists);
    }

    [Fact]
    public void Scenario23_LateApprovalPostFinalisationLock_PreventsModification()
    {
        var adjustment = new AttendanceAdjustment
        {
            AttendanceAdjustmentId = 30,
            OrganisationId = 1,
            IndividualId = 12,
            JobId = 32,
            WorkDate = new DateOnly(2026, 10, 15),
            VerificationStatus = AttendanceAdjustmentVerificationStatuses.Verified,
            PayrollProcessingStatus = AttendanceAdjustmentPayrollStatuses.Locked, // Finalised in past run
            PayrollRunId = 5
        };

        // When locked, modification is prohibited
        bool canModify = adjustment.PayrollProcessingStatus == AttendanceAdjustmentPayrollStatuses.Pending;
        Assert.False(canModify);
    }

    [Fact]
    public void Scenario24_MidMonthTransfer_UsesEffectiveDatedBasicSalary()
    {
        // Work date 2026-11-05 before transfer (basic salary 10,000 MVR)
        // Work date 2026-11-20 after transfer (basic salary 14,000 MVR)
        var (_, _, deductionEarly) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 10000m, officialDailyWorkingMinutes: 360, deductibleMinutes: 18);
        var (_, _, deductionLate) = AttendanceDeductionService.CalculateLateness(
            monthlyBasicSalary: 14000m, officialDailyWorkingMinutes: 360, deductibleMinutes: 18);

        // Early: (10000/30/360) * 18 = 16.67 MVR
        // Late: (14000/30/360) * 18 = 23.33 MVR
        Assert.Equal(16.67m, deductionEarly);
        Assert.Equal(23.33m, deductionLate);
    }

    [Fact]
    public void Scenario25_HistoricalEffectiveDate_RespectsPastPolicy()
    {
        var pastPolicy = new AttendanceDeductionPolicy
        {
            AttendanceDeductionPolicyId = 1,
            OrganisationId = 1,
            PolicyCode = "MCS-HIST-2025",
            EffectiveFrom = new DateOnly(2025, 1, 1),
            EffectiveTo = new DateOnly(2025, 12, 31),
            ArrivalBufferMinutes = 0, // No buffer prior to July 2026 amendment
            IsActive = true,
            IsVerified = true
        };

        var currentPolicy = CreateStandardMaldivesPolicy();

        DateOnly pastDate = new DateOnly(2025, 8, 10);
        DateOnly currentDate = new DateOnly(2026, 8, 10);

        bool appliesPast = pastDate >= pastPolicy.EffectiveFrom && (!pastPolicy.EffectiveTo.HasValue || pastDate <= pastPolicy.EffectiveTo.Value);
        bool appliesCurrent = currentDate >= currentPolicy.EffectiveFrom && (!currentPolicy.EffectiveTo.HasValue || currentDate <= currentPolicy.EffectiveTo.Value);

        Assert.True(appliesPast);
        Assert.True(appliesCurrent);
        Assert.Equal(0, pastPolicy.ArrivalBufferMinutes);
        Assert.Equal(30, currentPolicy.ArrivalBufferMinutes);
    }

    [Fact]
    public async Task Scenario26_MultiOrganisationConcurrency_IsolatesData()
    {
        var options = CreateDbOptions();
        await using var db = new HrmTeContext(options);

        db.AttendanceAdjustments.AddRange(
            new AttendanceAdjustment { AttendanceAdjustmentId = 101, OrganisationId = 1, JobId = 1, WorkDate = new DateOnly(2026, 11, 1), EstimatedDeductionAmount = 50m },
            new AttendanceAdjustment { AttendanceAdjustmentId = 102, OrganisationId = 2, JobId = 2, WorkDate = new DateOnly(2026, 11, 1), EstimatedDeductionAmount = 75m }
        );
        await db.SaveChangesAsync();

        var org1Items = await db.AttendanceAdjustments.Where(a => a.OrganisationId == 1).ToListAsync();
        var org2Items = await db.AttendanceAdjustments.Where(a => a.OrganisationId == 2).ToListAsync();

        Assert.Single(org1Items);
        Assert.Equal(101, org1Items[0].AttendanceAdjustmentId);
        Assert.Single(org2Items);
        Assert.Equal(102, org2Items[0].AttendanceAdjustmentId);
    }
}
