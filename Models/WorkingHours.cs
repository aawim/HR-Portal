using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class JobTypeWorkingHourRule
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int JobTypeId { get; set; }
    // Retained for existing records. Runtime requirements come from official templates.
    public int? RequiredWeeklyMinutes { get; set; }
    public int WorkingDaysMask { get; set; } = 31; // Sunday through Thursday.
    public int WeekStartsOn { get; set; }
    public bool CountDutyHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

public sealed class OrganisationScheduleAmendment
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public DateTime WorkDate { get; set; }
    public TimeSpan OriginalStart { get; set; }
    public TimeSpan RevisedStart { get; set; }
    public string Reason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

public sealed class OrganisationWorkingHourRequirement
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int? JobTypeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int RequiredDailyMinutes { get; set; }
    public string Reason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

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

public sealed class WeeklyHourExceptionRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public DateTime WeekStart { get; set; }
    public int ExcusedMinutes { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
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
