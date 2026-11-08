using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class OrganisationWorkingHourRequirementMapping : IEntityTypeConfiguration<OrganisationWorkingHourRequirement>
{
    public void Configure(EntityTypeBuilder<OrganisationWorkingHourRequirement> b)
    {
        b.ToTable("WH_OrganisationWorkingHourRequirements"); b.HasKey(x => x.Id);
        b.Property(x => x.FromDate).HasColumnType("date"); b.Property(x => x.ToDate).HasColumnType("date");
        b.Property(x => x.Reason).HasMaxLength(1000); b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.JobTypeId, x.FromDate }).IsUnique().HasFilter(null);
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JobType>().WithMany().HasForeignKey(x => x.JobTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}


public sealed class WeeklyHourExceptionRequestMapping : IEntityTypeConfiguration<WeeklyHourExceptionRequest>
{
    public void Configure(EntityTypeBuilder<WeeklyHourExceptionRequest> b)
    {
        b.ToTable("WH_WeeklyHourExceptionRequests"); b.HasKey(x => x.RequestId);
        b.Property(x => x.RequestId).ValueGeneratedNever();
        b.Property(x => x.WeekStart).HasColumnType("date"); b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasOne(x => x.Request).WithOne().HasForeignKey<WeeklyHourExceptionRequest>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrganisationId, x.IndividualId, x.WeekStart });
    }
}


public sealed class JobTypeWorkingHourRuleMapping : IEntityTypeConfiguration<JobTypeWorkingHourRule>
{
    public void Configure(EntityTypeBuilder<JobTypeWorkingHourRule> b)
    {
        b.ToTable("WH_JobTypeWorkingHourRules"); b.HasKey(x => x.Id);
        b.Property(x => x.EffectiveFrom).HasColumnType("date");
        b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.JobTypeId, x.EffectiveFrom }).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JobType>().WithMany().HasForeignKey(x => x.JobTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}


public sealed class OrganisationScheduleAmendmentMapping : IEntityTypeConfiguration<OrganisationScheduleAmendment>
{
    public void Configure(EntityTypeBuilder<OrganisationScheduleAmendment> b)
    {
        b.ToTable("SA_OrganisationScheduleAmendments"); b.HasKey(x => x.Id);
        b.Property(x => x.WorkDate).HasColumnType("date");
        b.Property(x => x.OriginalStart).HasColumnType("time");
        b.Property(x => x.RevisedStart).HasColumnType("time");
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.WorkDate, x.OriginalStart }).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}


public partial class HrmTeContext
{
    public DbSet<JobTypeWorkingHourRule> JobTypeWorkingHourRules => Set<JobTypeWorkingHourRule>();
    public DbSet<OrganisationScheduleAmendment> OrganisationScheduleAmendments => Set<OrganisationScheduleAmendment>();
    public DbSet<WeeklyHourExceptionRequest> WeeklyHourExceptionRequests => Set<WeeklyHourExceptionRequest>();
    public DbSet<OrganisationWorkingHourRequirement> OrganisationWorkingHourRequirements => Set<OrganisationWorkingHourRequirement>();
}
