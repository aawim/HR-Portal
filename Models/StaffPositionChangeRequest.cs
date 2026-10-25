using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class StaffPositionChangeRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public int PositionId { get; set; }
    public string ChangeKind { get; set; } = "Assign";
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? OriginalJson { get; set; }
    public string Reason { get; set; } = "";
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
}
public sealed class StaffPositionChangeRequestMapping : IEntityTypeConfiguration<StaffPositionChangeRequest>
{
    public void Configure(EntityTypeBuilder<StaffPositionChangeRequest> b)
    {
        b.ToTable("SP_StaffPositionChangeRequests", t => {
            t.HasCheckConstraint("CK_SP_PositionRequest_Kind", "[ChangeKind] IN ('Assign','Change','Dates','Remove')");
            t.HasCheckConstraint("CK_SP_PositionRequest_Dates", "[ToDate] IS NULL OR [ToDate] >= [FromDate]");
        });
        b.HasKey(x=>x.RequestId); b.Property(x=>x.RequestId).ValueGeneratedNever();
        b.Property(x=>x.ChangeKind).HasMaxLength(20); b.Property(x=>x.Reason).HasMaxLength(500);
        b.Property(x=>x.OriginalJson).HasMaxLength(4000);
        b.Property(x=>x.FromDate).HasColumnType("date"); b.Property(x=>x.ToDate).HasColumnType("date");
        b.HasOne(x=>x.Request).WithOne().HasForeignKey<StaffPositionChangeRequest>(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Position>().WithMany().HasForeignKey(x=>x.PositionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.OrganisationId,x.JobId});
    }
}
public partial class HrmTeContext { public DbSet<StaffPositionChangeRequest> StaffPositionChangeRequests => Set<StaffPositionChangeRequest>(); }
