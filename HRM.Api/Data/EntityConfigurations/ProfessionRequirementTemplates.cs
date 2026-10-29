using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class ProfessionRequirementTemplateMapping:IEntityTypeConfiguration<ProfessionRequirementTemplate>
{
    public void Configure(EntityTypeBuilder<ProfessionRequirementTemplate> b)
    {
        b.ToTable("PF_ProfessionRequirementTemplates",t=>t.HasCheckConstraint("CK_PF_ProfessionRequirements_Profile","ISJSON([ProfileJson])=1 AND [PublicServiceRank]>0"));b.HasKey(x=>x.Id);b.Property(x=>x.ProfileJson).HasColumnType("nvarchar(max)");b.Property(x=>x.SourceReference).HasMaxLength(1000);b.HasIndex(x=>new{x.ProfessionId,x.PublicServiceRank}).IsUnique();b.HasOne<PayProfession>().WithMany().HasForeignKey(x=>x.ProfessionId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<ProfessionRequirementTemplate> ProfessionRequirementTemplates=>Set<ProfessionRequirementTemplate>();
}
