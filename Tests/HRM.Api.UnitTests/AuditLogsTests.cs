using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Interceptors;
using HRM.AuditLogs.Models;
using HRM.AuditLogs.Options;
using HRM.AuditLogs.Persistence;
using HRM.AuditLogs.Security;
using HRM.AuditLogs.Services;
using AuditDbContext = HRM.AuditLogs.Persistence.AuditDbContext;
using AuditLog = HRM.AuditLogs.Models.AuditLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class AuditLogsTests
{
    private sealed class TestAuditedEntity
    {
        public int Id { get; set; }
        public int OrganisationId { get; set; }
        public int DivisionId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public string UnchangedNote { get; set; } = "initial";
        public string ConcurrencyToken { get; set; } = "v1";
    }

    private sealed class TestBusinessDbContext(DbContextOptions<TestBusinessDbContext> options) : DbContext(options)
    {
        public DbSet<TestAuditedEntity> Entities => Set<TestAuditedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAuditedEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd();
                b.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
            });
        }
    }

    private sealed class InMemoryAuditDbFactory(string dbName) : IDbContextFactory<HRM.AuditLogs.Persistence.AuditDbContext>
    {
        private readonly DbContextOptions<HRM.AuditLogs.Persistence.AuditDbContext> _options = new DbContextOptionsBuilder<HRM.AuditLogs.Persistence.AuditDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        public HRM.AuditLogs.Persistence.AuditDbContext CreateDbContext() => new(_options);
    }

    private sealed class FixedAuditContextAccessor(AuditActorContext context) : IAuditContextAccessor
    {
        public AuditActorContext? GetCurrentContext() => context;
        public Task<AuditActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(context);
    }

    private sealed class FixedAuditClock(DateTime utcNow) : IAuditClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class FailingAuditLogService : IAuditLogService
    {
        public Task<AuditLogResult> RecordAsync(AuditLogRequest request, CancellationToken ct = default) => throw new InvalidOperationException("Simulated audit DB failure");
        public Task<AuditLogResult> RecordBatchAsync(IEnumerable<AuditLogRequest> requests, CancellationToken ct = default) => throw new InvalidOperationException("Simulated audit DB failure");
        public Task<AuditLogResult> RecordWithinTransactionAsync(DbContext businessContext, IEnumerable<AuditLogRequest> requests, CancellationToken ct = default) => throw new InvalidOperationException("Simulated audit DB failure");
        public Task<AuditLogResult> RecordApprovalAsync(string m, string e, string id, string d, string? r = null, int? o = null, int? div = null, object? meta = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordRejectionAsync(string m, string e, string id, string d, string? r = null, int? o = null, int? div = null, object? meta = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordAttendanceCorrectionAsync(string e, string id, string d, IReadOnlyList<AuditPropertyChange> c, string? r = null, int? o = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordSalaryChangeAsync(string e, string id, string d, IReadOnlyList<AuditPropertyChange> c, string? r = null, int? o = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordPayrollAdjustmentAsync(string e, string id, string d, IReadOnlyList<AuditPropertyChange> c, string? r = null, int? o = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordPermissionChangeAsync(string a, string e, string id, string d, IReadOnlyList<AuditPropertyChange>? c = null, string? r = null, int? o = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordOrganisationAccessChangeAsync(string a, string id, string d, int? o = null, string? r = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordStaffTransferDecisionAsync(string a, string id, string d, string? r = null, int? o = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordWorkflowOverrideAsync(string m, string e, string id, string d, string r, int? o = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuditLogResult> RecordSecurityEventAsync(string a, string o, string m, string e, string id, string d, string? r = null, int? org = null, object? meta = null, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static AuditLogOptions CreateTestOptions()
    {
        var opts = new AuditLogOptions();
        opts.Policy.AuditedEntities[nameof(TestAuditedEntity)] = new AuditEntityRule
        {
            EntityName = nameof(TestAuditedEntity),
            Module = "Employees",
            IsMandatory = true,
            OrganisationIdProperty = nameof(TestAuditedEntity.OrganisationId),
            DivisionIdProperty = nameof(TestAuditedEntity.DivisionId)
        };
        return opts;
    }

    [Fact]
    public async Task AutomaticInterceptor_CapturesInsertUpdateDelete_WithGeneratedKeysRedactionAndDiffOnly()
    {
        var auditDbName = $"audit-{Guid.NewGuid():N}";
        var bizDbName = $"biz-{Guid.NewGuid():N}";
        var auditFactory = new InMemoryAuditDbFactory(auditDbName);
        var opts = Options.Create(CreateTestOptions());
        var actor = new AuditActorContext
        {
            TenantId = 10,
            OrganisationId = 10,
            DivisionId = 3,
            UserId = 42,
            ActorType = AuditActorTypes.User,
            CorrelationId = "corr-123",
            Source = "HRM.Api",
            CanViewAuditLogs = true,
            CanViewConfidentialValues = true,
            AuthorizedOrganisationIds = [10]
        };
        var accessor = new FixedAuditContextAccessor(actor);
        var clock = new FixedAuditClock(new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc));
        var auditService = new AuditLogService(auditFactory, accessor, clock, opts, NullLogger<AuditLogService>.Instance);
        var interceptor = new AuditSaveChangesInterceptor(auditService, accessor, clock, opts, NullLogger<AuditSaveChangesInterceptor>.Instance);

        var bizOptions = new DbContextOptionsBuilder<TestBusinessDbContext>()
            .UseInMemoryDatabase(bizDbName)
            .AddInterceptors(interceptor)
            .Options;

        int generatedId;
        await using (var db = new TestBusinessDbContext(bizOptions))
        {
            var entity = new TestAuditedEntity
            {
                OrganisationId = 10,
                DivisionId = 3,
                FullName = "Aminath Ali",
                PasswordHash = "super-secret-hash",
                BasicSalary = 15000m,
                UnchangedNote = "keep-same"
            };
            db.Entities.Add(entity);
            await db.SaveChangesAsync();
            generatedId = entity.Id;
            Assert.True(generatedId > 0);

            // UPDATE only FullName & BasicSalary (UnchangedNote must NOT be audited)
            entity.FullName = "Aminath Ali Updated";
            entity.BasicSalary = 18000m;
            await db.SaveChangesAsync();

            // DELETE
            db.Entities.Remove(entity);
            await db.SaveChangesAsync();
        }

        await using var auditDb = auditFactory.CreateDbContext();
        var logs = await auditDb.AuditLogs.Include(x => x.Details).OrderBy(x => x.AuditLogId).ToListAsync();
        Assert.Equal(3, logs.Count);

        // 1. INSERT with resolved generated PK and redacted PasswordHash
        var insertLog = logs[0];
        Assert.Equal("INSERT", insertLog.Action);
        Assert.Equal(generatedId.ToString(), insertLog.EntityId);
        Assert.Equal(10, insertLog.OrganisationId);
        Assert.Equal(3, insertLog.DivisionId);
        Assert.Equal("corr-123", insertLog.CorrelationId);
        var pwdDetail = insertLog.Details.Single(d => d.PropertyName == nameof(TestAuditedEntity.PasswordHash));
        Assert.True(pwdDetail.IsRedacted);
        Assert.Equal(AuditDataRedaction.RedactedPlaceholder, pwdDetail.NewValue);
        Assert.DoesNotContain("super-secret-hash", pwdDetail.NewValue ?? "");

        // 2. UPDATE captures ONLY changed properties (OldValue -> NewValue)
        var updateLog = logs[1];
        Assert.Equal("UPDATE", updateLog.Action);
        Assert.Equal(2, updateLog.Details.Count);
        Assert.DoesNotContain(updateLog.Details, d => d.PropertyName == nameof(TestAuditedEntity.UnchangedNote));
        var salaryDiff = updateLog.Details.Single(d => d.PropertyName == nameof(TestAuditedEntity.BasicSalary));
        Assert.Equal("15000", salaryDiff.OldValue);
        Assert.Equal("18000", salaryDiff.NewValue);

        // 3. DELETE captures OldValues and preserves history even though business row is deleted
        var deleteLog = logs[2];
        Assert.Equal("DELETE", deleteLog.Action);
        Assert.Contains(deleteLog.Details, d => d.PropertyName == nameof(TestAuditedEntity.FullName) && d.OldValue == "Aminath Ali Updated" && d.NewValue == null);
    }

    [Fact]
    public async Task MandatoryAuditFailure_ThrowsAndDoesNotSilentlySucceed()
    {
        var bizOptions = new DbContextOptionsBuilder<TestBusinessDbContext>()
            .UseInMemoryDatabase($"biz-fail-{Guid.NewGuid():N}")
            .AddInterceptors(new AuditSaveChangesInterceptor(
                new FailingAuditLogService(),
                new FixedAuditContextAccessor(AuditActorContext.System()),
                new FixedAuditClock(DateTime.UtcNow),
                Options.Create(CreateTestOptions()),
                NullLogger<AuditSaveChangesInterceptor>.Instance))
            .Options;

        await using var db = new TestBusinessDbContext(bizOptions);
        db.Entities.Add(new TestAuditedEntity { OrganisationId = 1, FullName = "Test" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrencyConflict_DoesNotEmitOrphanedAuditRecord()
    {
        var auditDbName = $"audit-conc-{Guid.NewGuid():N}";
        var bizDbName = $"biz-conc-{Guid.NewGuid():N}";
        var auditFactory = new InMemoryAuditDbFactory(auditDbName);
        var opts = Options.Create(CreateTestOptions());
        var accessor = new FixedAuditContextAccessor(AuditActorContext.System());
        var clock = new FixedAuditClock(DateTime.UtcNow);
        var auditService = new AuditLogService(auditFactory, accessor, clock, opts, NullLogger<AuditLogService>.Instance);
        var interceptor = new AuditSaveChangesInterceptor(auditService, accessor, clock, opts, NullLogger<AuditSaveChangesInterceptor>.Instance);

        var bizOptions = new DbContextOptionsBuilder<TestBusinessDbContext>()
            .UseInMemoryDatabase(bizDbName)
            .AddInterceptors(interceptor)
            .Options;

        int id;
        await using (var seed = new TestBusinessDbContext(bizOptions))
        {
            var e = new TestAuditedEntity { OrganisationId = 1, FullName = "Original", ConcurrencyToken = "v1" };
            seed.Entities.Add(e);
            await seed.SaveChangesAsync();
            id = e.Id;
        }

        await using var db1 = new TestBusinessDbContext(bizOptions);
        await using var db2 = new TestBusinessDbContext(bizOptions);
        var e1 = await db1.Entities.SingleAsync(x => x.Id == id);
        var e2 = await db2.Entities.SingleAsync(x => x.Id == id);

        e1.FullName = "Winner";
        e1.ConcurrencyToken = "v2";
        await db1.SaveChangesAsync();

        e2.FullName = "StaleLoser";
        e2.ConcurrencyToken = "v3";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());

        await using var auditDb = auditFactory.CreateDbContext();
        var logs = await auditDb.AuditLogs.ToListAsync();
        // Only INSERT + Winner UPDATE; StaleLoser failed and produced NO audit entry.
        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task ExplicitRecordAsync_IsIdempotentByEventId_AndNeverRecursivelyAudits()
    {
        var auditFactory = new InMemoryAuditDbFactory($"audit-idem-{Guid.NewGuid():N}");
        var opts = Options.Create(CreateTestOptions());
        var accessor = new FixedAuditContextAccessor(AuditActorContext.System());
        var clock = new FixedAuditClock(DateTime.UtcNow);
        var service = new AuditLogService(auditFactory, accessor, clock, opts, NullLogger<AuditLogService>.Instance);

        var fixedEventId = Guid.NewGuid();
        var req = new AuditLogRequest
        {
            EventId = fixedEventId,
            OrganisationId = 10,
            Action = "APPROVE",
            Module = "Transfers",
            EntityType = "StaffTransfer",
            EntityId = "99",
            Description = "Approved transfer #99"
        };

        var first = await service.RecordAsync(req);
        var retry = await service.RecordAsync(req);

        Assert.True(first.Succeeded);
        Assert.False(first.WasDuplicate);
        Assert.True(retry.Succeeded);
        Assert.True(retry.WasDuplicate);
        Assert.Equal(first.AuditLogId, retry.AuditLogId);

        // Policy strictly excludes AuditLog & AuditLogDetail from recursive auditing
        Assert.False(opts.Value.Policy.ShouldAuditEntity(nameof(AuditLog), out _));
        Assert.False(opts.Value.Policy.ShouldAuditEntity(nameof(AuditLogDetail), out _));
    }

    [Fact]
    public async Task QueryService_EnforcesTenantDivisionDelegatedAccess_AuditsCrossOrgRead_AndMasksConfidentialFields()
    {
        var auditFactory = new InMemoryAuditDbFactory($"audit-sec-{Guid.NewGuid():N}");
        var opts = Options.Create(CreateTestOptions());
        var clock = new FixedAuditClock(new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc));
        var systemAccessor = new FixedAuditContextAccessor(AuditActorContext.System());
        var writer = new AuditLogService(auditFactory, systemAccessor, clock, opts, NullLogger<AuditLogService>.Instance);
        var query = new AuditLogQueryService(auditFactory, writer, opts);

        await writer.RecordAsync(new AuditLogRequest
        {
            OrganisationId = 1,
            DivisionId = 10,
            UserId = 5,
            Action = "UPDATE",
            Module = "Compensation",
            EntityType = "EmployeeCompensation",
            EntityId = "500",
            Description = "Salary revised",
            Changes = [new AuditPropertyChange("BasicSalary", 12000m, 15000m)]
        });
        await writer.RecordAsync(new AuditLogRequest
        {
            OrganisationId = 1,
            DivisionId = 20,
            UserId = 6,
            Action = "UPDATE",
            Module = "Employees",
            EntityType = "Job",
            EntityId = "501",
            Description = "Division 20 change"
        });
        await writer.RecordAsync(new AuditLogRequest
        {
            OrganisationId = 2,
            DivisionId = 30,
            UserId = 7,
            Action = "APPROVE",
            Module = "Payroll",
            EntityType = "PayrollRun",
            EntityId = "77",
            Description = "Org 2 payroll approved"
        });

        // 1. Ordinary Org 1 / Division 10 viewer without confidential access:
        var org1Div10Viewer = new AuditActorContext
        {
            OrganisationId = 1,
            UserId = 100,
            CanViewAuditLogs = true,
            CanViewConfidentialValues = false,
            CanViewCrossOrganisationAudit = false,
            AuthorizedOrganisationIds = [1],
            AuthorizedDivisionIds = [10]
        };

        // Cannot query Org 2
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.SearchAsync(new AuditLogFilter { OrganisationId = 2 }, org1Div10Viewer));

        // Cannot query Division 20
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.SearchAsync(new AuditLogFilter { OrganisationId = 1, DivisionId = 20 }, org1Div10Viewer));

        // Can query Org 1 / Division 10, and BasicSalary is masked as [RESTRICTED]
        var div10Results = await query.SearchAsync(new AuditLogFilter { IncludeDetails = true }, org1Div10Viewer);
        var salaryLog = Assert.Single(div10Results.Items);
        var maskedDetail = Assert.Single(salaryLog.Details);
        Assert.True(maskedDetail.IsRedacted);
        Assert.Equal(AuditDataRedaction.ConfidentialPlaceholder, maskedDetail.NewValue);

        // 2. Delegated cross-organisation auditor with confidential access:
        var delegatedAuditor = new AuditActorContext
        {
            OrganisationId = 1,
            UserId = 200,
            ActorType = AuditActorTypes.Delegated,
            CanViewAuditLogs = true,
            CanViewConfidentialValues = true,
            CanViewCrossOrganisationAudit = true,
            AuthorizedOrganisationIds = [1, 2]
        };

        var org2Results = await query.SearchAsync(new AuditLogFilter { OrganisationId = 2 }, delegatedAuditor);
        Assert.Contains(org2Results.Items, x => x.EntityId == "77");

        // Cross-organisation read itself generated a CROSS_ORG_AUDIT_VIEW audit entry!
        await using var auditDb = auditFactory.CreateDbContext();
        Assert.True(await auditDb.AuditLogs.AnyAsync(x => x.Action == "CROSS_ORG_AUDIT_VIEW" && x.UserId == 200));
    }

    [Fact]
    public void AuditDbContext_ModelIntegrity_UsesAuditSchemaUniqueEventIdAndRestrictDelete()
    {
        var opts = new DbContextOptionsBuilder<HRM.AuditLogs.Persistence.AuditDbContext>()
            .UseInMemoryDatabase($"audit-meta-{Guid.NewGuid():N}")
            .Options;
        using var db = new HRM.AuditLogs.Persistence.AuditDbContext(opts);

        var logEntity = db.Model.FindEntityType(typeof(HRM.AuditLogs.Models.AuditLog))!;
        var detailEntity = db.Model.FindEntityType(typeof(HRM.AuditLogs.Models.AuditLogDetail))!;

        Assert.Equal("audit", logEntity.GetSchema());
        Assert.Equal("AuditLogs", logEntity.GetTableName());
        Assert.Equal("audit", detailEntity.GetSchema());
        Assert.Equal("AuditLogDetails", detailEntity.GetTableName());

        var eventIdIndex = logEntity.GetIndexes().Single(i => i.Properties.Any(p => p.Name == nameof(AuditLog.EventId)));
        Assert.True(eventIdIndex.IsUnique);

        var fk = detailEntity.GetForeignKeys().Single();
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }
}
