using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class PayExperienceAreaMapping:IEntityTypeConfiguration<PayExperienceArea>
{
    public void Configure(EntityTypeBuilder<PayExperienceArea> b)
    {
        b.ToTable("PF_ExperienceAreaSuggestions");b.HasKey(x=>x.Id);b.Property(x=>x.Sector).HasMaxLength(150);b.Property(x=>x.Name).HasMaxLength(200);b.Property(x=>x.Description).HasMaxLength(2000);b.Property(x=>x.Source).HasMaxLength(150);
        b.HasIndex(x=>new{x.JobFamilyId,x.Sector,x.Name}).IsUnique().HasFilter(null);b.HasOne<PayJobFamily>().WithMany().HasForeignKey(x=>x.JobFamilyId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<PayExperienceArea> PayExperienceAreas=>Set<PayExperienceArea>();
}
