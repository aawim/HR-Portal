using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class PayStudyFieldMapping:IEntityTypeConfiguration<PayStudyField>
{
    public void Configure(EntityTypeBuilder<PayStudyField> b)
    {
        b.ToTable("PF_StudyFieldSuggestions");b.HasKey(x=>x.Id);b.Property(x=>x.Name).HasMaxLength(200);b.Property(x=>x.Source).HasMaxLength(150);
        b.HasIndex(x=>new{x.JobFamilyId,x.Name}).IsUnique();b.HasOne<PayJobFamily>().WithMany().HasForeignKey(x=>x.JobFamilyId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<PayStudyField> PayStudyFields=>Set<PayStudyField>();
}
