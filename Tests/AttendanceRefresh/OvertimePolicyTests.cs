using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static class OvertimePolicyTests
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static async Task RunAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var factory = new PolicyFactory(new DbContextOptionsBuilder<HrmTeContext>().UseSqlite(connection).Options);
        await using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();
            db.Organisations.Add(new() { BusinessEntityID = 10, OrganisationName = "Tenant" });
            db.JobTypes.Add(new() { JobTypeId = 1, TypeName = "Contract", IsActive = true });
            await db.SaveChangesAsync();
        }
        var org = 10;
        var access = Proxy.For<IUserAccessService>((m,a) => m switch {
            "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = 1, IsSuperAdministrator = true }),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(org), "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == org),
            _ => throw new Exception(m) });
        // Like the production audit service, this saves ALL currently tracked entities.
        var logs = Proxy.For<IOperationLogService>((m,a) => m == "CreateAsync" ? Audit((HrmTeContext)a[0]!) : throw new Exception(m));
        var service = new JobTypeOvertimeSettingsService(factory, access, logs);
        HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations contract = service;
        var model = await contract.LoadAsync(1);
        Check(model.Id == 0 && !model.IsAllowed, "Unsaved policy should default to disabled");
        model.IsAllowed = true; model.MaximumWeeklyHours = 10; model.MaximumDailyHours = 2;
        await contract.SaveAsync(model);
        var loaded = await contract.LoadAsync(1);
        Check(loaded.Id > 0 && loaded.IsAllowed && loaded.MaximumWeeklyHours == 10 && loaded.MaximumDailyHours == 2, "First policy save failed");
        await using (var db = factory.CreateDbContext())
            Check(await db.OperationLogs.AnyAsync(l => l.OperationLogId == loaded.OperationLogId), "Required audit foreign key is invalid");
        loaded.MaximumWeeklyHours = 12; await contract.SaveAsync(loaded);
        var updated = await contract.LoadAsync(1);
        Check(updated.Version == loaded.Version + 1 && updated.MaximumWeeklyHours == 12, "Policy update/version failed");
        var denied = false;
        try { await contract.SaveAsync(loaded); } catch (InvalidOperationException) { denied = true; }
        Check(denied, "Stale policy overwrote newer limits");
        updated.IsAllowed = false; await contract.SaveAsync(updated);
        var disabled = await contract.LoadAsync(1);
        Check(!disabled.IsAllowed && disabled.MaximumWeeklyHours == 0 && disabled.MaximumDailyHours == null, "Disabling policy retained limits");
        org = 20; denied = false;
        try { await contract.SaveAsync(disabled); } catch (InvalidOperationException) { denied = true; }
        Check(denied, "Tenant switch wrote a policy to the wrong organisation");
    }
    static async Task<OperationLog> Audit(HrmTeContext db)
    {
        var row = new OperationLog { CreatedByIp = "", UpdatedByIp = "", CreatedDate = DateTime.UtcNow };
        db.OperationLogs.Add(row); await db.SaveChangesAsync(); return row;
    }
    sealed class PolicyFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
    { public HrmTeContext CreateDbContext() => new PolicyContext(options); }
    sealed class PolicyContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            Type[] types = [typeof(JobType), typeof(Organisation), typeof(OperationLog), typeof(JobTypeOvertimeRule)];
            foreach (var type in builder.Model.GetEntityTypes().Select(x => x.ClrType).ToArray())
                if (!types.Contains(type)) builder.Ignore(type);
            foreach (var type in types)
                foreach (var property in type.GetProperties().Where(x => !x.PropertyType.IsValueType && x.PropertyType != typeof(string) && x.PropertyType != typeof(byte[])))
                    builder.Entity(type).Ignore(property.Name);
            builder.Entity<JobType>().HasKey(x => x.JobTypeId);
            builder.Entity<Organisation>().HasKey(x => x.BusinessEntityID);
            builder.Entity<OperationLog>().HasKey(x => x.OperationLogId);
            builder.ApplyConfiguration(new JobTypeOvertimeRuleMapping());
            // SQLite has no native decimal; numeric affinity keeps the production limit check numeric.
            builder.Entity<JobTypeOvertimeRule>().Property(x => x.MaximumWeeklyHours).HasColumnType("REAL");
            builder.Entity<JobTypeOvertimeRule>().Property(x => x.MaximumDailyHours).HasColumnType("REAL");
        }
    }
}
