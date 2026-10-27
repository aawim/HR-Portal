using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Data.Configurations
{
    public sealed class AttendanceResolutionStatusConfiguration
    : IEntityTypeConfiguration<AttendanceResolutionStatus>
    {
        public void Configure(
            EntityTypeBuilder<AttendanceResolutionStatus> entity)
        {
            entity.ToTable("AttendanceResolutionStatuses");

            entity.HasKey(x =>
                x.AttendanceResolutionStatusId);

            entity.Property(x =>
                    x.AttendanceResolutionStatusId)
                .HasColumnName(
                    "AttendanceResolutionStatusID")
                .ValueGeneratedNever();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasIndex(x => x.Name)
                .IsUnique();
        }
    }
}
