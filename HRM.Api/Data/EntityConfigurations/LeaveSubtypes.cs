using HRM.Models.LeaveTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class LeaveSubtypeMapping:IEntityTypeConfiguration<LeaveSubtype>,IEntityTypeConfiguration<LeaveSubtypeUsage>
{
    public void Configure(EntityTypeBuilder<LeaveSubtype> b)
    {
        b.ToTable("LT_LeaveSubtypes",t=>t.HasCheckConstraint("CK_LT_LeaveSubtypes_Quota","[Days]>0 AND [CertificateRule] IN ('Any','Required','Without')"));
        b.HasKey(x=>x.Id);b.Property(x=>x.Name).HasMaxLength(150);b.Property(x=>x.CertificateRule).HasMaxLength(16);b.Property(x=>x.Days).HasPrecision(18,2);
        b.HasIndex(x=>new{x.OrganisationId,x.LeaveTypeId,x.Name}).IsUnique();
        b.HasOne<LeaveType>().WithMany().HasForeignKey(x=>x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<LeaveSubtypeUsage> b)
    {
        b.ToTable("LT_LeaveSubtypeUsages");b.HasKey(x=>x.LeaveId);
        b.Property(x=>x.SubtypeName).HasMaxLength(150);b.Property(x=>x.CertificateName).HasMaxLength(180);b.Property(x=>x.ContentType).HasMaxLength(50);
        b.Property(x=>x.DaysLimit).HasPrecision(18,2);b.Property(x=>x.PeriodStart).HasColumnType("date");
        b.HasOne(x=>x.Leave).WithOne().HasForeignKey<LeaveSubtypeUsage>(x=>x.LeaveId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x=>x.Subtype).WithMany().HasForeignKey(x=>x.SubtypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JobLeaveType>().WithMany().HasForeignKey(x=>x.JobLeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.JobLeaveTypeId,x.SubtypeId,x.PeriodStart});
    }
}
public partial class HrmTeContext
{
    public DbSet<LeaveSubtype> LeaveSubtypes=>Set<LeaveSubtype>();
    public DbSet<LeaveSubtypeUsage> LeaveSubtypeUsages=>Set<LeaveSubtypeUsage>();
}
