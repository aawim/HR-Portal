using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class OvertimeRequestMapping:IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> b)
    {
        b.ToTable("OT_OvertimeRequests",t=>t.HasCheckConstraint("CK_OT_OvertimeRequests_Minutes","[Minutes] > 0 AND [Minutes] <= 1440"));b.HasKey(x=>x.RequestId);b.Property(x=>x.RequestId).ValueGeneratedNever();
        b.Property(x=>x.WorkDate).HasColumnType("date");b.Property(x=>x.WeekStart).HasColumnType("date");b.Property(x=>x.Reason).HasMaxLength(1000);b.Property(x=>x.ApprovedPolicyJson).HasMaxLength(2000);b.Property(x=>x.PaymentReference).HasMaxLength(200);
        b.HasOne(x=>x.Request).WithOne().HasForeignKey<OvertimeRequest>(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);b.HasOne<User>().WithMany().HasForeignKey(x=>x.PaidByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.ClaimSource).HasMaxLength(20).HasDefaultValue("Legacy");
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.StartLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AttendanceLog>().WithMany().HasForeignKey(x => x.EndLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OvertimeWorkTask>().WithMany().HasForeignKey(x => x.PreapprovalTaskId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.OrganisationId,x.IndividualId,x.WorkDate});
    }
}

public partial class HrmTeContext{public DbSet<OvertimeRequest> OvertimeRequests=>Set<OvertimeRequest>();}
