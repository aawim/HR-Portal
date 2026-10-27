using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class SupervisorChangeRequestMapping : IEntityTypeConfiguration<SupervisorChangeRequest>
{
    public void Configure(EntityTypeBuilder<SupervisorChangeRequest> b)
    {
        b.ToTable("SV_SupervisorChangeRequests",t=>t.HasCheckConstraint("CK_SV_SupervisorChangeRequests_Target","[TargetKind] IN ('Department','Team')"));b.HasKey(x=>x.RequestId);b.Property(x=>x.RequestId).ValueGeneratedNever();
        b.Property(x=>x.TargetKind).HasMaxLength(20);b.Property(x=>x.TargetName).HasMaxLength(500);b.Property(x=>x.Reason).HasMaxLength(500);b.Property(x=>x.OriginalVersion).HasMaxLength(64);
        b.HasIndex(x=>new{x.OrganisationId,x.TargetKind,x.TargetId,x.IndividualId}).IsUnique().HasFilter("[IsOpen] = 1");
        b.HasOne(x=>x.Request).WithOne().HasForeignKey<SupervisorChangeRequest>(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x=>x.IndividualId).HasConstraintName("FK_SV_SupervisorChangeRequests_Individuals_IndividualId").OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext { public DbSet<SupervisorChangeRequest> SupervisorChangeRequests => Set<SupervisorChangeRequest>(); }
