using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class PayFrameworkReferenceMapping:IEntityTypeConfiguration<PayJobFamily>,IEntityTypeConfiguration<PayProfession>,IEntityTypeConfiguration<FrameworkJobMatrix>
{
    public void Configure(EntityTypeBuilder<PayJobFamily> b){b.ToTable("PF_JobFamilies",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobFamilyId);b.Property(x=>x.JobFamilyId).HasColumnName("JobFamilyID");b.Property(x=>x.Name).HasMaxLength(250);}
    public void Configure(EntityTypeBuilder<PayProfession> b)
    {
        b.ToTable("PF_Professions");b.HasKey(x=>x.Id);b.Property(x=>x.Name).HasMaxLength(250);b.Property(x=>x.SourceEdition).HasMaxLength(100);
        b.Property(x=>x.Tasks).HasColumnType("nvarchar(max)");b.Property(x=>x.ExampleTitlesJson).HasColumnType("nvarchar(max)");b.Property(x=>x.MisscoCodesJson).HasColumnType("nvarchar(max)");
        b.HasIndex(x=>new{x.JobFamilyId,x.Name,x.SourceEdition}).IsUnique();b.HasOne<PayJobFamily>().WithMany().HasForeignKey(x=>x.JobFamilyId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<FrameworkJobMatrix> b){b.Property(x=>x.JobFamilyId).HasColumnName("JobFamilyID");b.Property(x=>x.ProfessionId).HasColumnName("ProfessionID");}
}
public partial class HrmTeContext
{
    public DbSet<PayJobFamily> PayJobFamilies=>Set<PayJobFamily>();
    public DbSet<PayProfession> PayProfessions=>Set<PayProfession>();
}
