using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class WorkforceAttendanceSessionMapping:IEntityTypeConfiguration<WorkforceAttendanceSession>{public void Configure(EntityTypeBuilder<WorkforceAttendanceSession>b){b.ToTable("WS_AttendanceSessions");b.HasKey(x=>x.Id);b.Property(x=>x.Status).HasMaxLength(30);b.Property(x=>x.EvaluationStrategy).HasMaxLength(50);b.Property(x=>x.MatchReason).HasMaxLength(1000);b.HasIndex(x=>new{x.WorkPlanSegmentId,x.EvaluationVersion}).IsUnique();b.HasOne<WorkPlanSegment>().WithMany().HasForeignKey(x=>x.WorkPlanSegmentId).OnDelete(DeleteBehavior.Restrict);b.HasOne<DutyAssignment>().WithMany().HasForeignKey(x=>x.DutyAssignmentId).OnDelete(DeleteBehavior.Restrict);b.HasOne<OperationLog>().WithMany().HasForeignKey(x=>x.OperationLogId).OnDelete(DeleteBehavior.Restrict);}}
public partial class HrmTeContext{public DbSet<WorkforceAttendanceSession> WorkforceAttendanceSessions=>Set<WorkforceAttendanceSession>();}
