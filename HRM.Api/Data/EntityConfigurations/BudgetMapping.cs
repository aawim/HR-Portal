using HRM.Models.Budget;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class BudgetMapping :
    IEntityTypeConfiguration<FiscalYear>,
    IEntityTypeConfiguration<BudgetCategory>,
    IEntityTypeConfiguration<BudgetItem>,
    IEntityTypeConfiguration<BudgetTransactionType>,
    IEntityTypeConfiguration<BudgetAllocation>,
    IEntityTypeConfiguration<BudgetVirement>,
    IEntityTypeConfiguration<BudgetCommitment>,
    IEntityTypeConfiguration<BudgetTransaction>,
    IEntityTypeConfiguration<PayrollBudgetMapping>,
    IEntityTypeConfiguration<BudgetAuditEvent>
{
    public void Configure(EntityTypeBuilder<FiscalYear> b)
    {
        b.ToTable("BG_FiscalYears");
        b.HasKey(x => x.FiscalYearId);
        b.Property(x => x.TotalApprovedBudget).HasPrecision(18, 2);
        b.Property(x => x.TotalEmolumentsBudget).HasPrecision(18, 2);
        b.HasIndex(x => x.YearNumber).IsUnique();
    }

    public void Configure(EntityTypeBuilder<BudgetCategory> b)
    {
        b.ToTable("BG_BudgetCategories");
        b.HasKey(x => x.CategoryId);
        b.Property(x => x.CategoryCode).HasMaxLength(20);
        b.Property(x => x.CategoryNameEn).HasMaxLength(200);
        b.Property(x => x.CategoryNameDv).HasMaxLength(200);

        b.HasOne(x => x.ParentCategory)
            .WithMany(x => x.ChildCategories)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<BudgetItem> b)
    {
        b.ToTable("BG_BudgetItems");
        b.HasKey(x => x.BudgetItemID);
        b.Property(x => x.BudgetItemID).HasColumnName("BudgetItemID");
        b.Property(x => x.EconomicCode).HasMaxLength(20);
        b.Property(x => x.MoFClassificationCode).HasMaxLength(50);
        b.Property(x => x.ChapterCode).HasMaxLength(10);
        b.Property(x => x.AccountType).HasMaxLength(20);
        b.Property(x => x.DhivehiName).HasMaxLength(500);
        b.Property(x => x.EnglishName).HasMaxLength(500);
    }

    public void Configure(EntityTypeBuilder<BudgetTransactionType> b)
    {
        b.ToTable("BG_BudgetTransactionTypes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TypeName).HasMaxLength(50);
        b.Property(x => x.DhivehiTypeName).HasMaxLength(100);
    }

    public void Configure(EntityTypeBuilder<BudgetAllocation> b)
    {
        b.ToTable("BG_BudgetAllocations");
        b.HasKey(x => x.AllocationId);
        b.Property(x => x.ApprovedAmount).HasPrecision(18, 2);
        b.Property(x => x.RevisedAmount).HasPrecision(18, 2);
        b.Property(x => x.VirementInAmount).HasPrecision(18, 2);
        b.Property(x => x.VirementOutAmount).HasPrecision(18, 2);
        b.Property(x => x.CommittedAmount).HasPrecision(18, 2);
        b.Property(x => x.ExpendedAmount).HasPrecision(18, 2);
        b.Property(x => x.MoFApprovalReference).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(30);

        b.HasOne(x => x.FiscalYear)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.FiscalYearId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.BudgetItem)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.BudgetItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.FiscalYearId, x.OrganisationId, x.BudgetItemId }).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.FiscalYearId });
    }

    public void Configure(EntityTypeBuilder<BudgetVirement> b)
    {
        b.ToTable("BG_BudgetVirements");
        b.HasKey(x => x.VirementId);
        b.Property(x => x.VirementNumber).HasMaxLength(50);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.LegalBasis).HasMaxLength(500);
        b.Property(x => x.MoFReferenceNumber).HasMaxLength(100);
        b.Property(x => x.Status).HasMaxLength(30);

        b.HasOne(x => x.FiscalYear)
            .WithMany(x => x.Virements)
            .HasForeignKey(x => x.FiscalYearId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.FromBudgetItem)
            .WithMany()
            .HasForeignKey(x => x.FromBudgetItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ToBudgetItem)
            .WithMany()
            .HasForeignKey(x => x.ToBudgetItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.VirementNumber).IsUnique();
    }

    public void Configure(EntityTypeBuilder<BudgetCommitment> b)
    {
        b.ToTable("BG_BudgetCommitments");
        b.HasKey(x => x.CommitmentId);
        b.Property(x => x.CommitmentNumber).HasMaxLength(50);
        b.Property(x => x.CommitmentType).HasMaxLength(50);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Status).HasMaxLength(30);

        b.HasOne(x => x.FiscalYear)
            .WithMany(x => x.Commitments)
            .HasForeignKey(x => x.FiscalYearId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.BudgetItem)
            .WithMany()
            .HasForeignKey(x => x.BudgetItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.CommitmentNumber).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.BudgetItemId, x.Status });
    }

    public void Configure(EntityTypeBuilder<BudgetTransaction> b)
    {
        b.ToTable("BG_BudgetTransactions");
        b.HasKey(x => x.BudgetTransactionID);
        b.Property(x => x.ReferenceNumber).HasMaxLength(100);
        b.Property(x => x.LetterNumber).HasMaxLength(100);
        b.Property(x => x.BandeyriReferenceNumber).HasMaxLength(100);
        b.Property(x => x.SourceDocumentType).HasMaxLength(50);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.Amount).HasPrecision(18, 2);

        b.HasOne(x => x.BudgetItem)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.BudgetItemID)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TransactionType)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.TransactionTypeID)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.BudgetItemID, x.OrganisationID, x.Date });
    }

    public void Configure(EntityTypeBuilder<PayrollBudgetMapping> b)
    {
        b.ToTable("BG_PayrollBudgetMappings");
        b.HasKey(x => x.MappingId);
        b.Property(x => x.ComponentName).HasMaxLength(150);

        b.HasOne(x => x.BudgetItem)
            .WithMany(x => x.PayrollMappings)
            .HasForeignKey(x => x.BudgetItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<BudgetAuditEvent> b)
    {
        b.ToTable("BG_BudgetAuditEvents");
        b.HasKey(x => x.AuditEventId);
        b.Property(x => x.ActionType).HasMaxLength(50);
        b.Property(x => x.EntityName).HasMaxLength(100);
    }
}
