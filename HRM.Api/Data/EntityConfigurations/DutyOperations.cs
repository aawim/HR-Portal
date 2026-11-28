using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class DutyOperationMapping : IEntityTypeConfiguration<DutyOperation>
{
    public void Configure(EntityTypeBuilder<DutyOperation> b)
    {
        b.ToTable("WS_DutyOperations"); b.HasKey(x => x.Id); b.Property(x => x.OperationType).HasMaxLength(40); b.Property(x => x.Status).HasMaxLength(30); b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasIndex(x => new { x.DutyAssignmentId, x.Status }); b.HasOne(x => x.DutyAssignment).WithMany().HasForeignKey(x => x.DutyAssignmentId).OnDelete(DeleteBehavior.Restrict); b.HasOne<DutyAssignment>().WithMany().HasForeignKey(x => x.ReplacementDutyAssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict); b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext { public DbSet<DutyOperation> DutyOperations => Set<DutyOperation>(); }
