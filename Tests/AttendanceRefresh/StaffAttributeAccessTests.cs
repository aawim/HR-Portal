using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

static class StaffAttributeAccessTests
{
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options);
        var actor = new UserContextDto { UserId = 1, IndividualId = 100, IsHRStaff = true };
        var access = Proxy.For<IUserAccessService>((name, args) => name switch
        {
            "RequireContextAsync" => Task.FromResult(actor),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)args[0]! == 10),
            "HasPermissionAsync" => Task.FromResult((string)args[0]! == "Employee.View"),
            _ => throw new Exception(name)
        });
        await using var db = factory.CreateDbContext();
        db.Organisations.Add(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant" });
        db.UserOrganisations.Add(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 });
        db.Individuals.AddRange(Person(100, "Actor"), Person(200, "Political"), Person(300, "Normal"));
        db.Staffs.AddRange(new Staff { IndividualId = 100 }, new Staff { IndividualId = 200 }, new Staff { IndividualId = 300 });
        db.Users.Add(new User { UserId = 1, BusinessEntityID = 100, UserOrganisationId = 1, Username = "actor", IsActive = true, LocalUserStateId = 1 });
        db.PositionClassifications.AddRange(new PositionClassification { PositionClassificationId = 1, Name = "Political", IsActive = true },
            new PositionClassification { PositionClassificationId = 2, Name = "Civil Service", IsActive = true });
        db.Positions.AddRange(new Position { PositionId = 1, Name = "Political appointee", PositionClassificationId = 1, IsActive = true },
            new Position { PositionId = 2, Name = "Officer", PositionClassificationId = 2, IsActive = true });
        db.Jobs.AddRange(Job(1, 200), Job(2, 300));
        db.JobPositions.AddRange(Position(1, 1, 1), Position(2, 2, 2));
        db.StaffAccessPolicies.Add(new StaffAccessPolicy { OrganisationId = 10, PositionClassificationId = 1, IsRestricted = true, IsActive = true });
        await db.SaveChangesAsync();

        Check(!await StaffAttributeAccess.CanViewAsync(db, access, 200, 10), "Political staff was visible without an ABAC grant");
        Check(await StaffAttributeAccess.CanViewAsync(db, access, 300, 10), "Normal staff was hidden without a restriction");
        await Denied(() => StaffDataAccess.RequireIndividualAsync(db, access, 200));

        db.StaffAccessGrants.Add(new StaffAccessGrant { OrganisationId = 10, UserId = 1, PositionClassificationId = 1, IsAllowed = true, IsActive = true, Reason = "Political HR team" });
        await db.SaveChangesAsync();
        Check(await StaffAttributeAccess.CanViewAsync(db, access, 200, 10), "Classification grant did not reveal Political staff");

        db.StaffAccessGrants.Add(new StaffAccessGrant { OrganisationId = 10, UserId = 1, TargetIndividualId = 200, IsAllowed = false, IsActive = true, Reason = "Conflict" });
        await db.SaveChangesAsync();
        Check(!await StaffAttributeAccess.CanViewAsync(db, access, 200, 10), "Explicit deny did not override a classification allow");

        db.StaffAccessScopes.Add(new StaffAccessScope { OrganisationId = 10, UserId = 1, RequireExplicitGrant = true, IsActive = true });
        await db.SaveChangesAsync();
        Check(!await StaffAttributeAccess.CanViewAsync(db, access, 300, 10), "Explicit-only scope leaked normal staff");
        db.StaffAccessGrants.Add(new StaffAccessGrant { OrganisationId = 10, UserId = 1, TargetIndividualId = 300, IsAllowed = true, IsActive = true, Reason = "Assigned case" });
        await db.SaveChangesAsync();
        Check(await StaffAttributeAccess.CanViewAsync(db, access, 300, 10), "Direct staff grant was not applied");
        Check(await StaffAttributeAccess.CanViewAsync(db, access, 100, 10), "Self access was blocked");

        await RunOrganisationalIsolationTestsAsync();
    }

    private static async Task RunOrganisationalIsolationTestsAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options);
        var grantedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Employee.View",
            "Staff.AccessPolicy.Manage",
            "Staff.AccessScope.Manage",
            "Staff.AccessScope.Simulate"
        };
        var actor = new UserContextDto
        {
            UserId = 10,
            IndividualId = 1000,
            IsHRStaff = true,
            Username = "northern.hr",
            ActiveJob = new ActiveJobDto { JobId = 10 }
        };
        var access = Proxy.For<IUserAccessService>((name, args) => name switch
        {
            "RequireContextAsync" => Task.FromResult(actor),
            "GetContextAsync" => Task.FromResult<UserContextDto?>(actor),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)args[0]! == 10),
            "HasPermissionAsync" => Task.FromResult(grantedPermissions.Contains((string)args[0]!)),
            _ => throw new Exception(name)
        });

        await using var db = factory.CreateDbContext();
        db.Organisations.Add(new Organisation { BusinessEntityID = 10, OrganisationName = "DHR Enterprise" });
        db.UserOrganisations.Add(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 });

        // Hierarchy: 101 Main Office, 102 Northern Division -> 103 Northern Ops Dept, 104 Southern Division
        db.OrganisationStructures.AddRange(
            new OrganisationStructure { OrganisationStructureId = 101, OrganisationBusinessEntityID = 10, Name = "Main Office", OrganisationStructureStateId = 3 },
            new OrganisationStructure { OrganisationStructureId = 102, OrganisationBusinessEntityID = 10, Name = "Northern Division", OrganisationStructureStateId = 3 },
            new OrganisationStructure { OrganisationStructureId = 103, OrganisationBusinessEntityID = 10, Name = "Northern Operations Dept", OrganisationStructureStateId = 3 },
            new OrganisationStructure { OrganisationStructureId = 104, OrganisationBusinessEntityID = 10, Name = "Southern Division", OrganisationStructureStateId = 3 });
        db.OrganisationStructureRelations.Add(new OrganisationStructureRelation
        {
            OrganisationStructureRelationId = 1,
            ParentOrganisationStructureId = 102,
            OrganisationStructureId = 103,
            IsCurrent = true,
            ActiveFromDate = DateTime.UtcNow.AddYears(-1)
        });

        db.Individuals.AddRange(
            Person(1000, "NorthernHR"),
            Person(1001, "MainOfficeStaff"),
            Person(1002, "NorthernDivStaff"),
            Person(1003, "NorthernDeptStaff"),
            Person(1004, "SouthernDivStaff"));
        db.Staffs.AddRange(
            new Staff { IndividualId = 1000 },
            new Staff { IndividualId = 1001 },
            new Staff { IndividualId = 1002 },
            new Staff { IndividualId = 1003 },
            new Staff { IndividualId = 1004 });
        db.Users.Add(new User { UserId = 10, BusinessEntityID = 1000, UserOrganisationId = 1, Username = "northern.hr", IsActive = true, LocalUserStateId = 1 });

        db.Jobs.AddRange(
            new Job { JobId = 10, IndividualID = 1000, OrganisationID = 10, OrganisationStructureId = 102, JobStateId = 4, JoinedDate = DateTime.UtcNow.AddYears(-1), BasicSalary = 15000m },
            new Job { JobId = 11, IndividualID = 1001, OrganisationID = 10, OrganisationStructureId = 101, JobStateId = 4, JoinedDate = DateTime.UtcNow.AddYears(-1), BasicSalary = 18000m },
            new Job { JobId = 12, IndividualID = 1002, OrganisationID = 10, OrganisationStructureId = 102, JobStateId = 4, JoinedDate = DateTime.UtcNow.AddYears(-1), BasicSalary = 16500m },
            new Job { JobId = 13, IndividualID = 1003, OrganisationID = 10, OrganisationStructureId = 103, JobStateId = 4, JoinedDate = DateTime.UtcNow.AddYears(-1), BasicSalary = 14000m },
            new Job { JobId = 14, IndividualID = 1004, OrganisationID = 10, OrganisationStructureId = 104, JobStateId = 4, JoinedDate = DateTime.UtcNow.AddYears(-1), BasicSalary = 17000m });

        // Assign Northern Division + Descendants scope to Northern HR Officer
        db.UserAccessScopes.Add(new UserAccessScope
        {
            UserAccessScopeId = 1,
            OrganisationId = 10,
            UserId = 10,
            ScopeType = AccessScopeTypes.DivisionAndDescendants,
            UnitId = 102,
            IncludeDescendants = true,
            IsActive = true,
            Reason = "Northern HR Officer assignment"
        });
        await db.SaveChangesAsync();

        // 1. Northern HR Officer can view Northern Division (1002) and descendant Northern Dept (1003), but NOT Main Office (1001) or Southern Division (1004)
        Check(await StaffAttributeAccess.CanViewAsync(db, access, 1002, 10), "Northern HR could not view Northern Division employee");
        Check(await StaffAttributeAccess.CanViewAsync(db, access, 1003, 10), "Northern HR could not view descendant Northern Department employee");
        Check(!await StaffAttributeAccess.CanViewAsync(db, access, 1001, 10), "Northern HR leaked Main Office employee");
        Check(!await StaffAttributeAccess.CanViewAsync(db, access, 1004, 10), "Northern HR leaked Southern Division employee");
        await Denied(() => StaffDataAccess.RequireIndividualAsync(db, access, 1001));
        await Denied(() => StaffDataAccess.RequireIndividualAsync(db, access, 1004));

        // 2. Multi-division grant: add Southern Division scope
        var southernScope = new UserAccessScope
        {
            UserAccessScopeId = 2,
            OrganisationId = 10,
            UserId = 10,
            ScopeType = AccessScopeTypes.DivisionAndDescendants,
            UnitId = 104,
            IncludeDescendants = true,
            IsActive = true,
            Reason = "Multi-division HR coverage"
        };
        db.UserAccessScopes.Add(southernScope);
        await db.SaveChangesAsync();

        Check(await StaffAttributeAccess.CanViewAsync(db, access, 1002, 10), "Multi-division HR lost Northern Division access");
        Check(await StaffAttributeAccess.CanViewAsync(db, access, 1004, 10), "Multi-division HR could not view Southern Division employee");
        Check(!await StaffAttributeAccess.CanViewAsync(db, access, 1001, 10), "Multi-division HR leaked Main Office employee");

        // 3. Field-level classification redaction check
        var resolvedWithoutSensitivePerms = await OrganisationalAccessScopeResolver.ResolveCurrentUserAsync(db, access, 10);
        Check(!resolvedWithoutSensitivePerms.FieldPermissions.CanViewSalaryAndBank, "Salary & bank should be redacted without Employee.Salary.View");
        Check(!resolvedWithoutSensitivePerms.FieldPermissions.CanViewIdentification, "Identification should be redacted without Employee.Identification.View");
        Check(!resolvedWithoutSensitivePerms.FieldPermissions.CanViewPrivateRecord, "Private record should be redacted without Employee.PrivateRecord.View");

        grantedPermissions.Add("Employee.Salary.View");
        grantedPermissions.Add("Employee.Identification.View");
        var resolvedWithSensitivePerms = await OrganisationalAccessScopeResolver.ResolveCurrentUserAsync(db, access, 10);
        Check(resolvedWithSensitivePerms.FieldPermissions.CanViewSalaryAndBank, "Salary & bank should be visible with Employee.Salary.View");
        Check(resolvedWithSensitivePerms.FieldPermissions.CanViewIdentification, "Identification should be visible with Employee.Identification.View");

        // 4. Effective Access Simulation & Scope Management via PermissionSettingsService
        var logs = Proxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = 1 }));
        var contexts = Proxy.For<IUserContextService>((_, _) => Task.CompletedTask);
        var permissionSettingsService = new HRM.Services.PermissionSettingsService(factory, access, logs, contexts);
        var simulation = await permissionSettingsService.SimulateEffectiveAccessAsync(10);
        Check(simulation.UserId == 10, "Simulation returned wrong user ID");
        Check(!simulation.HasFullOrganisationScope, "Multi-division user should not report organisation-wide access");
        Check(simulation.TotalVisibleStaff == 4, $"Expected 4 visible employees (self + Northern + Northern Dept + Southern), got {simulation.TotalVisibleStaff}");
        Check(simulation.UnitAccessMatrix.Any(u => u.UnitId == 102 && u.IsAllowed), "Simulation did not mark Northern Division as allowed");
        Check(simulation.UnitAccessMatrix.Any(u => u.UnitId == 103 && u.IsAllowed), "Simulation did not mark descendant Northern Dept as allowed");
        Check(simulation.UnitAccessMatrix.Any(u => u.UnitId == 104 && u.IsAllowed), "Simulation did not mark Southern Division as allowed");
        Check(simulation.UnitAccessMatrix.Any(u => u.UnitId == 101 && !u.IsAllowed), "Simulation did not mark Main Office as restricted");

        // 5. Revoke Southern Division scope and verify audit trail
        await permissionSettingsService.RevokeOrganisationalAccessScopeAsync(2, "End of temporary coverage");
        Check(!await StaffAttributeAccess.CanViewAsync(db, access, 1004, 10), "Revoked Southern Division scope still allowed access");
        Check(await db.UserAccessScopeAudits.AnyAsync(a => a.UserAccessScopeId == 2 && a.Action == AccessScopeAuditActions.Revoked), "Scope revocation audit was not recorded");
    }

    private static Individual Person(int id, string name) => new() { BusinessEntityId = id, FirstNameEnglish = name };
    private static Job Job(int id, int person) => new() { JobId = id, IndividualID = person, OrganisationID = 10, JobStateId = 4, JoinedDate = DateTime.UtcNow.AddYears(-1) };
    private static JobPosition Position(int id, int job, int position) => new() { JobPositionId = id, JobId = job, PositionId = position, JobPositionStateId = 4, FromDate = DateTime.UtcNow.AddYears(-1) };
    private static async Task Denied(Func<Task> action) { try { await action(); throw new Exception("Expected access denial"); } catch (UnauthorizedAccessException) { } }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
