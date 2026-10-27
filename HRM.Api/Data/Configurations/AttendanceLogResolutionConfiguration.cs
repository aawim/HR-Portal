using HRM.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace HRM.Data.Configurations
{
    public sealed class AttendanceLogResolutionConfiguration
    : IEntityTypeConfiguration<AttendanceLogResolution>
    {
        public void Configure(
            EntityTypeBuilder<AttendanceLogResolution> entity)
        {
            entity.ToTable("AttendanceLogResolutions");

            entity.HasKey(x => x.AttendanceLogResolutionId);

            entity.Property(x => x.AttendanceLogResolutionId)
                .HasColumnName("AttendanceLogResolutionID");

            entity.Property(x => x.AttendanceLogId)
                .HasColumnName("AttendanceLogID");

            entity.Property(x => x.WorkPlanId)
                .HasColumnName("WorkPlanID");

            entity.Property(x => x.WorkAssignmentId)
                .HasColumnName("WorkAssignmentID");

            entity.Property(x => x.WorkAssignmentSegmentId)
                .HasColumnName("WorkAssignmentSegmentID");

            entity.Property(x => x.JobId)
                .HasColumnName("JobID");

            entity.Property(x => x.AttendanceResolutionStatusId)
                .HasColumnName("AttendanceResolutionStatusID");

            entity.Property(x => x.ResolutionDate)
                .HasColumnType("datetime2(0)");

            entity.Property(x => x.ResolutionMessage)
                .HasMaxLength(1000);

            entity.Property(x => x.Latitude)
                .HasPrecision(10, 8);

            entity.Property(x => x.Longitude)
                .HasPrecision(11, 8);

            entity.Property(x => x.DeviceIdentifier)
                .HasMaxLength(200);

            entity.Property(x => x.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.Property(x => x.CreatedDate)
                .HasColumnType("datetime2(0)");

            entity.HasOne(x => x.AttendanceLog)
                .WithMany(x => x.AttendanceLogResolutions)
                .HasForeignKey(x => x.AttendanceLogId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AttendanceResolutionStatus)
                .WithMany(x => x.AttendanceLogResolutions)
                .HasForeignKey(x =>
                    x.AttendanceResolutionStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkPlan)
                .WithMany(x => x.AttendanceLogResolutions)
                .HasForeignKey(x => x.WorkPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkAssignment)
                .WithMany(x => x.AttendanceLogResolutions)
                .HasForeignKey(x => x.WorkAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkAssignmentSegment)
                .WithMany(x => x.AttendanceLogResolutions)
                .HasForeignKey(x =>
                    x.WorkAssignmentSegmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.AttendanceLogId)
                .IsUnique()
                .HasFilter("[IsValid] = 1")
                .HasDatabaseName(
                    "UX_AttendanceLogResolutions_ActiveAttendanceLog");

            entity.HasIndex(x => new
            {
                x.WorkAssignmentId,
                x.AttendanceResolutionStatusId,
                x.IsValid
            })
                .HasDatabaseName(
                    "IX_AttendanceLogResolutions_WorkAssignment");
        }
    }
}
