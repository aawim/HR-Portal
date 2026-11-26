using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Payroll;

public sealed class PayslipService : IPayslipService
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly ILogger<PayslipService> _logger;

    public PayslipService(IDbContextFactory<HrmTeContext> factory, ILogger<PayslipService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<List<PayslipDto>> GetEmployeePayslipsAsync(int individualId, int organisationId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var records = await db.PayrollEmployees
            .AsNoTracking()
            .Include(e => e.PayrollRun)
                .ThenInclude(r => r!.PayrollPeriod)
            .Include(e => e.Lines)
            .Where(e => e.IndividualId == individualId &&
                        e.PayrollRun != null &&
                        e.PayrollRun.OrganisationId == organisationId &&
                        (e.PayrollRun.Status == PayrollRunStatus.Finalised ||
                         e.PayrollRun.Status == PayrollRunStatus.PaymentPrepared ||
                         e.PayrollRun.Status == PayrollRunStatus.Paid ||
                         e.PayrollRun.Status == PayrollRunStatus.Closed))
            .OrderByDescending(e => e.PayrollRun!.PayrollPeriod!.PeriodYear)
            .ThenByDescending(e => e.PayrollRun!.PayrollPeriod!.PeriodMonth)
            .ToListAsync();

        var organisationName = await db.Organisations.Where(o => o.BusinessEntityID == organisationId)
            .Select(o => o.OrganisationName).SingleOrDefaultAsync() ?? "";
        var runIds = records.Select(r => r.PayrollRunId).Distinct().ToList();
        var snapshots = await db.PayrollInputSnapshots.AsNoTracking()
            .Where(s => runIds.Contains(s.PayrollRunId) && s.IndividualId == individualId).ToListAsync();
        var result = new List<PayslipDto>();
        foreach (var r in records)
        {
            var frozen = snapshots.FirstOrDefault(s => s.PayrollRunId == r.PayrollRunId);
            var input = frozen == null ? null : JsonSerializer.Deserialize<EmployeePayrollInputItem>(frozen.SnapshotJson);
            var period = r.PayrollRun!.PayrollPeriod!;
            var earnings = r.Lines.Where(l => l.ComponentType == SalaryComponentType.BasicSalary ||
                                              l.ComponentType == SalaryComponentType.FixedAllowance ||
                                              l.ComponentType == SalaryComponentType.VariableAllowance ||
                                              l.ComponentType == SalaryComponentType.DutyAllowance ||
                                              l.ComponentType == SalaryComponentType.ShiftAllowance ||
                                              l.ComponentType == SalaryComponentType.Overtime ||
                                              l.ComponentType == SalaryComponentType.Bonus ||
                                              l.ComponentType == SalaryComponentType.Reimbursement)
                .Select(l => new PayrollLineDto(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference))
                .ToList();

            var deductions = r.Lines.Where(l => l.ComponentType == SalaryComponentType.EmployeePension ||
                                                l.ComponentType == SalaryComponentType.EmployeeTax ||
                                                l.ComponentType == SalaryComponentType.UnpaidLeaveDeduction ||
                                                l.ComponentType == SalaryComponentType.SalaryAdvanceDeduction ||
                                                l.ComponentType == SalaryComponentType.LoanDeduction ||
                                                l.ComponentType == SalaryComponentType.OtherDeduction ||
                                                l.ComponentType == SalaryComponentType.AttendanceLatenessDeduction ||
                                                l.ComponentType == SalaryComponentType.AttendanceAbsenceDeduction)
                .Select(l => new PayrollLineDto(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference))
                .ToList();

            var employerItems = r.Lines.Where(l => l.ComponentType == SalaryComponentType.EmployerPension ||
                                                   l.ComponentType == SalaryComponentType.EmployerStatutory)
                .Select(l => new PayrollLineDto(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference))
                .ToList();

            result.Add(new PayslipDto(
                PayrollEmployeeId: r.PayrollEmployeeId,
                PayrollRunId: r.PayrollRunId,
                OrganisationId: organisationId,
                OrganisationName: organisationName,
                IndividualId: r.IndividualId,
                EmployeeNumber: r.EmployeeNumber,
                EmployeeName: r.EmployeeName,
                DepartmentName: r.DepartmentName,
                PositionTitle: r.PositionTitle,
                GradeCode: r.GradeCode,
                StepNumber: r.StepNumber,
                PeriodYear: period.PeriodYear,
                PeriodMonth: period.PeriodMonth,
                PeriodStartDate: period.PeriodStartDate,
                PeriodEndDate: period.PeriodEndDate,
                PaymentDate: period.PaymentDate,
                BaseSalary: r.BaseSalary,
                GrossEarnings: r.GrossEarnings,
                TotalDeductions: r.TotalDeductions,
                EmployerContributions: r.EmployerContributions,
                NetSalary: r.NetSalary,
                BankName: input?.BankName ?? "",
                BankAccountNumber: input?.BankAccountNumber ?? "",
                PaymentReference: $"SAL-{r.PayrollRunId}-{r.IndividualId}",
                Earnings: earnings,
                Deductions: deductions,
                EmployerItems: employerItems
            ));
        }

        return result;
    }

    public async Task<PayslipDto?> GetPayslipDetailAsync(int payrollEmployeeId, int organisationId, int requestingIndividualId, bool isPrivileged)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var r = await db.PayrollEmployees
            .AsNoTracking()
            .Include(e => e.PayrollRun)
                .ThenInclude(run => run!.PayrollPeriod)
            .Include(e => e.Lines)
            .SingleOrDefaultAsync(e => e.PayrollEmployeeId == payrollEmployeeId &&
                                       e.PayrollRun != null &&
                                       e.PayrollRun.OrganisationId == organisationId);

        if (r == null) return null;
        if (!isPrivileged && r.PayrollRun!.Status is not (PayrollRunStatus.Finalised or PayrollRunStatus.PaymentPrepared or
            PayrollRunStatus.PaymentSubmitted or PayrollRunStatus.PartiallyPaid or PayrollRunStatus.Paid or PayrollRunStatus.Closed))
            throw new InvalidOperationException("Payslip is available after payroll finalisation.");

        // Security check: only own payslip or privileged user
        if (r.IndividualId != requestingIndividualId && !isPrivileged)
        {
            _logger.LogWarning("Access denied: User {User} attempted to access payslip {Id} for Employee {Target}",
                requestingIndividualId, payrollEmployeeId, r.IndividualId);
            throw new UnauthorizedAccessException("You are not authorized to view this payslip.");
        }

        var organisationName = await db.Organisations.Where(o => o.BusinessEntityID == organisationId)
            .Select(o => o.OrganisationName).SingleOrDefaultAsync() ?? "";
        var frozen = await db.PayrollInputSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.PayrollRunId == r.PayrollRunId && s.IndividualId == r.IndividualId);
        var input = frozen == null ? null : JsonSerializer.Deserialize<EmployeePayrollInputItem>(frozen.SnapshotJson);
        var period = r.PayrollRun!.PayrollPeriod!;
        var earnings = r.Lines.Where(l => (int)l.ComponentType <= 8)
            .Select(l => new PayrollLineDto(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference))
            .ToList();

        var deductions = r.Lines.Where(l => PayrollReportValues.IsEmployeeDeduction(l.ComponentType))
            .Select(l => new PayrollLineDto(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference))
            .ToList();

        var employerItems = r.Lines.Where(l => l.ComponentType is SalaryComponentType.EmployerPension or SalaryComponentType.EmployerStatutory)
            .Select(l => new PayrollLineDto(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference))
            .ToList();

        return new PayslipDto(
            PayrollEmployeeId: r.PayrollEmployeeId,
            PayrollRunId: r.PayrollRunId,
            OrganisationId: organisationId,
            OrganisationName: organisationName,
            IndividualId: r.IndividualId,
            EmployeeNumber: r.EmployeeNumber,
            EmployeeName: r.EmployeeName,
            DepartmentName: r.DepartmentName,
            PositionTitle: r.PositionTitle,
            GradeCode: r.GradeCode,
            StepNumber: r.StepNumber,
            PeriodYear: period.PeriodYear,
            PeriodMonth: period.PeriodMonth,
            PeriodStartDate: period.PeriodStartDate,
            PeriodEndDate: period.PeriodEndDate,
            PaymentDate: period.PaymentDate,
            BaseSalary: r.BaseSalary,
            GrossEarnings: r.GrossEarnings,
            TotalDeductions: r.TotalDeductions,
            EmployerContributions: r.EmployerContributions,
            NetSalary: r.NetSalary,
            BankName: input?.BankName ?? "",
            BankAccountNumber: input?.BankAccountNumber ?? "",
            PaymentReference: $"SAL-{r.PayrollRunId}-{r.IndividualId}",
            Earnings: earnings,
            Deductions: deductions,
            EmployerItems: employerItems
        );
    }
}
