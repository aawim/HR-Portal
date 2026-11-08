using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Models.Archives;
namespace HRM.Models;
public sealed class JobTypeLeaveSetMapping:IEntityTypeConfiguration<JobTypeLeaveSet>
{
    public void Configure(EntityTypeBuilder<JobTypeLeaveSet> b)
    {
        b.ToTable("LS_JobTypeLeaveSets");b.HasKey(x=>x.Id);
        b.HasIndex(x=>new{x.OrganisationId,x.JobTypeId,x.LeaveSetId}).IsUnique();
        b.Property(x=>x.OperationLogId).IsConcurrencyToken();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JobType>().WithMany().HasForeignKey(x=>x.JobTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LeaveSet>().WithMany().HasForeignKey(x=>x.LeaveSetId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x=>x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext {public DbSet<JobTypeLeaveSet> JobTypeLeaveSets=>Set<JobTypeLeaveSet>();}
