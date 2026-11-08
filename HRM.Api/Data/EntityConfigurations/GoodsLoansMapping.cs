using HRM.Models;
using HRM.Models.GoodsLoans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class GoodsLoansMapping :
    IEntityTypeConfiguration<GoodsLoanStore>,
    IEntityTypeConfiguration<GoodsLoanStoreAgreement>,
    IEntityTypeConfiguration<GoodsLoanDeductionPolicy>,
    IEntityTypeConfiguration<GoodsLoan>,
    IEntityTypeConfiguration<GoodsLoanItem>,
    IEntityTypeConfiguration<GoodsLoanInstallment>,
    IEntityTypeConfiguration<GoodsLoanPayrollDeduction>,
    IEntityTypeConfiguration<GoodsLoanStoreSettlement>,
    IEntityTypeConfiguration<GoodsLoanStoreSettlementAllocation>,
    IEntityTypeConfiguration<GoodsLoanStatusHistory>,
    IEntityTypeConfiguration<GoodsLoanDocument>
{
    public void Configure(EntityTypeBuilder<GoodsLoanStore> b)
    {
        b.ToTable("GL_Stores");
        b.HasKey(x => x.StoreID).HasName("PK_GL_Stores");
        b.Property(x => x.StoreID).ValueGeneratedOnAdd();
        b.Property(x => x.StoreCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.StoreName).HasMaxLength(200).IsRequired();
        b.Property(x => x.RegistrationNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.ContactPerson).HasMaxLength(150).IsRequired();
        b.Property(x => x.PhoneNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.Email).HasMaxLength(150).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500).IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedDate).HasColumnType("datetime2(0)");

        b.HasIndex(x => x.StoreCode).IsUnique().HasDatabaseName("UX_GL_Stores_StoreCode");
        b.HasIndex(x => x.IsActive).HasDatabaseName("IX_GL_Stores_IsActive");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanStoreAgreement> b)
    {
        b.ToTable("GL_StoreAgreements");
        b.HasKey(x => x.AgreementID).HasName("PK_GL_StoreAgreements");
        b.Property(x => x.AgreementID).ValueGeneratedOnAdd();
        b.Property(x => x.AgreementNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.EffectiveFrom).HasColumnType("date");
        b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.Property(x => x.MaximumLoanAmount).HasPrecision(18, 2);
        b.Property(x => x.InterestRate).HasPrecision(5, 2).HasDefaultValue(0.0m);
        b.Property(x => x.SettlementMode).HasMaxLength(50).HasDefaultValue(GoodsLoanSettlementMode.Monthly);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedDate).HasColumnType("datetime2(0)");

        b.HasOne(x => x.Store)
            .WithMany(s => s.Agreements)
            .HasForeignKey(x => x.StoreID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_StoreAgreements_Stores");

        b.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(x => x.OrganisationBusinessEntityID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_StoreAgreements_Organisations");

        b.HasIndex(x => new { x.OrganisationBusinessEntityID, x.AgreementNumber }).IsUnique().HasDatabaseName("UX_GL_StoreAgreements_Org_AgreementNumber");
        b.HasIndex(x => new { x.OrganisationBusinessEntityID, x.StoreID, x.IsActive }).HasDatabaseName("IX_GL_StoreAgreements_Org_Store_Active");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanDeductionPolicy> b)
    {
        b.ToTable("GL_DeductionPolicies");
        b.HasKey(x => x.PolicyID).HasName("PK_GL_DeductionPolicies");
        b.Property(x => x.PolicyID).ValueGeneratedOnAdd();
        b.Property(x => x.PolicyName).HasMaxLength(150).IsRequired();
        b.Property(x => x.MaximumSalaryPercentage).HasPrecision(5, 2);
        b.Property(x => x.MinimumNetSalary).HasPrecision(18, 2);
        b.Property(x => x.MaximumOutstandingAmount).HasPrecision(18, 2);
        b.Property(x => x.SalaryBasis).HasMaxLength(50).IsRequired();
        b.Property(x => x.EffectiveFrom).HasColumnType("date");
        b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(x => x.OrganisationBusinessEntityID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_DeductionPolicies_Organisations");

        b.HasIndex(x => new { x.OrganisationBusinessEntityID, x.IsActive }).HasDatabaseName("IX_GL_DeductionPolicies_Org_Active");
    }

    public void Configure(EntityTypeBuilder<GoodsLoan> b)
    {
        b.ToTable("GL_Loans");
        b.HasKey(x => x.LoanID).HasName("PK_GL_Loans");
        b.Property(x => x.LoanID).ValueGeneratedOnAdd();
        b.Property(x => x.LoanNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.PurchaseDate).HasColumnType("date");
        b.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalRepayableAmount).HasPrecision(18, 2);
        b.Property(x => x.FirstDeductionMonth).HasColumnType("date");
        b.Property(x => x.SalaryAtApproval).HasPrecision(18, 2);
        b.Property(x => x.EligibleSalaryAtApproval).HasPrecision(18, 2);
        b.Property(x => x.DeductionPercentageAtApproval).HasPrecision(5, 2);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.ApprovedDate).HasColumnType("datetime2(0)");
        b.Property(x => x.ClosedDate).HasColumnType("datetime2(0)");
        b.Property(x => x.Remarks).HasMaxLength(1000);
        b.Property(x => x.CreatedDate).HasColumnType("datetime2(0)");
        b.Property(x => x.ModifiedDate).HasColumnType("datetime2(0)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Store)
            .WithMany(s => s.Loans)
            .HasForeignKey(x => x.StoreID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_Loans_Stores");

        b.HasOne(x => x.Agreement)
            .WithMany(a => a.Loans)
            .HasForeignKey(x => x.AgreementID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_Loans_StoreAgreements");

        b.HasOne(x => x.Policy)
            .WithMany(p => p.Loans)
            .HasForeignKey(x => x.PolicyID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_Loans_DeductionPolicies");

        b.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(x => x.OrganisationBusinessEntityID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_Loans_Organisations");

        b.HasOne<Individual>()
            .WithMany()
            .HasForeignKey(x => x.IndividualID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_Loans_Individuals");

        b.HasOne<Job>()
            .WithMany()
            .HasForeignKey(x => x.JobID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_Loans_Jobs");

        b.HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RequestID)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_GL_Loans_Requests");

        b.HasIndex(x => new { x.OrganisationBusinessEntityID, x.LoanNumber }).IsUnique().HasDatabaseName("UX_GL_Loans_Org_LoanNumber");
        b.HasIndex(x => new { x.OrganisationBusinessEntityID, x.IndividualID, x.Status }).HasDatabaseName("IX_GL_Loans_Org_Individual_Status");
        b.HasIndex(x => new { x.JobID, x.Status }).HasDatabaseName("IX_GL_Loans_Job_Status");
        b.HasIndex(x => x.StoreID).HasDatabaseName("IX_GL_Loans_StoreID");
        b.HasIndex(x => x.RequestID).HasDatabaseName("IX_GL_Loans_RequestID");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanItem> b)
    {
        b.ToTable("GL_LoanItems");
        b.HasKey(x => x.LoanItemID).HasName("PK_GL_LoanItems");
        b.Property(x => x.LoanItemID).ValueGeneratedOnAdd();
        b.Property(x => x.ItemDescription).HasMaxLength(300).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);

        b.HasOne(x => x.Loan)
            .WithMany(l => l.Items)
            .HasForeignKey(x => x.LoanID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_GL_LoanItems_Loans");

        b.HasIndex(x => x.LoanID).HasDatabaseName("IX_GL_LoanItems_LoanID");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanInstallment> b)
    {
        b.ToTable("GL_LoanInstallments");
        b.HasKey(x => x.InstallmentID).HasName("PK_GL_LoanInstallments");
        b.Property(x => x.InstallmentID).ValueGeneratedOnAdd();
        b.Property(x => x.DueMonth).HasColumnType("date");
        b.Property(x => x.ScheduledAmount).HasPrecision(18, 2);
        b.Property(x => x.PaidAmount).HasPrecision(18, 2).HasDefaultValue(0.0m);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.LastPaymentDate).HasColumnType("datetime2(0)");
        b.Property(x => x.CreatedDate).HasColumnType("datetime2(0)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Loan)
            .WithMany(l => l.Installments)
            .HasForeignKey(x => x.LoanID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_GL_LoanInstallments_Loans");

        b.HasIndex(x => new { x.LoanID, x.InstallmentNumber }).IsUnique().HasDatabaseName("UX_GL_LoanInstallments_Loan_Number");
        b.HasIndex(x => new { x.DueMonth, x.Status }).HasDatabaseName("IX_GL_LoanInstallments_DueMonth_Status");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanPayrollDeduction> b)
    {
        b.ToTable("GL_PayrollDeductions");
        b.HasKey(x => x.DeductionID).HasName("PK_GL_PayrollDeductions");
        b.Property(x => x.DeductionID).ValueGeneratedOnAdd();
        b.Property(x => x.TransactionType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.DeductionDate).HasColumnType("date");
        b.Property(x => x.CreatedDate).HasColumnType("datetime2(0)");

        b.HasOne(x => x.Installment)
            .WithMany(i => i.Deductions)
            .HasForeignKey(x => x.InstallmentID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_PayrollDeductions_Installments");

        b.HasIndex(x => new { x.InstallmentID, x.PayrollRunID, x.TransactionType }).HasDatabaseName("IX_GL_PayrollDeductions_Inst_Run_Type");
        b.HasIndex(x => x.PayrollRunID).HasDatabaseName("IX_GL_PayrollDeductions_PayrollRunID");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanStoreSettlement> b)
    {
        b.ToTable("GL_StoreSettlements");
        b.HasKey(x => x.SettlementID).HasName("PK_GL_StoreSettlements");
        b.Property(x => x.SettlementID).ValueGeneratedOnAdd();
        b.Property(x => x.SettlementNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.PaymentDate).HasColumnType("date");
        b.Property(x => x.PaymentReference).HasMaxLength(150).IsRequired();
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired();
        b.Property(x => x.CreatedDate).HasColumnType("datetime2(0)");

        b.HasOne(x => x.Store)
            .WithMany(s => s.Settlements)
            .HasForeignKey(x => x.StoreID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_StoreSettlements_Stores");

        b.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(x => x.OrganisationBusinessEntityID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_StoreSettlements_Organisations");

        b.HasIndex(x => new { x.OrganisationBusinessEntityID, x.SettlementNumber }).IsUnique().HasDatabaseName("UX_GL_StoreSettlements_Org_SettlementNumber");
        b.HasIndex(x => new { x.OrganisationBusinessEntityID, x.StoreID, x.Status }).HasDatabaseName("IX_GL_StoreSettlements_Org_Store_Status");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanStoreSettlementAllocation> b)
    {
        b.ToTable("GL_StoreSettlementAllocations");
        b.HasKey(x => x.AllocationID).HasName("PK_GL_StoreSettlementAllocations");
        b.Property(x => x.AllocationID).ValueGeneratedOnAdd();
        b.Property(x => x.AllocatedAmount).HasPrecision(18, 2);

        b.HasOne(x => x.Settlement)
            .WithMany(s => s.Allocations)
            .HasForeignKey(x => x.SettlementID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_GL_StoreSettlementAllocations_Settlements");

        b.HasOne(x => x.Loan)
            .WithMany(l => l.SettlementAllocations)
            .HasForeignKey(x => x.LoanID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_GL_StoreSettlementAllocations_Loans");

        b.HasIndex(x => new { x.SettlementID, x.LoanID }).HasDatabaseName("IX_GL_StoreSettlementAllocations_Settlement_Loan");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanStatusHistory> b)
    {
        b.ToTable("GL_LoanStatusHistory");
        b.HasKey(x => x.HistoryID).HasName("PK_GL_LoanStatusHistory");
        b.Property(x => x.HistoryID).ValueGeneratedOnAdd();
        b.Property(x => x.PreviousStatus).HasMaxLength(50).IsRequired();
        b.Property(x => x.NewStatus).HasMaxLength(50).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.ChangedDate).HasColumnType("datetime2(0)");

        b.HasOne(x => x.Loan)
            .WithMany(l => l.StatusHistory)
            .HasForeignKey(x => x.LoanID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_GL_LoanStatusHistory_Loans");

        b.HasIndex(x => x.LoanID).HasDatabaseName("IX_GL_LoanStatusHistory_LoanID");
    }

    public void Configure(EntityTypeBuilder<GoodsLoanDocument> b)
    {
        b.ToTable("GL_LoanDocuments");
        b.HasKey(x => x.DocumentID).HasName("PK_GL_LoanDocuments");
        b.Property(x => x.DocumentID).ValueGeneratedOnAdd();
        b.Property(x => x.FileName).HasMaxLength(250).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.UploadedDate).HasColumnType("datetime2(0)");

        b.HasOne(x => x.Loan)
            .WithMany(l => l.Documents)
            .HasForeignKey(x => x.LoanID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_GL_LoanDocuments_Loans");

        b.HasIndex(x => x.LoanID).HasDatabaseName("IX_GL_LoanDocuments_LoanID");
    }
}
