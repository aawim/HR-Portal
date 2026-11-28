using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class TeamScheduleAdjustmentMapping : IEntityTypeConfiguration<TeamScheduleAdjustment>
{
    public void Configure(EntityTypeBuilder<TeamScheduleAdjustment> b)
    {
        b.ToTable("TM_TeamScheduleAdjustments", t => t.HasCheckConstraint("CK_TM_TeamScheduleAdjustments_Time",
            "[RevisedCheckIn] IS NOT NULL OR [RevisedCheckOut] IS NOT NULL"));
        b.HasKey(x => x.Id);
        b.Property(x => x.WorkDate).HasColumnType("date");
        b.Property(x => x.RevisedCheckIn).HasColumnType("time");
        b.Property(x => x.RevisedCheckOut).HasColumnType("time");
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.TeamId, x.WorkDate, x.IsActive });
        b.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<TeamScheduleAdjustment> TeamScheduleAdjustments => Set<TeamScheduleAdjustment>();
}
