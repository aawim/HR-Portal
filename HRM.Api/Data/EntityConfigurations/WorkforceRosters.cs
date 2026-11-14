using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class WorkforceRosterMapping : IEntityTypeConfiguration<WorkforceRoster>
{
    public void Configure(EntityTypeBuilder<WorkforceRoster> b)
    {
        b.ToTable("WS_Rosters"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(200); b.Property(x => x.Status).HasMaxLength(30);
        b.Property(x => x.StartsOn).HasColumnType("date"); b.Property(x => x.EndsOn).HasColumnType("date"); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.StartsOn, x.EndsOn }); b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class WorkforceRosterVersionMapping : IEntityTypeConfiguration<WorkforceRosterVersion>
{
    public void Configure(EntityTypeBuilder<WorkforceRosterVersion> b)
    {
        b.ToTable("WS_RosterVersions"); b.HasKey(x => x.Id); b.Property(x => x.Status).HasMaxLength(30); b.Property(x => x.ChangeReason).HasMaxLength(1000);
        b.HasIndex(x => new { x.WorkforceRosterId, x.VersionNumber }).IsUnique(); b.HasOne(x => x.WorkforceRoster).WithMany().HasForeignKey(x => x.WorkforceRosterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class DutyOccurrenceMapping : IEntityTypeConfiguration<DutyOccurrence>
{
    public void Configure(EntityTypeBuilder<DutyOccurrence> b)
    {
        b.ToTable("WS_DutyOccurrences"); b.HasKey(x => x.Id); b.Property(x => x.TimeZoneId).HasMaxLength(100); b.Property(x => x.LocationName).HasMaxLength(300); b.Property(x => x.Status).HasMaxLength(30); b.Property(x => x.SourceKey).HasMaxLength(200); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.WorkforceRosterVersionId, x.StartAt, x.EndAt }); b.HasIndex(x => new { x.WorkforceRosterVersionId, x.SourceKey }).IsUnique().HasFilter("[SourceKey] IS NOT NULL");
        b.HasOne(x => x.WorkforceRosterVersion).WithMany().HasForeignKey(x => x.WorkforceRosterVersionId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.DutyType).WithMany().HasForeignKey(x => x.DutyTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class DutyAssignmentMapping : IEntityTypeConfiguration<DutyAssignment>
{
    public void Configure(EntityTypeBuilder<DutyAssignment> b)
    {
        b.ToTable("WS_DutyAssignments"); b.HasKey(x => x.Id); b.Property(x => x.Status).HasMaxLength(30); b.Property(x => x.AssignmentReason).HasMaxLength(1000); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.DutyOccurrenceId, x.JobId }).IsUnique(); b.HasIndex(x => new { x.IndividualId, x.Status });
        b.HasOne(x => x.DutyOccurrence).WithMany().HasForeignKey(x => x.DutyOccurrenceId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<WorkforceRoster> WorkforceRosters => Set<WorkforceRoster>(); public DbSet<WorkforceRosterVersion> WorkforceRosterVersions => Set<WorkforceRosterVersion>();
    public DbSet<DutyOccurrence> DutyOccurrences => Set<DutyOccurrence>(); public DbSet<DutyAssignment> DutyAssignments => Set<DutyAssignment>();
}
