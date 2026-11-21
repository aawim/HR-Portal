using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using HRM.ActivityLogs.Persistence;
using HRM.ActivityLogs.Processing;
using HRM.ActivityLogs.Security;
using HRM.ActivityLogs.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class ActivityLogsTests
{
    private sealed class TestActivityDbContextFactory(DbContextOptions<ActivityDbContext> options)
        : IDbContextFactory<ActivityDbContext>
    {
        public ActivityDbContext CreateDbContext() => new(options);
    }

    private sealed class StubActivityContextAccessor(ActivityActorContext context) : IActivityContextAccessor
    {
        public Task<ActivityActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(context);
    }

    private sealed class FixedClock(DateTime utcNow) : IActivityClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }

    private static (TestActivityDbContextFactory Factory, ActivityQueueChannel Queue, ActivityLogService Service, ActivityLogQueryService Query, FixedClock Clock) CreateHarness(
        ActivityActorContext? actor = null,
        ActivityLogOptions? options = null,
        DateTime? utcNow = null)
    {
        var dbName = $"ActivityLogsTest_{Guid.NewGuid():N}";
        var dbOptions = new DbContextOptionsBuilder<ActivityDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var factory = new TestActivityDbContextFactory(dbOptions);
        var opts = options ?? new ActivityLogOptions
        {
            EnableAsyncQueue = false,
            DefaultRetentionDays = 90
        };
        var optionsWrapper = Options.Create(opts);
        var clock = new FixedClock(utcNow ?? new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc));
        var accessor = new StubActivityContextAccessor(actor ?? new ActivityActorContext
        {
            TenantId = 10,
            OrganisationId = 10,
            DivisionId = 100,
            TeamId = 500,
            UserId = 42,
            ActorType = ActivityActorTypes.User,
            CorrelationId = "corr-123",
            OpaqueSessionId = ActivitySanitizer.ToOpaqueSessionId("raw-session-secret"),
            Source = "HRM.Api",
            CanViewTeamActivity = true,
            CanViewOrganisationActivity = true,
            CanExportActivity = true,
            AuthorizedOrganisationIds = [10],
            AuthorizedDivisionIds = [100],
            AuthorizedTeamIds = [500]
        });

        var queue = new ActivityQueueChannel(optionsWrapper);
        var service = new ActivityLogService(
            factory,
            accessor,
            clock,
            queue,
            optionsWrapper,
            NullLogger<ActivityLogService>.Instance);

        var query = new ActivityLogQueryService(
            factory,
            service,
            optionsWrapper);

        return (factory, queue, service, query, clock);
    }

    [Fact]
    public async Task RecordAsync_PersistsActivityWithServerDerivedActorContextAndSanitizedMetadata()
    {
        var (factory, _, service, _, _) = CreateHarness();

        await service.RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Payroll,
            ActivityType = ActivityTypes.PayrollGenerationStarted,
            Module = "Payroll",
            Action = "INITIATE",
            Description = "Initiated monthly payroll run.",
            TargetEntityType = "PayrollRun",
            TargetEntityId = "77",
            TargetDisplayName = "November 2026\r\nPayroll",
            Metadata = new Dictionary<string, object?>
            {
                ["runType"] = "Regular",
                ["salary"] = 55000m,
                ["accessToken"] = "secret-jwt-value",
                ["period"] = "2026-11"
            }
        });

        await using var db = factory.CreateDbContext();
        var log = await db.ActivityLogs.SingleAsync();
        Assert.Equal(10, log.TenantId);
        Assert.Equal(10, log.OrganisationId);
        Assert.Equal(100, log.DivisionId);
        Assert.Equal(42, log.UserId);
        Assert.Equal("Payroll", log.ActivityCategory);
        Assert.Equal(ActivityTypes.PayrollGenerationStarted, log.ActivityType);
        Assert.Equal("November 2026 Payroll", log.TargetDisplayName);
        Assert.NotNull(log.MetadataJson);
        Assert.Contains("Regular", log.MetadataJson);
        Assert.DoesNotContain("55000", log.MetadataJson);
        Assert.DoesNotContain("secret-jwt-value", log.MetadataJson);
        Assert.NotNull(log.SessionId);
        Assert.DoesNotContain("raw-session-secret", log.SessionId);
    }

    [Fact]
    public async Task RecordAsync_IsIdempotentByEventId()
    {
        var (factory, _, service, _, _) = CreateHarness();
        var eventId = Guid.NewGuid();

        await service.RecordAsync(new ActivityLogRequest
        {
            EventId = eventId,
            Category = ActivityCategory.Transfers,
            ActivityType = ActivityTypes.EmployeeTransferSubmitted,
            Module = "Transfers",
            Action = "SUBMIT",
            Description = "Submitted transfer request."
        });

        await service.RecordAsync(new ActivityLogRequest
        {
            EventId = eventId,
            Category = ActivityCategory.Transfers,
            ActivityType = ActivityTypes.EmployeeTransferSubmitted,
            Module = "Transfers",
            Action = "SUBMIT",
            Description = "Duplicate delivery should be ignored."
        });

        await using var db = factory.CreateDbContext();
        Assert.Equal(1, await db.ActivityLogs.CountAsync());
    }

    [Fact]
    public async Task QueryService_EnforcesStrictOrganisationDivisionAndTeamIsolation()
    {
        var (factory, _, _, query, _) = CreateHarness();
        await using (var db = factory.CreateDbContext())
        {
            db.ActivityLogs.AddRange(
                new ActivityLog
                {
                    EventId = Guid.NewGuid(),
                    TenantId = 10,
                    OrganisationId = 10,
                    DivisionId = 100,
                    TeamId = 500,
                    UserId = 42,
                    ActorType = ActivityActorTypes.User,
                    ActivityCategory = "Attendance",
                    ActivityType = ActivityTypes.AttendanceRequestSubmitted,
                    Module = "Attendance",
                    Action = "SUBMIT",
                    Description = "Org 10 Div 100 Team 500",
                    OccurredAtUtc = DateTime.UtcNow.AddMinutes(-5),
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
                },
                new ActivityLog
                {
                    EventId = Guid.NewGuid(),
                    TenantId = 20,
                    OrganisationId = 20,
                    DivisionId = 200,
                    TeamId = 600,
                    UserId = 99,
                    ActorType = ActivityActorTypes.User,
                    ActivityCategory = "Attendance",
                    ActivityType = ActivityTypes.AttendanceRequestSubmitted,
                    Module = "Attendance",
                    Action = "SUBMIT",
                    Description = "Org 20 confidential activity",
                    OccurredAtUtc = DateTime.UtcNow.AddMinutes(-2),
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
                });
            await db.SaveChangesAsync();
        }

        var org10Admin = new ActivityActorContext
        {
            TenantId = 10,
            OrganisationId = 10,
            UserId = 42,
            CanViewOrganisationActivity = true,
            AuthorizedOrganisationIds = [10]
        };

        // Cross-org query must be rejected for non-SuperAdmin
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.GetOrganisationActivitiesAsync(20, new ActivityLogFilter(), org10Admin));

        // Scoped org search returns only Org 10 records
        var org10Page = await query.SearchAsync(new ActivityLogFilter(), org10Admin);
        Assert.Single(org10Page.Items);
        Assert.Equal(10, org10Page.Items[0].OrganisationId);

        // Unauthorized team query must be rejected
        var teamSupervisor = new ActivityActorContext
        {
            TenantId = 10,
            OrganisationId = 10,
            UserId = 55,
            CanViewTeamActivity = true,
            AuthorizedOrganisationIds = [10],
            AuthorizedTeamIds = [500]
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.GetTeamActivitiesAsync(999, new ActivityLogFilter(), teamSupervisor));

        var team500Page = await query.GetTeamActivitiesAsync(500, new ActivityLogFilter(), teamSupervisor);
        Assert.Single(team500Page.Items);
    }

    [Fact]
    public async Task QueryService_IndividualUserSeesOnlyOwnActivities()
    {
        var (factory, _, _, query, _) = CreateHarness();
        await using (var db = factory.CreateDbContext())
        {
            db.ActivityLogs.AddRange(
                new ActivityLog
                {
                    EventId = Guid.NewGuid(),
                    TenantId = 10,
                    OrganisationId = 10,
                    UserId = 42,
                    ActorType = ActivityActorTypes.User,
                    ActivityCategory = "Leave",
                    ActivityType = ActivityTypes.LeaveRequestSubmitted,
                    Module = "Leave",
                    Action = "SUBMIT",
                    Description = "Own leave request",
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new ActivityLog
                {
                    EventId = Guid.NewGuid(),
                    TenantId = 10,
                    OrganisationId = 10,
                    UserId = 77,
                    ActorType = ActivityActorTypes.User,
                    ActivityCategory = "Leave",
                    ActivityType = ActivityTypes.LeaveRequestSubmitted,
                    Module = "Leave",
                    Action = "SUBMIT",
                    Description = "Colleague leave request",
                    OccurredAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow
                });
            await db.SaveChangesAsync();
        }

        var employee = new ActivityActorContext
        {
            TenantId = 10,
            OrganisationId = 10,
            UserId = 42,
            AuthorizedOrganisationIds = [10]
        };

        var mine = await query.GetMyRecentAsync(new ActivityLogFilter(), employee);
        Assert.Single(mine.Items);
        Assert.Equal(42, mine.Items[0].UserId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.GetEmployeeTimelineAsync(77, new ActivityLogFilter(), employee));
    }

    [Fact]
    public async Task OutboxAndRetentionProcessors_DeliverOutboxAndPurgeExpiredRecords()
    {
        var now = new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
        var opts = new ActivityLogOptions
        {
            EnableAsyncQueue = true,
            DefaultRetentionDays = 90,
            CategoryRetentionDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Navigation"] = 7
            }
        };

        var (factory, queue, service, _, clock) = CreateHarness(options: opts, utcNow: now);

        // Enqueue via transactional outbox
        await service.RecordInOutboxAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Workflow,
            ActivityType = ActivityTypes.WorkflowApproved,
            Module = "Workflow",
            Action = "APPROVE",
            Description = "Approved transfer workflow."
        });

        // Drain outbox via processor
        var processor = new ActivityEventProcessor(
            queue,
            factory,
            new IAuditlessClockAdapter(clock),
            Options.Create(opts),
            NullLogger<ActivityEventProcessor>.Instance);

        var processed = await processor.ProcessPendingOutboxBatchAsync(CancellationToken.None);
        Assert.Equal(1, processed);

        await using (var db = factory.CreateDbContext())
        {
            Assert.Equal(1, await db.ActivityLogs.CountAsync());
            var outbox = await db.ActivityOutbox.SingleAsync();
            Assert.NotNull(outbox.ProcessedAtUtc);

            // Seed expired Navigation record (10 days old > 7-day retention)
            db.ActivityLogs.Add(new ActivityLog
            {
                EventId = Guid.NewGuid(),
                TenantId = 10,
                OrganisationId = 10,
                UserId = 42,
                ActorType = ActivityActorTypes.User,
                ActivityCategory = "Navigation",
                ActivityType = ActivityTypes.UserSwitchedOrganisation,
                Module = "Navigation",
                Action = "SWITCH",
                Description = "Old navigation event",
                OccurredAtUtc = now.AddDays(-10),
                CreatedAtUtc = now.AddDays(-10)
            });
            await db.SaveChangesAsync();
        }

        var retention = new ActivityRetentionProcessor(
            factory,
            clock,
            Options.Create(opts),
            NullLogger<ActivityRetentionProcessor>.Instance);

        var deleted = await retention.RunCleanupOnceAsync(CancellationToken.None);
        Assert.Equal(1, deleted);

        await using (var db = factory.CreateDbContext())
        {
            var remaining = await db.ActivityLogs.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal("Workflow", remaining[0].ActivityCategory);
        }
    }

    [Fact]
    public async Task ExportCsvAsync_NeutralizesFormulaInjectionAndLogsExportActivity()
    {
        var (factory, _, _, query, _) = CreateHarness();
        await using (var db = factory.CreateDbContext())
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                EventId = Guid.NewGuid(),
                TenantId = 10,
                OrganisationId = 10,
                UserId = 42,
                ActorType = ActivityActorTypes.User,
                ActivityCategory = "Communication",
                ActivityType = ActivityTypes.AnnouncementPublished,
                Module = "Communication",
                Action = "PUBLISH",
                Description = "=HYPERLINK(\"http://evil\")",
                OccurredAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var admin = new ActivityActorContext
        {
            TenantId = 10,
            OrganisationId = 10,
            UserId = 42,
            CanViewOrganisationActivity = true,
            CanExportActivity = true,
            AuthorizedOrganisationIds = [10]
        };

        var export = await query.ExportCsvAsync(new ActivityLogFilter(), admin);
        var csv = System.Text.Encoding.UTF8.GetString(export.Content);
        Assert.Contains("'=HYPERLINK", csv);

        // Verify export itself was recorded as an activity
        await using var verifyDb = factory.CreateDbContext();
        Assert.True(await verifyDb.ActivityLogs.AnyAsync(a => a.ActivityType == ActivityTypes.ActivityExported));
    }

    [Fact]
    public void ActivityDbContext_ModelMapsToDedicatedActivitySchema()
    {
        var options = new DbContextOptionsBuilder<ActivityDbContext>()
            .UseSqlServer("Server=.;Database=HRMActivityLogs;Trusted_Connection=True;")
            .Options;

        using var db = new ActivityDbContext(options);
        var activityEntity = db.Model.FindEntityType(typeof(ActivityLog));
        var outboxEntity = db.Model.FindEntityType(typeof(ActivityOutboxMessage));

        Assert.NotNull(activityEntity);
        Assert.Equal("activity", activityEntity!.GetSchema());
        Assert.Equal("ActivityLogs", activityEntity.GetTableName());
        Assert.NotNull(outboxEntity);
        Assert.Equal("activity", outboxEntity!.GetSchema());
        Assert.Equal("ActivityOutbox", outboxEntity.GetTableName());
    }
}
