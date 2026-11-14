using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class DutyTypeMapping : IEntityTypeConfiguration<DutyType>
{
    public void Configure(EntityTypeBuilder<DutyType> b)
    {
        b.ToTable("WS_DutyTypes"); b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(64); b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Category).HasMaxLength(50); b.Property(x => x.AttendanceStrategy).HasMaxLength(50);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.Code }).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WorkTemplate>().WithMany().HasForeignKey(x => x.WorkTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WorkforcePolicyMapping : IEntityTypeConfiguration<WorkforcePolicy>
{
    public void Configure(EntityTypeBuilder<WorkforcePolicy> b)
    {
        b.ToTable("WS_WorkforcePolicies"); b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(64); b.Property(x => x.Name).HasMaxLength(200); b.Property(x => x.RulesJson).HasMaxLength(8000);
        b.Property(x => x.EffectiveFrom).HasColumnType("date"); b.Property(x => x.EffectiveTo).HasColumnType("date"); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.Code, x.EffectiveFrom }).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WorkforcePolicyAssignmentMapping : IEntityTypeConfiguration<WorkforcePolicyAssignment>
{
    public void Configure(EntityTypeBuilder<WorkforcePolicyAssignment> b)
    {
        b.ToTable("WS_WorkforcePolicyAssignments"); b.HasKey(x => x.Id);
        b.Property(x => x.EffectiveFrom).HasColumnType("date"); b.Property(x => x.EffectiveTo).HasColumnType("date"); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.JobId, x.IndividualId, x.DutyTypeId, x.EffectiveFrom });
        b.HasOne(x => x.WorkforcePolicy).WithMany().HasForeignKey(x => x.WorkforcePolicyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Individual>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DutyType>().WithMany().HasForeignKey(x => x.DutyTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<DutyType> DutyTypes => Set<DutyType>();
    public DbSet<WorkforcePolicy> WorkforcePolicies => Set<WorkforcePolicy>();
    public DbSet<WorkforcePolicyAssignment> WorkforcePolicyAssignments => Set<WorkforcePolicyAssignment>();
}
