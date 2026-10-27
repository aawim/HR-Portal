using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class OvertimePayrollBatchMapping:IEntityTypeConfiguration<OvertimePayrollBatch>
{
    public void Configure(EntityTypeBuilder<OvertimePayrollBatch> b)
    {
        b.ToTable("OT_PayrollBatches");b.HasKey(x=>x.Id);b.Property(x=>x.PayrollMonth).HasColumnType("date");b.Property(x=>x.Reference).HasMaxLength(200);
        b.HasIndex(x=>new{x.OrganisationId,x.ClientBatchId}).IsUnique();b.HasIndex(x=>new{x.OrganisationId,x.PayrollMonth});
        b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x=>x.ProcessedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x=>x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OvertimePayrollItemMapping:IEntityTypeConfiguration<OvertimePayrollItem>
{
    public void Configure(EntityTypeBuilder<OvertimePayrollItem> b)
    {
        b.ToTable("OT_PayrollItems");b.HasKey(x=>x.RequestId);b.Property(x=>x.RequestId).ValueGeneratedNever();b.Property(x=>x.WorkDate).HasColumnType("date");b.Property(x=>x.RequestReference).HasMaxLength(200);
        b.HasOne<OvertimeRequest>().WithOne().HasForeignKey<OvertimePayrollItem>(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.Batch).WithMany(x=>x.Items).HasForeignKey(x=>x.BatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<OvertimePayrollBatch> OvertimePayrollBatches=>Set<OvertimePayrollBatch>();
    public DbSet<OvertimePayrollItem> OvertimePayrollItems=>Set<OvertimePayrollItem>();
}
