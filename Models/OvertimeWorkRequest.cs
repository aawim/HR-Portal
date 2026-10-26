using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

// Proposed work and missing punches remain separate from payable claims.
public sealed class OvertimeWorkRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public string Kind { get; set; } = "Preapproval";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime? ProposedStart { get; set; }
    public DateTime? ProposedEnd { get; set; }
    public int? StartLogId { get; set; }
    public int? EndLogId { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
    public ICollection<OvertimeWorkTask> Tasks { get; set; } = new List<OvertimeWorkTask>();
}
public sealed class OvertimeWorkTask
{
    public int Id { get; set; }
    public int WorkRequestId { get; set; }
    public string Description { get; set; } = "";
    public OvertimeWorkRequest WorkRequest { get; set; } = null!;
}
public sealed class OvertimeWorkRequestMapping : IEntityTypeConfiguration<OvertimeWorkRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeWorkRequest> b)
    {
        b.ToTable("OT_WorkRequests"); b.HasKey(x => x.RequestId); b.Property(x => x.RequestId).ValueGeneratedNever();
        b.Property(x => x.Kind).HasMaxLength(20); b.Property(x => x.Reason).HasMaxLength(1000);
        b.Property(x => x.FromDate).HasColumnType("date"); b.Property(x => x.ToDate).HasColumnType("date");
        b.HasOne(x => x.Request).WithOne().HasForeignKey<OvertimeWorkRequest>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.StartLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.EndLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrganisationId, x.IndividualId, x.FromDate });
    }
}
public sealed class OvertimeWorkTaskMapping : IEntityTypeConfiguration<OvertimeWorkTask>
{
    public void Configure(EntityTypeBuilder<OvertimeWorkTask> b)
    {
        b.ToTable("OT_WorkTasks"); b.HasKey(x => x.Id); b.Property(x => x.Description).HasMaxLength(500);
        b.HasOne(x => x.WorkRequest).WithMany(x => x.Tasks).HasForeignKey(x => x.WorkRequestId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<OvertimeWorkRequest> OvertimeWorkRequests => Set<OvertimeWorkRequest>();
    public DbSet<OvertimeWorkTask> OvertimeWorkTasks => Set<OvertimeWorkTask>();
}
