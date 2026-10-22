using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<DutyChangeRequest> DutyChangeRequests => Set<DutyChangeRequest>();
}

public sealed class DutyChangeRequestMapping : IEntityTypeConfiguration<DutyChangeRequest>
{
    public void Configure(EntityTypeBuilder<DutyChangeRequest> b)
    {
        b.ToTable("DC_DutyChangeRequests"); b.HasKey(x => x.DutyChangeRequestId);
        b.Property(x => x.Kind).HasMaxLength(12); b.Property(x => x.Reason).HasMaxLength(500); b.Property(x => x.Acceptance).HasMaxLength(16);
        b.Property(x => x.SourceFingerprint).HasMaxLength(64); b.Property(x => x.SwapFingerprint).HasMaxLength(64);
        b.HasIndex(x => x.RequestId).IsUnique(); b.HasIndex(x => new { x.OrganisationId, x.IsOpen });
        b.HasIndex(x => x.SourceAssignmentId).IsUnique().HasFilter("[IsOpen] = 1");
        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkAssignment>().WithMany().HasForeignKey(x => x.SourceAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkAssignment>().WithMany().HasForeignKey(x => x.SwapAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.SourceJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.TargetJobId).OnDelete(DeleteBehavior.Restrict);
    }
}
