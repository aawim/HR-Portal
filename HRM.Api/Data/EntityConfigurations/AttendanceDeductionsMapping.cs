using HRM.Models;
using HRM.Models.AttendanceDeductions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class AttendanceDeductionsMapping :
    IEntityTypeConfiguration<AttendanceDeductionPolicy>,
    IEntityTypeConfiguration<AttendanceAdjustment>,
    IEntityTypeConfiguration<AttendanceExceptionReview>
{
    public void Configure(EntityTypeBuilder<AttendanceDeductionPolicy> b)
    {
        b.ToTable("ATD_AttendanceDeductionPolicies");
        b.HasKey(x => x.AttendanceDeductionPolicyId).HasName("PK_ATD_AttendanceDeductionPolicies");
        b.Property(x => x.AttendanceDeductionPolicyId).HasColumnName("AttendanceDeductionPolicyID").ValueGeneratedOnAdd();
        b.Property(x => x.OrganisationId).HasColumnName("OrganisationID");
        b.Property(x => x.PolicyCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.PolicyName).HasMaxLength(150).IsRequired();
        b.Property(x => x.LegalReference).HasMaxLength(250).IsRequired();
        b.Property(x => x.OfficialMinutesSource).HasMaxLength(50).IsRequired();
        b.Property(x => x.BufferEligibilityCategory).HasMaxLength(100).IsRequired();
        b.Property(x => x.BufferMakeUpWindow).HasMaxLength(50).IsRequired();
        b.Property(x => x.BufferExceededRule).HasMaxLength(50).IsRequired();
        b.Property(x => x.LateDeductionFormula).HasMaxLength(50).IsRequired();
        b.Property(x => x.AbsenceDeductionFormula).HasMaxLength(50).IsRequired();
        b.Property(x => x.ServiceAllowanceComponentCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.RoundingMethod).HasMaxLength(50).IsRequired();
        b.Property(x => x.ApprovedByUserId).HasColumnName("ApprovedByUserID");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.ApprovedAtUtc).HasColumnType("datetime2(0)");

        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.OrganisationId, x.PolicyCode, x.EffectiveFrom }).IsUnique()
            .HasDatabaseName("UX_ATD_AttendanceDeductionPolicies_Org_Code_Date");
        b.HasIndex(x => new { x.OrganisationId, x.IsActive })
            .HasDatabaseName("IX_ATD_AttendanceDeductionPolicies_Org_Active");
    }

    public void Configure(EntityTypeBuilder<AttendanceAdjustment> b)
    {
        b.ToTable("ATD_AttendanceAdjustments");
        b.HasKey(x => x.AttendanceAdjustmentId).HasName("PK_ATD_AttendanceAdjustments");
        b.Property(x => x.AttendanceAdjustmentId).HasColumnName("AttendanceAdjustmentID").ValueGeneratedOnAdd();
        b.Property(x => x.OrganisationId).HasColumnName("OrganisationID");
        b.Property(x => x.IndividualId).HasColumnName("IndividualID");
        b.Property(x => x.JobId).HasColumnName("JobID");
        b.Property(x => x.WorkDate).HasColumnType("date");
        b.Property(x => x.AdjustmentType).HasMaxLength(50).IsRequired();
        b.Property(x => x.EstimatedDeductionAmount).HasPrecision(18, 2);
        b.Property(x => x.BasicSalaryDeductionAmount).HasPrecision(18, 2);
        b.Property(x => x.ServiceAllowanceDeductionAmount).HasPrecision(18, 2);
        b.Property(x => x.SourceAttendanceReference).HasMaxLength(100).IsRequired();
        b.Property(x => x.AttendanceOutcome).HasMaxLength(50).IsRequired();
        b.Property(x => x.VerificationStatus).HasMaxLength(50).IsRequired();
        b.Property(x => x.VerifiedByUserId).HasColumnName("VerifiedByUserID");
        b.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.ReviewerComments).HasMaxLength(1000);
        b.Property(x => x.PolicyCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.PolicyVersion).HasMaxLength(50).IsRequired();
        b.Property(x => x.RegulatorySource).HasMaxLength(250).IsRequired();
        b.Property(x => x.PayrollProcessingStatus).HasMaxLength(50).IsRequired();
        b.Property(x => x.PayrollRunId).HasColumnName("PayrollRunID");
        b.Property(x => x.PayrollEmployeeId).HasColumnName("PayrollEmployeeID");
        b.Property(x => x.PayrollLineId).HasColumnName("PayrollLineID");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(0)");

        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Strict idempotency: prevent duplicate adjustments for the same employee, date, and type
        b.HasIndex(x => new { x.OrganisationId, x.JobId, x.WorkDate, x.AdjustmentType })
            .IsUnique()
            .HasFilter("[VerificationStatus] <> 'Cancelled'")
            .HasDatabaseName("UX_ATD_AttendanceAdjustments_Org_Job_Date_Type");

        b.HasIndex(x => new { x.OrganisationId, x.WorkDate, x.VerificationStatus })
            .HasDatabaseName("IX_ATD_AttendanceAdjustments_Org_Date_Status");

        b.HasIndex(x => new { x.OrganisationId, x.PayrollProcessingStatus, x.PayrollRunId })
            .HasDatabaseName("IX_ATD_AttendanceAdjustments_Org_PayrollStatus");

        b.HasIndex(x => x.SourceAttendanceReference)
            .HasDatabaseName("IX_ATD_AttendanceAdjustments_SourceRef");
    }

    public void Configure(EntityTypeBuilder<AttendanceExceptionReview> b)
    {
        b.ToTable("ATD_AttendanceExceptionReviews");
        b.HasKey(x => x.AttendanceExceptionReviewId).HasName("PK_ATD_AttendanceExceptionReviews");
        b.Property(x => x.AttendanceExceptionReviewId).HasColumnName("AttendanceExceptionReviewID").ValueGeneratedOnAdd();
        b.Property(x => x.AttendanceAdjustmentId).HasColumnName("AttendanceAdjustmentID");
        b.Property(x => x.OrganisationId).HasColumnName("OrganisationID");
        b.Property(x => x.Action).HasMaxLength(50).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.PerformedByUserId).HasColumnName("PerformedByUserID");
        b.Property(x => x.PerformedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.PreviousStatus).HasMaxLength(50).IsRequired();
        b.Property(x => x.NewStatus).HasMaxLength(50).IsRequired();

        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceAdjustment>().WithMany().HasForeignKey(x => x.AttendanceAdjustmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.AttendanceAdjustmentId, x.PerformedAtUtc })
            .HasDatabaseName("IX_ATD_AttendanceExceptionReviews_Adj_Time");
    }
}
