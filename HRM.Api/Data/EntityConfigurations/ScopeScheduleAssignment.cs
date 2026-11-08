using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Models.WorkPlanning;
namespace HRM.Models;
public sealed class ScopeScheduleAssignmentMapping : IEntityTypeConfiguration<ScopeScheduleAssignment>
{
    public void Configure(EntityTypeBuilder<ScopeScheduleAssignment> b)
    {
        b.ToTable("WP_ScopeScheduleAssignments"); b.HasKey(x=>x.Id);
        b.Property(x=>x.SchedulePurpose).HasMaxLength(20); b.Property(x=>x.ScheduleMode).HasMaxLength(20);
        b.Property(x=>x.RotationTemplateIds).HasMaxLength(2000);
        b.Property(x=>x.EffectiveFrom).HasColumnType("date"); b.Property(x=>x.EffectiveTo).HasColumnType("date");
        b.Property(x=>x.RotationAnchorDate).HasColumnType("date"); b.Property(x=>x.Version).IsConcurrencyToken();
        b.HasIndex(x=>new {x.OrganisationId,x.EffectiveFrom,x.EffectiveTo});
        b.HasOne(x=>x.WorkTemplate).WithMany().HasForeignKey(x=>x.WorkTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OrganisationStructure>().WithMany().HasForeignKey(x=>x.OrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Group>().WithMany().HasForeignKey(x=>x.GroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x=>x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext { public DbSet<ScopeScheduleAssignment> ScopeScheduleAssignments => Set<ScopeScheduleAssignment>(); }
