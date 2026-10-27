using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
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
