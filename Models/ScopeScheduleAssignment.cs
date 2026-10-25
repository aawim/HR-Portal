using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Models.WorkPlanning;
namespace HRM.Models;

public sealed class ScopeScheduleAssignment
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int? OrganisationStructureId { get; set; }
    public int? GroupId { get; set; }
    public int WorkTemplateId { get; set; }
    public string SchedulePurpose { get; set; } = "Official";
    public string ScheduleMode { get; set; } = "Standard";
    public int WorkingDaysMask { get; set; } = 31;
    public DateTime? RotationAnchorDate { get; set; }
    public string? RotationTemplateIds { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
    public WorkTemplate WorkTemplate { get; set; } = null!;
}
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
