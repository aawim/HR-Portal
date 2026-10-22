using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class StaffOnboardingRequest
{
    public int RequestId { get; set; }
    public int IndividualId { get; set; }
    public int OrganisationId { get; set; }
    public int OrganisationStructureId { get; set; }
    public int JobTypeId { get; set; }
    public int? PositionId { get; set; }
    public DateTime JoinedDate { get; set; }
    public string Remarks { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsOpen { get; set; } = true;
    public int? JobId { get; set; }
    public int? UserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Request Request { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
}

public sealed class StaffOnboardingMapping : IEntityTypeConfiguration<StaffOnboardingRequest>
{
    public void Configure(EntityTypeBuilder<StaffOnboardingRequest> b)
    {
        b.ToTable("SO_StaffOnboardingRequests"); b.HasKey(x => x.RequestId);
        b.Property(x => x.RequestId).ValueGeneratedNever();
        b.Property(x => x.JoinedDate).HasColumnType("date");
        b.Property(x => x.Remarks).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne(x => x.Request).WithOne().HasForeignKey<StaffOnboardingRequest>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OrganisationStructure>().WithMany().HasForeignKey(x => x.OrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JobType>().WithMany().HasForeignKey(x => x.JobTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Position>().WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.IndividualId).IsUnique().HasFilter("[IsOpen] = 1");
        b.HasIndex(x => new { x.OrganisationId, x.IsOpen });
    }
}

public partial class HrmTeContext
{
    public DbSet<StaffOnboardingRequest> StaffOnboardingRequests => Set<StaffOnboardingRequest>();
}
