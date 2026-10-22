using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class LateCheckInRequest
{
    public int RequestId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AttendanceLogId { get; set; }
    public string Kind { get; set; } = "LateCheckIn";
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RequestedStart { get; set; }
    public DateTime? RequestedEnd { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
    public long WorkAssignmentId { get; set; }
    public string ScheduleName { get; set; } = "";
    public DateTime ScheduledStart { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public int LateMinutes { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
}
public sealed class LateCheckInRequestMapping : IEntityTypeConfiguration<LateCheckInRequest>
{
    public void Configure(EntityTypeBuilder<LateCheckInRequest> b)
    {
        b.ToTable("LC_LateCheckInRequests"); b.HasKey(x => x.RequestId);
        b.Property(x => x.RequestId).ValueGeneratedNever();
        b.Property(x => x.Kind).HasMaxLength(20);
        b.Property(x => x.ScheduleName).HasMaxLength(250); b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasOne(x => x.Request).WithOne().HasForeignKey<LateCheckInRequest>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.AttendanceLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrganisationId, x.IndividualId });
        b.HasIndex(x => new { x.AttendanceLogId, x.WorkAssignmentId });
    }
}
public partial class HrmTeContext
{
    public DbSet<LateCheckInRequest> LateCheckInRequests => Set<LateCheckInRequest>();
}
