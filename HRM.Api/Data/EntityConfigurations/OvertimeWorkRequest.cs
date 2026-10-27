using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
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
