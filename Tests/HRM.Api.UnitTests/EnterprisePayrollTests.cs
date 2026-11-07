using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using HRM.Services.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class EnterprisePayrollTests
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

    [Fact]
    public void CalculationEngine_ProratesBasicSalary_WhenEmployeeJoinsMidMonth()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30); // 30 days in November
        var joinedDate = new DateOnly(2026, 11, 16); // 15 worked days (16th to 30th)

        var input = new EmployeePayrollInputItem(
            JobId: 101,
            IndividualId: 501,
            EmployeeNumber: "EMP-501",
            EmployeeName: "Ahmed Ali",
            DepartmentName: "Finance",
            PositionTitle: "Accountant",
            GradeCode: "G4",
            StepNumber: 2,
            BasicSalary: 30000m,
            CurrencyTypeId: 1,
            BankName: "BML",
            BankBranch: "Male",
            BankAccountNumber: "7701501000",
            AccountHolderName: "Ahmed Ali",
            JoinedDate: joinedDate,
            TerminatedDate: null,
            Allowances: new List<InputAllowanceItem>(),
            OvertimeClaims: new List<InputOvertimeItem>(),
            UnpaidLeaves: new List<InputLeaveDeductionItem>(),
            Deductions: new List<InputDeductionAgreementItem>(),
            Adjustments: new List<InputAdjustmentItem>()
        );

        var outcome = engine.CalculateEmployee(input, periodStart, periodEnd, employeePensionRate: 0.07m, employerPensionRate: 0.07m);

        // 30,000 / 30 days * 15 days = 15,000
        Assert.Equal(15000.00m, outcome.BaseSalary);
        Assert.Equal(15000.00m, outcome.GrossEarnings);

        // Employee Pension: 7% of 15,000 = 1,050.00
        Assert.Equal(1050.00m, outcome.TotalDeductions);

        // Employer Pension: 7% of 15,000 = 1,050.00
        Assert.Equal(1050.00m, outcome.EmployerContributions);

        // Net Salary: 15,000 - 1,050 = 13,950.00
        Assert.Equal(13950.00m, outcome.NetSalary);
        Assert.Empty(outcome.Errors);
    }

    [Fact]
    public void CalculationEngine_AppliesAllowances_Overtime_And_Deductions_Deterministically()
    {
        var engine = new PayrollCalculationEngine();
        var periodStart = new DateOnly(2026, 11, 1);
        var periodEnd = new DateOnly(2026, 11, 30);

        var allowances = new List<InputAllowanceItem>
        {
            new(1, "ALLW_DUTY", "Duty Allowance", SalaryComponentType.DutyAllowance, 5000m, IsTaxable: true, IsPensionable: true, IsProratable: false, periodStart, null),
            new(2, "ALLW_PHONE", "Phone Allowance", SalaryComponentType.FixedAllowance, 1000m, IsTaxable: true, IsPensionable: false, IsProratable: false, periodStart, null)
        };

        var overtime = new List<InputOvertimeItem>
        {
            new(101, "OT-101", new DateOnly(2026, 11, 5), 120, 200m, 400.00m) // 2 hours
        };

        var unpaidLeave = new List<InputLeaveDeductionItem>
        {
            new(201, "Unpaid Leave", new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 11), 2, 1.0m) // 2 days
        };

        var loanDeduction = new List<InputDeductionAgreementItem>
        {
            new(301, 401, "LOAN-401", "Staff Loan", 1500.00m)
        };

        var input = new EmployeePayrollInputItem(
            JobId: 102,
            IndividualId: 502,
            EmployeeNumber: "EMP-502",
            EmployeeName: "Mariyam Hassan",
            DepartmentName: "IT",
            PositionTitle: "Systems Engineer",
            GradeCode: "G5",
            StepNumber: 1,
            BasicSalary: 25000m,
            CurrencyTypeId: 1,
            BankName: "BML",
            BankBranch: "Male",
            BankAccountNumber: "7701502000",
            AccountHolderName: "Mariyam Hassan",
            JoinedDate: new DateOnly(2024, 1, 1),
            TerminatedDate: null,
            Allowances: allowances,
            OvertimeClaims: overtime,
            UnpaidLeaves: unpaidLeave,
            Deductions: loanDeduction,
            Adjustments: new List<InputAdjustmentItem>()
        );

        var outcome = engine.CalculateEmployee(input, periodStart, periodEnd, employeePensionRate: 0.07m, employerPensionRate: 0.07m);

        // Basic: 25,000, Allowances: 6,000, OT: 400 => Gross: 31,400.00
        Assert.Equal(25000.00m, outcome.BaseSalary);
        Assert.Equal(31400.00m, outcome.GrossEarnings);

        // Pensionable base: Basic (25,000) + Duty Allowance (5,000) = 30,000
        // Employee pension (7% of 30,000) = 2,100.00
        // Unpaid leave deduction (25,000 / 30 * 2) = 1,666.67
        // Loan deduction: 1,500.00
        // Total deductions = 2,100 + 1,666.67 + 1,500 = 5,266.67
        Assert.Equal(5266.67m, outcome.TotalDeductions);

        // Net salary: 31,400.00 - 5,266.67 = 26,133.33
        Assert.Equal(26133.33m, outcome.NetSalary);
        Assert.Empty(outcome.Errors);
    }

    [Fact]
    public async Task PayrollQueueService_AtomicClaiming_IncrementsFencingToken_AndPreventsDuplicateClaim()
    {
        var options = CreateDbOptions();
        var factory = new TestDbContextFactory(options);
        var logger = NullLogger<PayrollQueueService>.Instance;
        var queue = new PayrollQueueService(factory, logger);

        long jobId = await queue.EnqueueJobAsync(organisationId: 1, payrollRunId: 10, PayrollJobType.CalculateBatch, priority: 5);

        // First worker claims the job
        var claimed1 = await queue.ClaimNextJobAsync("Worker-1", TimeSpan.FromMinutes(2));
        Assert.NotNull(claimed1);
        Assert.Equal(jobId, claimed1.JobId);
        Assert.Equal("Worker-1", claimed1.LeaseOwner);
        Assert.Equal(2, claimed1.FencingToken); // Started at 1, incremented to 2

        // Second worker tries to claim next job immediately
        var claimed2 = await queue.ClaimNextJobAsync("Worker-2", TimeSpan.FromMinutes(2));
        Assert.Null(claimed2); // No available job to claim!
    }

    [Fact]
    public async Task PayrollQueueService_ExpiredLease_IsRecoveredSafely()
    {
        var options = CreateDbOptions();
        var factory = new TestDbContextFactory(options);
        var logger = NullLogger<PayrollQueueService>.Instance;
        var queue = new PayrollQueueService(factory, logger);

        long jobId = await queue.EnqueueJobAsync(organisationId: 1, payrollRunId: 10, PayrollJobType.CalculateBatch);

        // Simulate an expired lease from a crashed worker
        await using (var db = factory.CreateDbContext())
        {
            var job = await db.PayrollBackgroundJobs.SingleAsync(j => j.JobId == jobId);
            job.Status = PayrollJobStatus.Processing;
            job.LeaseOwner = "Crashed-Worker";
            job.LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10); // Expired 10 mins ago
            job.AttemptCount = 1;
            await db.SaveChangesAsync();
        }

        int recovered = await queue.RecoverExpiredJobsAsync();
        Assert.Equal(1, recovered);

        await using (var db = factory.CreateDbContext())
        {
            var refreshedJob = await db.PayrollBackgroundJobs.SingleAsync(j => j.JobId == jobId);
            Assert.Equal(PayrollJobStatus.Pending, refreshedJob.Status);
            Assert.Null(refreshedJob.LeaseOwner);
        }
    }

    [Fact]
    public async Task PayrollApprovalService_EnforcesSegregationOfDuties_AndImmutableLock()
    {
        var options = CreateDbOptions();
        var factory = new TestDbContextFactory(options);
        var logger = NullLogger<PayrollApprovalService>.Instance;
        var service = new PayrollApprovalService(factory, logger);

        int orgId = 1;
        int preparerUserId = 10;
        int approverUserId = 20;

        await using (var db = factory.CreateDbContext())
        {
            var period = new PayrollPeriod
            {
                PayrollPeriodId = 1,
                OrganisationId = orgId,
                PeriodYear = 2026,
                PeriodMonth = 11,
                PeriodStartDate = new DateOnly(2026, 11, 1),
                PeriodEndDate = new DateOnly(2026, 11, 30),
                PeriodStatus = PayrollPeriodStatus.Open
            };
            db.PayrollPeriods.Add(period);

            var run = new PayrollRun
            {
                PayrollRunId = 1,
                OrganisationId = orgId,
                PayrollPeriodId = 1,
                Status = PayrollRunStatus.Calculated,
                Title = "November 2026 Payroll",
                PreparedByUserId = preparerUserId,
                PreparedAtUtc = DateTime.UtcNow
            };
            db.PayrollRuns.Add(run);
            await db.SaveChangesAsync();
        }

        // 1. Submit for review
        await service.SubmitForReviewAsync(orgId, 1, preparerUserId, "Submitted");
        await using (var db = factory.CreateDbContext())
        {
            var updated = await db.PayrollRuns.SingleAsync(r => r.PayrollRunId == 1);
            Assert.Equal(PayrollRunStatus.UnderReview, updated.Status);
        }

        // 2. Preparer cannot approve their own run (Segregation of Duties)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApprovePayrollAsync(orgId, 1, preparerUserId, "Self approve"));
        Assert.Contains("Segregation of duties", ex.Message);

        // 3. Different approver approves
        await service.ApprovePayrollAsync(orgId, 1, approverUserId, "Approved");
        await using (var db = factory.CreateDbContext())
        {
            var updated = await db.PayrollRuns.SingleAsync(r => r.PayrollRunId == 1);
            Assert.Equal(PayrollRunStatus.Approved, updated.Status);
        }

        // 4. Finalise run (Immutable)
        await service.FinalisePayrollAsync(orgId, 1, approverUserId, "Finalised");
        await using (var db = factory.CreateDbContext())
        {
            var updated = await db.PayrollRuns.SingleAsync(r => r.PayrollRunId == 1);
            Assert.Equal(PayrollRunStatus.Finalised, updated.Status);
        }

        // 5. Finalised run cannot be rejected or modified
        var rejectEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RejectPayrollAsync(orgId, 1, approverUserId, "Reject finalised"));
        Assert.Contains("Cannot reject finalised", rejectEx.Message);
    }
}
