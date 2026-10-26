using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class OvertimeRequest
{
    public int RequestId {get;set;}public int OrganisationId {get;set;}public int IndividualId {get;set;}public int JobId {get;set;}
    public DateTime WorkDate {get;set;}public DateTime WeekStart {get;set;}public int Minutes {get;set;}
    public string Reason {get;set;}="";public string? ApprovedPolicyJson {get;set;}
    public DateTime CreatedAtUtc {get;set;}public DateTime? PaidAtUtc {get;set;}public int? PaidByUserId {get;set;}public string? PaymentReference {get;set;}
    public Request Request {get;set;}=null!;
}
public sealed class OvertimeRequestMapping:IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> b)
    {
        b.ToTable("OT_OvertimeRequests",t=>t.HasCheckConstraint("CK_OT_OvertimeRequests_Minutes","[Minutes] > 0 AND [Minutes] <= 1440"));b.HasKey(x=>x.RequestId);b.Property(x=>x.RequestId).ValueGeneratedNever();
        b.Property(x=>x.WorkDate).HasColumnType("date");b.Property(x=>x.WeekStart).HasColumnType("date");b.Property(x=>x.Reason).HasMaxLength(1000);b.Property(x=>x.ApprovedPolicyJson).HasMaxLength(2000);b.Property(x=>x.PaymentReference).HasMaxLength(200);
        b.HasOne(x=>x.Request).WithOne().HasForeignKey<OvertimeRequest>(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);b.HasOne<User>().WithMany().HasForeignKey(x=>x.PaidByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.OrganisationId,x.IndividualId,x.WorkDate});
    }
}
public partial class HrmTeContext{public DbSet<OvertimeRequest> OvertimeRequests=>Set<OvertimeRequest>();}
