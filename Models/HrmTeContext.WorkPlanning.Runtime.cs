using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext
{
    private void ConfigureWorkPlanningRuntime(ModelBuilder modelBuilder)
    {
        ConfigureWorkAssignmentState(modelBuilder);
        ConfigureWorkAssignmentTransferState(modelBuilder);
        ConfigureWorkPlan(modelBuilder);
        ConfigureWorkAssignment(modelBuilder);
        ConfigureWorkAssignmentSegment(modelBuilder);
        ConfigureWorkAssignmentOwner(modelBuilder);
        ConfigureWorkAssignmentTransfer(modelBuilder);
    }

    private static void ConfigureWorkAssignmentState(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkAssignmentState>(entity =>
        {
            entity.ToTable("WorkAssignmentStates");

            entity.HasKey(e => e.WorkAssignmentStateId);

            entity.Property(e => e.WorkAssignmentStateId)
                .HasColumnName("WorkAssignmentStateID");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.IsFinalState)
                .HasDefaultValue(false);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("UX_WorkAssignmentStates_Code");
        });
    }

    private static void ConfigureWorkAssignmentTransferState(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkAssignmentTransferState>(entity =>
        {
            entity.ToTable("WorkAssignmentTransferStates");

            entity.HasKey(e => e.WorkAssignmentTransferStateId);

            entity.Property(e => e.WorkAssignmentTransferStateId)
                .HasColumnName("WorkAssignmentTransferStateID");

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.IsProcessingState)
                .HasDefaultValue(false);

            entity.Property(e => e.IsFinalState)
                .HasDefaultValue(false);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("UX_WorkAssignmentTransferStates_Code");
        });
    }

    //private static void ConfigureWorkPlan(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.Entity<WorkPlan>(entity =>
    //    {
    //        entity.ToTable("WorkPlans");

    //        entity.HasKey(e => e.WorkPlanId);

    //        entity.Property(e => e.WorkPlanId)
    //            .HasColumnName("WorkPlanID");

    //        entity.Property(e => e.IndividualId)
    //            .HasColumnName("IndividualID");

    //        entity.Property(e => e.JobId)
    //            .HasColumnName("JobID");

    //        entity.Property(e => e.OrganisationBusinessEntityId)
    //            .HasColumnName("OrganisationBusinessEntityID");

    //        entity.Property(e => e.PlanningProviderId)
    //            .HasColumnName("PlanningProviderID");

    //        entity.Property(e => e.WorkDate)
    //            .HasColumnType("date");

    //        entity.Property(e => e.GenerationSource)
    //            .HasMaxLength(100)
    //            .IsRequired();

    //        entity.Property(e => e.GeneratedDate)
    //            .HasColumnType("datetime2");

    //        entity.Property(e => e.GeneratedByUserId)
    //            .HasColumnName("GeneratedByUserID");

    //        entity.Property(e => e.IsFinalized)
    //            .HasDefaultValue(false);

    //        entity.Property(e => e.FinalizedDate)
    //            .HasColumnType("datetime2");

    //        entity.Property(e => e.FinalizedByUserId)
    //            .HasColumnName("FinalizedByUserID");

    //        entity.Property(e => e.Remarks)
    //            .HasMaxLength(1000);

    //        entity.Property(e => e.IsValid)
    //            .HasDefaultValue(true);

    //        entity.Property(e => e.OperationLogId)
    //            .HasColumnName("OperationLogID");

    //        entity.Property(e => e.CreatedDate)
    //            .HasColumnType("datetime2");

    //        entity.HasIndex(e => new
    //        {
    //            e.JobId,
    //            e.WorkDate
    //        })
    //            .IsUnique()
    //            .HasDatabaseName("UX_WorkPlans_JobID_WorkDate");

    //        entity.HasIndex(e => new
    //        {
    //            e.IndividualId,
    //            e.WorkDate
    //        })
    //            .HasDatabaseName("IX_WorkPlans_IndividualID_WorkDate");

    //        entity.HasIndex(e => new
    //        {
    //            e.OrganisationBusinessEntityId,
    //            e.WorkDate
    //        })
    //            .HasDatabaseName(
    //                "IX_WorkPlans_OrganisationBusinessEntityID_WorkDate");

    //        entity.HasOne(e => e.Individual)
    //            .WithMany()
    //            .HasForeignKey(e => e.IndividualId)
    //            .OnDelete(DeleteBehavior.Restrict);

    //        entity.HasOne(e => e.Job)
    //            .WithMany()
    //            .HasForeignKey(e => e.JobId)
    //            .OnDelete(DeleteBehavior.Restrict);

    //        entity.HasOne(e => e.Organisation)
    //            .WithMany()
    //            .HasForeignKey(e => e.OrganisationBusinessEntityId)
    //            .OnDelete(DeleteBehavior.Restrict);

    //        entity.HasOne(e => e.PlanningProvider)
    //            .WithMany(e => e.WorkPlans)
    //            .HasForeignKey(e => e.PlanningProviderId)
    //            .OnDelete(DeleteBehavior.Restrict);

    //        entity.HasOne(e => e.GeneratedByUser)
    //            .WithMany()
    //            .HasForeignKey(e => e.GeneratedByUserId)
    //            .OnDelete(DeleteBehavior.Restrict);

    //        entity.HasOne(e => e.FinalizedByUser)
    //            .WithMany()
    //            .HasForeignKey(e => e.FinalizedByUserId)
    //            .OnDelete(DeleteBehavior.Restrict);

    //        entity.HasOne(e => e.OperationLog)
    //            .WithMany()
    //            .HasForeignKey(e => e.OperationLogId)
    //            .OnDelete(DeleteBehavior.Restrict);
    //    });
    //}


    private static void ConfigureWorkAssignment(
     ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkAssignment>(entity =>
        {
            entity.ToTable("WorkAssignments");

            entity.HasKey(x => x.WorkAssignmentId)
                .HasName("PK_WorkAssignments");

            entity.Property(x => x.WorkAssignmentId)
                .HasColumnName("WorkAssignmentID")
                .UseIdentityColumn(1, 1)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.WorkPlanId)
                .HasColumnName("WorkPlanID");

            entity.Property(x => x.WorkTemplateId)
                .HasColumnName("WorkTemplateID");

            entity.Property(x => x.WorkTemplateTypeId)
                .HasColumnName("WorkTemplateTypeID");

            entity.Property(x => x.WorkAssignmentStateId)
                .HasColumnName("WorkAssignmentStateID");

            entity.Property(x => x.Name)
                .HasColumnName("Name")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasColumnName("Code")
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasColumnName("Description");

            entity.Property(x => x.StartDateTime)
                .HasColumnName("StartDateTime")
                .HasColumnType("datetime2(0)");

            entity.Property(x => x.EndDateTime)
                .HasColumnName("EndDateTime")
                .HasColumnType("datetime2(0)");

            entity.Property(x => x.GraceMinutes)
                .HasColumnName("GraceMinutes")
                .HasDefaultValue(0);

            entity.Property(x => x.RequiresAttendance)
                .HasColumnName("RequiresAttendance")
                .HasDefaultValue(false);

            entity.Property(x => x.RequiresCheckOut)
                .HasColumnName("RequiresCheckOut")
                .HasDefaultValue(false);

            entity.Property(x => x.Priority)
                .HasColumnName("Priority")
                .HasDefaultValue(0);

            entity.Property(x => x.AssignmentSource)
                .HasColumnName("AssignmentSource")
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(x => x.SourceReferenceType)
                .HasColumnName("SourceReferenceType")
                .HasConversion<string>()
                 .HasMaxLength(50);

            entity.Property(x => x.SourceReferenceId)
                .HasColumnName("SourceReferenceID");

            entity.Property(x => x.LocationName)
                .HasColumnName("LocationName")
                .HasMaxLength(250);

            entity.Property(x => x.Latitude)
                .HasColumnName("Latitude")
                .HasPrecision(10, 7);

            entity.Property(x => x.Longitude)
                .HasColumnName("Longitude")
                .HasPrecision(10, 7);

            entity.Property(x => x.AllowedRadiusMeters)
                .HasColumnName("AllowedRadiusMeters")
                .HasPrecision(18, 2);

            entity.Property(x => x.CancelledDate)
                .HasColumnName("CancelledDate")
                .HasColumnType("datetime2(0)");

            entity.Property(x => x.CancellationReason)
                .HasColumnName("CancellationReason")
                .HasMaxLength(1000);

            entity.Property(x => x.CancelledByUserId)
                .HasColumnName("CancelledByUserID");

            entity.Property(x => x.IsValid)
                .HasColumnName("IsValid")
                .HasDefaultValue(true);

            entity.Property(x => x.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.Property(x => x.CreatedDate)
                .HasColumnName("CreatedDate")
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSDATETIME()");

            entity.Property(x => x.CreatedByUserId)
                .HasColumnName("CreatedByUserID");

            entity.HasOne(x => x.WorkPlan)
                .WithMany(x => x.WorkAssignments)
                .HasForeignKey(x => x.WorkPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkTemplate)
                .WithMany(x => x.WorkAssignments)
                .HasForeignKey(x => x.WorkTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkTemplateType)
                .WithMany(x => x.WorkAssignments)
                .HasForeignKey(x => x.WorkTemplateTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkAssignmentState)
                .WithMany(x => x.WorkAssignments)
                .HasForeignKey(x => x.WorkAssignmentStateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.OperationLog)
                .WithMany()
                .HasForeignKey(x => x.OperationLogId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CancelledByUser)
                .WithMany()
                .HasForeignKey(x => x.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }




    private static void ConfigureWorkAssignmentSegment(
      ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkAssignmentSegment>(entity =>
        {
            entity.ToTable("WorkAssignmentSegments");

            entity.HasKey(x => x.WorkAssignmentSegmentId);

            entity.Property(x => x.WorkAssignmentSegmentId)
                .HasColumnName("WorkAssignmentSegmentID")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.WorkAssignmentId)
                .HasColumnName("WorkAssignmentID");

            entity.Property(x => x.WorkTemplateSegmentId)
                .HasColumnName("WorkTemplateSegmentID");

            entity.Property(x => x.WorkSegmentTypeId)
                .HasColumnName("WorkSegmentTypeID");

            entity.Property(x => x.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.HasOne(x => x.WorkAssignment)
                .WithMany(x => x.WorkAssignmentSegments)
                .HasForeignKey(x => x.WorkAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.WorkTemplateSegment)
                .WithMany(x => x.WorkAssignmentSegments)
                .HasForeignKey(x => x.WorkTemplateSegmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkSegmentType)
                .WithMany(x => x.WorkAssignmentSegments)
                .HasForeignKey(x => x.WorkSegmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
    private static void ConfigureWorkAssignmentOwner(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkAssignmentOwner>(entity =>
        {
            entity.ToTable("WorkAssignmentOwners");

            entity.HasKey(e => e.WorkAssignmentOwnerId);

            entity.Property(e => e.WorkAssignmentOwnerId)
                .HasColumnName("WorkAssignmentOwnerID");

            entity.Property(e => e.WorkAssignmentId)
                .HasColumnName("WorkAssignmentID");

            entity.Property(e => e.IndividualId)
                .HasColumnName("IndividualID");

            entity.Property(e => e.JobId)
                .HasColumnName("JobID");

            entity.Property(e => e.OwnershipType)
                .HasConversion<string>()
                .HasColumnName("OwnershipType")
                .IsRequired();

            entity.Property(e => e.AssignedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.AssignedByUserId)
                .HasColumnName("AssignedByUserID");

            entity.Property(e => e.EffectiveFrom)
                .HasColumnType("datetime2");

            entity.Property(e => e.EffectiveTo)
                .HasColumnType("datetime2");

            entity.Property(e => e.RelievedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RelievedByUserId)
                .HasColumnName("RelievedByUserID");

            entity.Property(e => e.ReliefReason)
                .HasMaxLength(1000);

            entity.Property(e => e.IsCurrentOwner)
                .HasDefaultValue(true);

            entity.Property(e => e.IsValid)
                .HasDefaultValue(true);

            entity.Property(e => e.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.HasIndex(e => new
            {
                e.WorkAssignmentId,
                e.IsCurrentOwner
            })
                .HasDatabaseName(
                    "IX_WorkAssignmentOwners_AssignmentID_CurrentOwner");

            entity.HasIndex(e => new
            {
                e.IndividualId,
                e.EffectiveFrom,
                e.EffectiveTo
            })
                .HasDatabaseName(
                    "IX_WorkAssignmentOwners_IndividualID_EffectiveDates");

            entity.HasOne(e => e.WorkAssignment)
                .WithMany(e => e.WorkAssignmentOwners)
                .HasForeignKey(e => e.WorkAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Individual)
                .WithMany()
                .HasForeignKey(e => e.IndividualId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RelievedByUser)
                .WithMany()
                .HasForeignKey(e => e.RelievedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OperationLog)
                .WithMany()
                .HasForeignKey(e => e.OperationLogId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureWorkAssignmentTransfer(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkAssignmentTransfer>(entity =>
        {
            entity.ToTable("WorkAssignmentTransfers");

            entity.HasKey(e => e.WorkAssignmentTransferId);

            entity.Property(e => e.WorkAssignmentTransferId)
                .HasColumnName("WorkAssignmentTransferID");

            entity.Property(e => e.WorkAssignmentId)
                .HasColumnName("WorkAssignmentID");

            entity.Property(e => e.WorkAssignmentTransferStateId)
                .HasColumnName("WorkAssignmentTransferStateID");

            entity.Property(e => e.FromWorkAssignmentOwnerId)
                .HasColumnName("FromWorkAssignmentOwnerID");

            entity.Property(e => e.ToWorkAssignmentOwnerId)
                .HasColumnName("ToWorkAssignmentOwnerID");

            entity.Property(e => e.FromIndividualId)
                .HasColumnName("FromIndividualID");

            entity.Property(e => e.FromJobId)
                .HasColumnName("FromJobID");

            entity.Property(e => e.ToIndividualId)
                .HasColumnName("ToIndividualID");

            entity.Property(e => e.ToJobId)
                .HasColumnName("ToJobID");

            entity.Property(e => e.TransferType)
                .HasConversion<int>()
                .HasColumnName("TransferType")
                .IsRequired();

            entity.Property(e => e.Reason)
                .HasMaxLength(1000);

            entity.Property(e => e.EffectiveFrom)
                .HasColumnType("datetime2");

            entity.Property(e => e.EffectiveTo)
                .HasColumnType("datetime2");

            entity.Property(e => e.RequestId)
                .HasColumnName("RequestID");

            entity.Property(e => e.RequestedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RequestedByUserId)
                .HasColumnName("RequestedByUserID");

            entity.Property(e => e.ApprovedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.ApprovedByUserId)
                .HasColumnName("ApprovedByUserID");

            entity.Property(e => e.RejectedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.RejectedByUserId)
                .HasColumnName("RejectedByUserID");

            entity.Property(e => e.RejectionReason)
                .HasMaxLength(1000);

            entity.Property(e => e.CompletedDate)
                .HasColumnType("datetime2");

            entity.Property(e => e.IsValid)
                .HasDefaultValue(true);

            entity.Property(e => e.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.HasIndex(e => new
            {
                e.WorkAssignmentId,
                e.WorkAssignmentTransferStateId
            })
                .HasDatabaseName(
                    "IX_WorkAssignmentTransfers_AssignmentID_StateID");

            entity.HasIndex(e => new
            {
                e.ToIndividualId,
                e.EffectiveFrom
            })
                .HasDatabaseName(
                    "IX_WorkAssignmentTransfers_ToIndividualID_EffectiveFrom");

          

            entity.HasOne(e => e.WorkAssignmentTransferState)
                .WithMany(e => e.WorkAssignmentTransfers)
                .HasForeignKey(e => e.WorkAssignmentTransferStateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FromWorkAssignmentOwner)
                .WithMany()
                .HasForeignKey(e => e.FromWorkAssignmentOwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ToWorkAssignmentOwner)
                .WithMany()
                .HasForeignKey(e => e.ToWorkAssignmentOwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Request)
                .WithMany()
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OperationLog)
                .WithMany()
                .HasForeignKey(e => e.OperationLogId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}