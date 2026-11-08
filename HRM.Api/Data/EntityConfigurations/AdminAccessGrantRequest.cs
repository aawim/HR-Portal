using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class AdminAccessGrantMapping : IEntityTypeConfiguration<AdminAccessGrantRequest>
{
    public void Configure(EntityTypeBuilder<AdminAccessGrantRequest> b)
    {
        b.ToTable("AA_AdminAccessGrantRequests"); b.HasKey(x => x.RequestId);
        b.Property(x => x.RequestId).ValueGeneratedNever();
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne(x => x.Request).WithOne().HasForeignKey<AdminAccessGrantRequest>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.UserId).IsUnique().HasFilter("[IsOpen] = 1");
    }
}


public partial class HrmTeContext
{
    public DbSet<AdminAccessGrantRequest> AdminAccessGrantRequests => Set<AdminAccessGrantRequest>();
}
