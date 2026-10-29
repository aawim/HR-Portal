using HRM.Models.Archives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class StaffFinanceMapping : IEntityTypeConfiguration<AllowanceAssessment>,IEntityTypeConfiguration<AllowanceType>, IEntityTypeConfiguration<PositionAllowanceRule>,
    IEntityTypeConfiguration<StaffAllowance>, IEntityTypeConfiguration<DeductionPolicy>, IEntityTypeConfiguration<DeductionAgreement>,
    IEntityTypeConfiguration<DeductionInstalment>, IEntityTypeConfiguration<DeductionPayment>, IEntityTypeConfiguration<DeductionPaymentAllocation>,
    IEntityTypeConfiguration<StaffFinanceRequest>, IEntityTypeConfiguration<StaffFinanceDocument>, IEntityTypeConfiguration<StaffFinancePayrollBatch>,
    IEntityTypeConfiguration<StaffFinancePayrollEntry>, IEntityTypeConfiguration<FinanceCurrency>
{
    private static void Table<T>(EntityTypeBuilder<T> b, string name) where T : class
    {
        b.ToTable(name,t=>t.UseSqlOutputClause(false)); b.HasKey("Id");
        foreach (var p in b.Metadata.GetProperties())
        {
            if (p.ClrType == typeof(decimal)) b.Property(p.Name).HasPrecision(18, 2);
            if (p.ClrType == typeof(string)) b.Property(p.Name).HasMaxLength(500);
        }
    }
    public void Configure(EntityTypeBuilder<FinanceCurrency> b)
    { b.ToTable("CurrencyTypes", t => t.ExcludeFromMigrations()); b.HasKey(x=>x.CurrencyTypeId); b.Property(x=>x.CurrencyTypeId).HasColumnName("CurrencyTypeID"); b.Property(x=>x.Name).HasMaxLength(200); }
    public void Configure(EntityTypeBuilder<AllowanceType> b)
    { Table(b,"AL_AllowanceTypes"); b.Property(x=>x.Name).HasMaxLength(150); b.HasIndex(x=>new{x.OrganisationId,x.Name}).IsUnique(); b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<AllowanceAssessment> b)
    {Table(b,"AL_AllowanceAssessments");b.HasIndex(x=>new{x.RequestId,x.Id});b.HasOne<StaffFinanceRequest>().WithMany().HasForeignKey(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);b.HasOne<FinanceCurrency>().WithMany().HasForeignKey(x=>x.CurrencyTypeId).OnDelete(DeleteBehavior.Restrict);b.HasOne<User>().WithMany().HasForeignKey(x=>x.AssessedByUserId).OnDelete(DeleteBehavior.Restrict);}
    public void Configure(EntityTypeBuilder<PositionAllowanceRule> b)
    {
        Table(b,"AL_PositionAllowanceRules"); b.HasIndex(x=>new{x.OrganisationId,x.PositionId,x.AllowanceTypeId,x.FromDate}).IsUnique();
        b.HasOne<Position>().WithMany().HasForeignKey(x=>x.PositionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AllowanceType>().WithMany().HasForeignKey(x=>x.AllowanceTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FinanceCurrency>().WithMany().HasForeignKey(x=>x.CurrencyTypeId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<StaffAllowance> b)
    {
        Table(b,"AL_StaffAllowances"); b.HasIndex(x=>x.RequestId).IsUnique();
        b.HasOne<Request>().WithMany().HasForeignKey(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AllowanceType>().WithMany().HasForeignKey(x=>x.AllowanceTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FinanceCurrency>().WithMany().HasForeignKey(x=>x.CurrencyTypeId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<DeductionPolicy> b)
    { Table(b,"DA_DeductionPolicies"); b.HasIndex(x=>new{x.OrganisationId,x.CurrencyTypeId,x.FromDate}).IsUnique(); b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict); b.HasOne<FinanceCurrency>().WithMany().HasForeignKey(x=>x.CurrencyTypeId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<DeductionAgreement> b)
    {
        Table(b,"DA_DeductionAgreements"); b.HasIndex(x=>x.RequestId).IsUnique(); b.HasIndex(x=>new{x.OrganisationId,x.CreditorBusinessEntityId,x.Reference}).IsUnique();
        b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Request>().WithMany().HasForeignKey(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BusinessEntity>().WithMany().HasForeignKey(x=>x.CreditorBusinessEntityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x=>x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FinanceCurrency>().WithMany().HasForeignKey(x=>x.CurrencyTypeId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<DeductionInstalment> b)
    { Table(b,"DA_Instalments"); b.HasIndex(x=>new{x.AgreementId,x.Version,x.DueDate}).IsUnique(); b.HasOne<DeductionAgreement>().WithMany().HasForeignKey(x=>x.AgreementId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Request>().WithMany().HasForeignKey(x=>x.SourceRequestId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<DeductionPayment> b)
    {
        Table(b,"DA_Payments"); b.HasIndex(x=>x.RequestId).IsUnique().HasFilter("[RequestId] IS NOT NULL"); b.HasIndex(x=>x.PayrollEntryId).IsUnique().HasFilter("[PayrollEntryId] IS NOT NULL");
        b.HasOne<DeductionAgreement>().WithMany().HasForeignKey(x=>x.AgreementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Request>().WithMany().HasForeignKey(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StaffFinancePayrollEntry>().WithMany().HasForeignKey(x=>x.PayrollEntryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BusinessEntity>().WithMany().HasForeignKey(x=>x.PayerBusinessEntityId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<DeductionPaymentAllocation> b)
    { Table(b,"DA_PaymentAllocations"); b.HasIndex(x=>new{x.PaymentId,x.InstalmentId}).IsUnique(); b.HasOne<DeductionPayment>().WithMany().HasForeignKey(x=>x.PaymentId).OnDelete(DeleteBehavior.Restrict); b.HasOne<DeductionInstalment>().WithMany().HasForeignKey(x=>x.InstalmentId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<StaffFinanceRequest> b)
    { b.ToTable("AD_FinanceRequests",t=>t.UseSqlOutputClause(false)); b.HasKey(x=>x.RequestId); b.Property(x=>x.RequestId).ValueGeneratedNever(); b.Property(x=>x.Kind).HasMaxLength(30); b.Property(x=>x.PayloadJson).HasColumnType("nvarchar(max)"); b.HasIndex(x=>new{x.OrganisationId,x.ClientRequestId}).IsUnique(); b.HasOne(x=>x.Request).WithOne().HasForeignKey<StaffFinanceRequest>(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<StaffFinanceDocument> b)
    { Table(b,"AD_RequestDocuments"); b.Property(x=>x.ContentType).HasMaxLength(100); b.Property(x=>x.Content).HasColumnType("varbinary(max)"); b.HasOne<StaffFinanceRequest>().WithMany().HasForeignKey(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<StaffFinancePayrollBatch> b)
    { Table(b,"AD_PayrollBatches"); b.Property(x=>x.SelectionJson).HasColumnType("nvarchar(max)"); b.Property(x=>x.Fingerprint).HasMaxLength(64); b.HasIndex(x=>new{x.OrganisationId,x.ClientBatchId}).IsUnique(); b.HasOne<PayrollCycle>().WithMany().HasForeignKey(x=>x.PayrollCycleRequestId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<StaffFinancePayrollEntry> b)
    {
        Table(b,"AD_PayrollEntries"); b.Property(x=>x.SourceKey).HasMaxLength(100); b.HasIndex(x=>new{x.OrganisationId,x.Month,x.SourceKey}).IsUnique();
        b.HasOne<StaffFinancePayrollBatch>().WithMany().HasForeignKey(x=>x.BatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DeductionInstalment>().WithMany().HasForeignKey(x=>x.InstalmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StaffAllowance>().WithMany().HasForeignKey(x=>x.AllowanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PositionAllowanceRule>().WithMany().HasForeignKey(x=>x.PositionRuleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JobPosition>().WithMany().HasForeignKey(x=>x.JobPositionId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<FinanceCurrency> FinanceCurrencies => Set<FinanceCurrency>();
    public DbSet<AllowanceAssessment> AllowanceAssessments=>Set<AllowanceAssessment>();
    public DbSet<AllowanceType> AllowanceTypes => Set<AllowanceType>();
    public DbSet<PositionAllowanceRule> PositionAllowanceRules => Set<PositionAllowanceRule>();
    public DbSet<StaffAllowance> StaffAllowances => Set<StaffAllowance>();
    public DbSet<DeductionPolicy> DeductionPolicies => Set<DeductionPolicy>();
    public DbSet<DeductionAgreement> DeductionAgreements => Set<DeductionAgreement>();
    public DbSet<DeductionInstalment> DeductionInstalments => Set<DeductionInstalment>();
    public DbSet<DeductionPayment> DeductionPayments => Set<DeductionPayment>();
    public DbSet<DeductionPaymentAllocation> DeductionPaymentAllocations => Set<DeductionPaymentAllocation>();
    public DbSet<StaffFinanceRequest> StaffFinanceRequests => Set<StaffFinanceRequest>();
    public DbSet<StaffFinanceDocument> StaffFinanceDocuments => Set<StaffFinanceDocument>();
    public DbSet<StaffFinancePayrollBatch> StaffFinancePayrollBatches => Set<StaffFinancePayrollBatch>();
    public DbSet<StaffFinancePayrollEntry> StaffFinancePayrollEntries => Set<StaffFinancePayrollEntry>();
}
