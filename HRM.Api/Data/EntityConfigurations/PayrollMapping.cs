using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class PayrollMapping :
    IEntityTypeConfiguration<PayrollPeriod>,
    IEntityTypeConfiguration<SalaryComponent>,
    IEntityTypeConfiguration<SalaryStructure>,
    IEntityTypeConfiguration<EmployeeCompensation>,
    IEntityTypeConfiguration<CalculationRule>,
    IEntityTypeConfiguration<PayrollRun>,
    IEntityTypeConfiguration<PayrollRunBatch>,
    IEntityTypeConfiguration<PayrollEmployee>,
    IEntityTypeConfiguration<PayrollLine>,
    IEntityTypeConfiguration<PayrollInputSnapshot>,
    IEntityTypeConfiguration<PayrollException>,
    IEntityTypeConfiguration<PayrollAdjustment>,
    IEntityTypeConfiguration<PaymentBatch>,
    IEntityTypeConfiguration<PaymentTransaction>,
    IEntityTypeConfiguration<PayrollAuditEvent>,
    IEntityTypeConfiguration<PayrollBackgroundJob>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> b)
    {
        b.ToTable("PAY_PayrollPeriods");
        b.HasKey(x => x.PayrollPeriodId);
        b.HasIndex(x => new { x.OrganisationId, x.PeriodYear, x.PeriodMonth, x.PayrollGroupId }).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.PeriodStatus });
    }

    public void Configure(EntityTypeBuilder<SalaryComponent> b)
    {
        b.ToTable("PAY_SalaryComponents");
        b.HasKey(x => x.SalaryComponentId);
        b.Property(x => x.ComponentCode).HasMaxLength(50);
        b.Property(x => x.ComponentName).HasMaxLength(150);
        b.Property(x => x.DefaultAmount).HasPrecision(18, 2);
        b.Property(x => x.DefaultPercentage).HasPrecision(18, 4);
        b.HasIndex(x => new { x.OrganisationId, x.ComponentCode }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<SalaryStructure> b)
    {
        b.ToTable("PAY_SalaryStructures");
        b.HasKey(x => x.SalaryStructureId);
        b.Property(x => x.StructureCode).HasMaxLength(50);
        b.Property(x => x.StructureName).HasMaxLength(150);
        b.Property(x => x.GradeCode).HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.MinimumSalary).HasPrecision(18, 2);
        b.Property(x => x.MaximumSalary).HasPrecision(18, 2);
        b.HasIndex(x => new { x.OrganisationId, x.StructureCode }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<EmployeeCompensation> b)
    {
        b.ToTable("PAY_EmployeeCompensation");
        b.HasKey(x => x.EmployeeCompensationId);
        b.Property(x => x.BasicSalary).HasPrecision(18, 2);
        b.Property(x => x.GradeCode).HasMaxLength(50);
        b.Property(x => x.BankName).HasMaxLength(100);
        b.Property(x => x.BankBranch).HasMaxLength(100);
        b.Property(x => x.BankAccountNumber).HasMaxLength(50);
        b.Property(x => x.AccountHolderName).HasMaxLength(150);
        b.HasIndex(x => new { x.OrganisationId, x.JobId, x.EffectiveFrom, x.EffectiveTo });
        b.HasIndex(x => new { x.OrganisationId, x.IndividualId });
    }

    public void Configure(EntityTypeBuilder<CalculationRule> b)
    {
        b.ToTable("PAY_CalculationRules");
        b.HasKey(x => x.CalculationRuleId);
        b.Property(x => x.RuleCode).HasMaxLength(50);
        b.Property(x => x.RuleName).HasMaxLength(150);
        b.Property(x => x.RuleType).HasMaxLength(50);
        b.Property(x => x.PercentageValue).HasPrecision(18, 4);
        b.Property(x => x.FlatThresholdValue).HasPrecision(18, 2);
        b.HasIndex(x => new { x.OrganisationId, x.RuleCode });
    }

    public void Configure(EntityTypeBuilder<PayrollRun> b)
    {
        b.ToTable("PAY_PayrollRuns");
        b.HasKey(x => x.PayrollRunId);
        b.Property(x => x.Title).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.TotalGrossEarnings).HasPrecision(18, 2);
        b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        b.Property(x => x.TotalEmployerContributions).HasPrecision(18, 2);
        b.Property(x => x.TotalNetPayable).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.PayrollPeriod)
            .WithMany(x => x.PayrollRuns)
            .HasForeignKey(x => x.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.OrganisationId, x.PayrollPeriodId });
        b.HasIndex(x => new { x.OrganisationId, x.Status });
    }

    public void Configure(EntityTypeBuilder<PayrollRunBatch> b)
    {
        b.ToTable("PAY_PayrollRunBatches");
        b.HasKey(x => x.PayrollRunBatchId);
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);
        b.HasOne(x => x.PayrollRun)
            .WithMany(x => x.Batches)
            .HasForeignKey(x => x.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.PayrollRunId, x.BatchNumber }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PayrollEmployee> b)
    {
        b.ToTable("PAY_PayrollEmployees");
        b.HasKey(x => x.PayrollEmployeeId);
        b.Property(x => x.EmployeeNumber).HasMaxLength(50);
        b.Property(x => x.EmployeeName).HasMaxLength(150);
        b.Property(x => x.DepartmentName).HasMaxLength(150);
        b.Property(x => x.PositionTitle).HasMaxLength(150);
        b.Property(x => x.GradeCode).HasMaxLength(50);
        b.Property(x => x.BaseSalary).HasPrecision(18, 2);
        b.Property(x => x.GrossEarnings).HasPrecision(18, 2);
        b.Property(x => x.TotalDeductions).HasPrecision(18, 2);
        b.Property(x => x.EmployerContributions).HasPrecision(18, 2);
        b.Property(x => x.NetSalary).HasPrecision(18, 2);
        b.Property(x => x.CalculationStatus).HasMaxLength(50);
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);

        b.HasOne(x => x.PayrollRun)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.PayrollRunId, x.IndividualId }).IsUnique();
        b.HasIndex(x => new { x.PayrollRunId, x.PayrollRunBatchId });
    }

    public void Configure(EntityTypeBuilder<PayrollLine> b)
    {
        b.ToTable("PAY_PayrollLines");
        b.HasKey(x => x.PayrollLineId);
        b.Property(x => x.ComponentCode).HasMaxLength(50);
        b.Property(x => x.ComponentName).HasMaxLength(150);
        b.Property(x => x.Rate).HasPrecision(18, 4);
        b.Property(x => x.Units).HasPrecision(18, 4);
        b.Property(x => x.CalculatedAmount).HasPrecision(18, 2);
        b.Property(x => x.SourceReference).HasMaxLength(200);

        b.HasOne(x => x.PayrollEmployee)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.PayrollEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.PayrollEmployeeId, x.ComponentCode });
        b.HasIndex(x => x.PayrollRunId);
    }

    public void Configure(EntityTypeBuilder<PayrollInputSnapshot> b)
    {
        b.ToTable("PAY_PayrollInputSnapshots");
        b.HasKey(x => x.PayrollInputSnapshotId);
        b.Property(x => x.SnapshotHash).HasMaxLength(100);
        b.HasIndex(x => new { x.PayrollRunId, x.JobId }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PayrollException> b)
    {
        b.ToTable("PAY_PayrollExceptions");
        b.HasKey(x => x.PayrollExceptionId);
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Message).HasMaxLength(500);
        b.Property(x => x.Details).HasMaxLength(2000);

        b.HasOne(x => x.PayrollRun)
            .WithMany(x => x.Exceptions)
            .HasForeignKey(x => x.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.PayrollRunId, x.Severity });
    }

    public void Configure(EntityTypeBuilder<PayrollAdjustment> b)
    {
        b.ToTable("PAY_PayrollAdjustments");
        b.HasKey(x => x.PayrollAdjustmentId);
        b.Property(x => x.ComponentCode).HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(250);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.OrganisationId, x.JobId, x.PayrollRunId });
    }

    public void Configure(EntityTypeBuilder<PaymentBatch> b)
    {
        b.ToTable("PAY_PaymentBatches");
        b.HasKey(x => x.PaymentBatchId);
        b.Property(x => x.BatchReference).HasMaxLength(100);
        b.Property(x => x.BankCode).HasMaxLength(50);
        b.Property(x => x.FormatCode).HasMaxLength(50);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.OrganisationId, x.BatchReference }).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.PayrollRunId });
    }

    public void Configure(EntityTypeBuilder<PaymentTransaction> b)
    {
        b.ToTable("PAY_PaymentTransactions");
        b.HasKey(x => x.PaymentTransactionId);
        b.Property(x => x.EmployeeName).HasMaxLength(150);
        b.Property(x => x.BankAccountNumber).HasMaxLength(50);
        b.Property(x => x.BankName).HasMaxLength(100);
        b.Property(x => x.BankBranch).HasMaxLength(100);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.PaymentReference).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.TransactionReference).HasMaxLength(100);
        b.Property(x => x.FailureReason).HasMaxLength(500);

        b.HasOne(x => x.PaymentBatch)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.PaymentBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.PaymentBatchId, x.PayrollEmployeeId });
        b.HasIndex(x => x.PaymentReference).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PayrollAuditEvent> b)
    {
        b.ToTable("PAY_PayrollAuditEvents");
        b.HasKey(x => x.PayrollAuditEventId);
        b.Property(x => x.ActionType).HasMaxLength(100);
        b.Property(x => x.OldState).HasMaxLength(100);
        b.Property(x => x.NewState).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.IpAddress).HasMaxLength(50);
        b.HasIndex(x => new { x.OrganisationId, x.PayrollRunId, x.TimestampUtc });
    }

    public void Configure(EntityTypeBuilder<PayrollBackgroundJob> b)
    {
        b.ToTable("PAY_BackgroundJobs");
        b.HasKey(x => x.JobId);
        b.Property(x => x.LeaseOwner).HasMaxLength(150);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.HasIndex(x => new { x.Status, x.Priority, x.CreatedAtUtc });
        b.HasIndex(x => new { x.OrganisationId, x.PayrollRunId });
        b.HasIndex(x => new { x.Status, x.LeaseExpiresAtUtc });
    }
}
