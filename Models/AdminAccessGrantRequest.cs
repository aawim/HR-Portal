using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class AdminAccessGrantRequest
{
    public int RequestId { get; set; }
    public int UserId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int CreatedByUserId { get; set; }
    public string GroupIdsJson { get; set; } = "[]";
    public string PermissionIdsJson { get; set; } = "[]";
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public bool IsOpen { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public Request Request { get; set; } = null!;
}

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
