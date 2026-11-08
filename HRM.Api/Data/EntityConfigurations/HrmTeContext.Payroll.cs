using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<EmployeeCompensation> EmployeeCompensations => Set<EmployeeCompensation>();
    public DbSet<CalculationRule> CalculationRules => Set<CalculationRule>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRunBatch> PayrollRunBatches => Set<PayrollRunBatch>();
    public DbSet<PayrollEmployee> PayrollEmployees => Set<PayrollEmployee>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
    public DbSet<PayrollInputSnapshot> PayrollInputSnapshots => Set<PayrollInputSnapshot>();
    public DbSet<PayrollException> PayrollExceptions => Set<PayrollException>();
    public DbSet<PayrollAdjustment> PayrollAdjustments => Set<PayrollAdjustment>();
    public DbSet<PaymentBatch> PaymentBatches => Set<PaymentBatch>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PayrollAuditEvent> PayrollAuditEvents => Set<PayrollAuditEvent>();
    public DbSet<PayrollBackgroundJob> PayrollBackgroundJobs => Set<PayrollBackgroundJob>();
}
