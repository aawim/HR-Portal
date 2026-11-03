using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class AttendanceReconciliationRunMapping : IEntityTypeConfiguration<AttendanceReconciliationRun>
{
    public void Configure(EntityTypeBuilder<AttendanceReconciliationRun> b)
    {
        b.ToTable("AR_AttendanceReconciliationRuns");
        b.HasKey(x => x.AttendanceReconciliationRunId).HasName("PK_AR_AttendanceReconciliationRuns");
        b.Property(x => x.AttendanceReconciliationRunId).HasColumnName("AttendanceReconciliationRunID").ValueGeneratedOnAdd();
        b.Property(x => x.OrganisationId).HasColumnName("OrganisationID");
        b.Property(x => x.IndividualId).HasColumnName("IndividualID");
        b.Property(x => x.JobId).HasColumnName("JobID");
        b.Property(x => x.FromWorkDate).HasColumnType("date");
        b.Property(x => x.ToWorkDate).HasColumnType("date");
        b.Property(x => x.TriggerSource).HasMaxLength(50).IsRequired();
        b.Property(x => x.PolicyVersion).HasMaxLength(40).IsRequired();
        b.Property(x => x.InitiatedByUserId).HasColumnName("InitiatedByUserID");
        b.Property(x => x.OperationLogId).HasColumnName("OperationLogID");
        b.Property(x => x.StartedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.SummaryMessage).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(false);

        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.InitiatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.OrganisationId, x.IndividualId, x.FromWorkDate, x.ToWorkDate })
            .HasDatabaseName("IX_AR_AttendanceReconciliationRuns_OrgIndividualDate");
    }
}

public sealed class AttendanceObligationMapping : IEntityTypeConfiguration<AttendanceObligation>
{
    public void Configure(EntityTypeBuilder<AttendanceObligation> b)
    {
        b.ToTable("AR_AttendanceObligations");
        b.HasKey(x => x.AttendanceObligationId).HasName("PK_AR_AttendanceObligations");
        b.Property(x => x.AttendanceObligationId).HasColumnName("AttendanceObligationID").ValueGeneratedOnAdd();
        b.Property(x => x.OrganisationId).HasColumnName("OrganisationID");
        b.Property(x => x.IndividualId).HasColumnName("IndividualID");
        b.Property(x => x.JobId).HasColumnName("JobID");
        b.Property(x => x.WorkDate).HasColumnType("date");
        b.Property(x => x.WorkPlanId).HasColumnName("WorkPlanID");
        b.Property(x => x.WorkAssignmentId).HasColumnName("WorkAssignmentID");
        b.Property(x => x.WorkPlanSegmentId).HasColumnName("WorkPlanSegmentID");
        b.Property(x => x.ObligationType).HasMaxLength(30).IsRequired();
        b.Property(x => x.ScheduleName).HasMaxLength(250).IsRequired();
        b.Property(x => x.ScheduledStart).HasColumnType("datetime2(0)");
        b.Property(x => x.ScheduledEnd).HasColumnType("datetime2(0)");
        b.Property(x => x.ObligationState).HasMaxLength(30).IsRequired();
        b.Property(x => x.ResolvedStartAttendanceLogId).HasColumnName("ResolvedStartAttendanceLogID");
        b.Property(x => x.ResolvedEndAttendanceLogId).HasColumnName("ResolvedEndAttendanceLogID");
        b.Property(x => x.ActualStartDateTime).HasColumnType("datetime2(0)");
        b.Property(x => x.ActualEndDateTime).HasColumnType("datetime2(0)");
        b.Property(x => x.StatusReason).HasMaxLength(500);
        b.Property(x => x.PolicyVersion).HasMaxLength(40).IsRequired();
        b.Property(x => x.ReconciliationRunId).HasColumnName("ReconciliationRunID");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(0)");

        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkPlan>().WithMany().HasForeignKey(x => x.WorkPlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkAssignment>().WithMany().HasForeignKey(x => x.WorkAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkPlanSegment>().WithMany().HasForeignKey(x => x.WorkPlanSegmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.ResolvedStartAttendanceLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.ResolvedEndAttendanceLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceReconciliationRun>().WithMany().HasForeignKey(x => x.ReconciliationRunId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.IndividualId, x.JobId, x.WorkDate, x.IsValid })
            .HasDatabaseName("IX_AR_AttendanceObligations_EmployeeWorkDate");
    }
}

public sealed class AttendanceManualOverrideMapping : IEntityTypeConfiguration<AttendanceManualOverride>
{
    public void Configure(EntityTypeBuilder<AttendanceManualOverride> b)
    {
        b.ToTable("AR_AttendanceManualOverrides");
        b.HasKey(x => x.AttendanceManualOverrideId).HasName("PK_AR_AttendanceManualOverrides");
        b.Property(x => x.AttendanceManualOverrideId).HasColumnName("AttendanceManualOverrideID").ValueGeneratedOnAdd();
        b.Property(x => x.OrganisationId).HasColumnName("OrganisationID");
        b.Property(x => x.IndividualId).HasColumnName("IndividualID");
        b.Property(x => x.JobId).HasColumnName("JobID");
        b.Property(x => x.WorkDate).HasColumnType("date");
        b.Property(x => x.AttendanceLogId).HasColumnName("AttendanceLogID");
        b.Property(x => x.WorkPlanId).HasColumnName("WorkPlanID");
        b.Property(x => x.WorkAssignmentId).HasColumnName("WorkAssignmentID");
        b.Property(x => x.WorkPlanSegmentId).HasColumnName("WorkPlanSegmentID");
        b.Property(x => x.OverrideAction).HasMaxLength(50).IsRequired();
        b.Property(x => x.TargetSchedulePurpose).HasMaxLength(30).IsRequired();
        b.Property(x => x.TargetClockTypeId).HasColumnName("TargetClockTypeID");
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserID");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(0)");
        b.Property(x => x.SupersededByOverrideId).HasColumnName("SupersededByOverrideID");
        b.Property(x => x.OperationLogId).HasColumnName("OperationLogID");

        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.AttendanceLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkPlan>().WithMany().HasForeignKey(x => x.WorkPlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkAssignment>().WithMany().HasForeignKey(x => x.WorkAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkPlanSegment>().WithMany().HasForeignKey(x => x.WorkPlanSegmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.IndividualId, x.JobId, x.WorkDate, x.IsActive })
            .HasDatabaseName("IX_AR_AttendanceManualOverrides_EmployeeWorkDate");
    }
}

public partial class HrmTeContext
{
    public DbSet<AttendanceReconciliationRun> AttendanceReconciliationRuns => Set<AttendanceReconciliationRun>();
    public DbSet<AttendanceObligation> AttendanceObligations => Set<AttendanceObligation>();
    public DbSet<AttendanceManualOverride> AttendanceManualOverrides => Set<AttendanceManualOverride>();
}
