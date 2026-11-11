using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<OrganisationEvent> OrganisationEvents => Set<OrganisationEvent>();
    public DbSet<OrganisationEventAudience> OrganisationEventAudiences => Set<OrganisationEventAudience>();
    public DbSet<OrganisationEventRecurrence> OrganisationEventRecurrences => Set<OrganisationEventRecurrence>();
    public DbSet<OrganisationEventOccurrenceException> OrganisationEventOccurrenceExceptions => Set<OrganisationEventOccurrenceException>();
    public DbSet<OrganisationCalendarException> OrganisationCalendarExceptions => Set<OrganisationCalendarException>();
    public DbSet<OrganisationCalendarExceptionSegment> OrganisationCalendarExceptionSegments => Set<OrganisationCalendarExceptionSegment>();
    public DbSet<CalendarImpactAssessment> CalendarImpactAssessments => Set<CalendarImpactAssessment>();
    public DbSet<OrganisationEventNotification> OrganisationEventNotifications => Set<OrganisationEventNotification>();
    public DbSet<OrganisationEventAudit> OrganisationEventAudits => Set<OrganisationEventAudit>();
    public DbSet<SupervisorCalendarTask> SupervisorCalendarTasks => Set<SupervisorCalendarTask>();
    public DbSet<SupervisorCalendarTaskAssignee> SupervisorCalendarTaskAssignees => Set<SupervisorCalendarTaskAssignee>();
}

public sealed class OrganisationEventMapping : IEntityTypeConfiguration<OrganisationEvent>
{
    public void Configure(EntityTypeBuilder<OrganisationEvent> b)
    {
        b.ToTable("OC_OrganisationEvents"); b.HasKey(x => x.OrganisationEventId);
        b.Property(x => x.EventCode).HasMaxLength(40);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Category).HasMaxLength(40).IsRequired();
        b.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
        b.Property(x => x.VenueName).HasMaxLength(200);
        b.Property(x => x.VenueAddress).HasMaxLength(400);
        b.Property(x => x.MeetingUrl).HasMaxLength(500);
        b.Property(x => x.ContactInformation).HasMaxLength(300);
        b.Property(x => x.DressCode).HasMaxLength(200);
        b.Property(x => x.AttendanceRequirement).HasMaxLength(24).IsRequired();
        b.Property(x => x.CalendarImpactType).HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.Visibility).HasMaxLength(20).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.OrganisationBusinessEntityId, x.Status, x.StartDateTime });
        b.HasMany(x => x.Audiences).WithOne().HasForeignKey(x => x.OrganisationEventId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Recurrence).WithOne().HasForeignKey<OrganisationEventRecurrence>(x => x.OrganisationEventId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrganisationEventAudienceMapping : IEntityTypeConfiguration<OrganisationEventAudience>
{
    public void Configure(EntityTypeBuilder<OrganisationEventAudience> b)
    {
        b.ToTable("OC_OrganisationEventAudiences"); b.HasKey(x => x.OrganisationEventAudienceId);
        b.Property(x => x.AudienceType).HasMaxLength(24).IsRequired();
        b.HasIndex(x => new { x.OrganisationEventId, x.AudienceType, x.TargetEntityId }).IsUnique();
    }
}

public sealed class OrganisationEventRecurrenceMapping : IEntityTypeConfiguration<OrganisationEventRecurrence>
{
    public void Configure(EntityTypeBuilder<OrganisationEventRecurrence> b)
    {
        b.ToTable("OC_OrganisationEventRecurrences"); b.HasKey(x => x.OrganisationEventRecurrenceId);
        b.Property(x => x.RecurrenceType).HasMaxLength(12).IsRequired();
        b.HasIndex(x => x.OrganisationEventId).IsUnique();
        b.HasMany(x => x.Exceptions).WithOne().HasForeignKey(x => x.OrganisationEventRecurrenceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrganisationEventOccurrenceExceptionMapping : IEntityTypeConfiguration<OrganisationEventOccurrenceException>
{
    public void Configure(EntityTypeBuilder<OrganisationEventOccurrenceException> b)
    {
        b.ToTable("OC_OrganisationEventOccurrenceExceptions"); b.HasKey(x => x.OrganisationEventOccurrenceExceptionId);
        b.Property(x => x.OccurrenceDate).HasColumnType("date");
        b.Property(x => x.OverrideVenueName).HasMaxLength(200);
        b.Property(x => x.Reason).HasMaxLength(400);
        b.HasIndex(x => new { x.OrganisationEventRecurrenceId, x.OccurrenceDate }).IsUnique();
    }
}

public sealed class OrganisationCalendarExceptionMapping : IEntityTypeConfiguration<OrganisationCalendarException>
{
    public void Configure(EntityTypeBuilder<OrganisationCalendarException> b)
    {
        b.ToTable("OC_CalendarExceptions"); b.HasKey(x => x.OrganisationCalendarExceptionId);
        b.Property(x => x.EffectiveDate).HasColumnType("date");
        b.Property(x => x.EffectiveEndDate).HasColumnType("date");
        b.Property(x => x.TargetScope).HasMaxLength(16).IsRequired();
        b.Property(x => x.CalendarImpactType).HasMaxLength(32).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(400).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.OrganisationBusinessEntityId, x.EffectiveDate, x.EffectiveEndDate, x.IsActive });
        b.HasOne<OrganisationEvent>().WithMany().HasForeignKey(x => x.OrganisationEventId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Segments).WithOne().HasForeignKey(x => x.OrganisationCalendarExceptionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrganisationCalendarExceptionSegmentMapping : IEntityTypeConfiguration<OrganisationCalendarExceptionSegment>
{
    public void Configure(EntityTypeBuilder<OrganisationCalendarExceptionSegment> b)
    {
        b.ToTable("OC_CalendarExceptionSegments"); b.HasKey(x => x.OrganisationCalendarExceptionSegmentId);
    }
}

public sealed class CalendarImpactAssessmentMapping : IEntityTypeConfiguration<CalendarImpactAssessment>
{
    public void Configure(EntityTypeBuilder<CalendarImpactAssessment> b)
    {
        b.ToTable("OC_CalendarImpactAssessments"); b.HasKey(x => x.CalendarImpactAssessmentId);
        b.Property(x => x.SourceRecordType).HasMaxLength(16).IsRequired();
        b.Property(x => x.AssessmentType).HasMaxLength(32).IsRequired();
        b.Property(x => x.PreviousValue).HasMaxLength(500);
        b.Property(x => x.ProposedValue).HasMaxLength(500);
        b.Property(x => x.ImpactDetails).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        // Idempotency: re-running an assessment never duplicates a record.
        b.HasIndex(x => new { x.OrganisationCalendarExceptionId, x.SourceRecordType, x.SourceRecordId, x.AssessmentType }).IsUnique();
        b.HasOne<OrganisationCalendarException>().WithMany().HasForeignKey(x => x.OrganisationCalendarExceptionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrganisationEventNotificationMapping : IEntityTypeConfiguration<OrganisationEventNotification>
{
    public void Configure(EntityTypeBuilder<OrganisationEventNotification> b)
    {
        b.ToTable("OC_EventNotifications"); b.HasKey(x => x.OrganisationEventNotificationId);
        b.Property(x => x.OccurrenceDate).HasColumnType("date");
        b.Property(x => x.Reason).HasMaxLength(32).IsRequired();
        b.HasIndex(x => new { x.OrganisationEventId, x.OccurrenceDate, x.IndividualId, x.Reason }).IsUnique();
        b.HasOne<OrganisationEvent>().WithMany().HasForeignKey(x => x.OrganisationEventId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SupervisorCalendarTaskMapping : IEntityTypeConfiguration<SupervisorCalendarTask>
{
    public void Configure(EntityTypeBuilder<SupervisorCalendarTask> b)
    {
        b.ToTable("OC_SupervisorTasks"); b.HasKey(x => x.SupervisorCalendarTaskId);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.TaskCategory).HasMaxLength(40).IsRequired();
        b.Property(x => x.Priority).HasMaxLength(10).IsRequired();
        b.Property(x => x.Status).HasMaxLength(16).IsRequired();
        b.Property(x => x.SourceModule).HasMaxLength(40);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.OrganisationBusinessEntityId, x.OwnerIndividualId, x.DueDateTime });
        b.HasMany(x => x.Assignees).WithOne().HasForeignKey(x => x.SupervisorCalendarTaskId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SupervisorCalendarTaskAssigneeMapping : IEntityTypeConfiguration<SupervisorCalendarTaskAssignee>
{
    public void Configure(EntityTypeBuilder<SupervisorCalendarTaskAssignee> b)
    {
        b.ToTable("OC_SupervisorTaskAssignees"); b.HasKey(x => new { x.SupervisorCalendarTaskId, x.IndividualId });
    }
}

public sealed class OrganisationEventAuditMapping : IEntityTypeConfiguration<OrganisationEventAudit>
{
    public void Configure(EntityTypeBuilder<OrganisationEventAudit> b)
    {
        b.ToTable("OC_EventAudits"); b.HasKey(x => x.OrganisationEventAuditId);
        b.Property(x => x.Action).HasMaxLength(32).IsRequired();
        b.Property(x => x.PreviousValue).HasMaxLength(2000);
        b.Property(x => x.NewValue).HasMaxLength(2000);
        b.Property(x => x.Reason).HasMaxLength(400);
        b.HasIndex(x => new { x.OrganisationEventId, x.CreatedAtUtc });
        b.HasOne<OrganisationEvent>().WithMany().HasForeignKey(x => x.OrganisationEventId).OnDelete(DeleteBehavior.Restrict);
    }
}
