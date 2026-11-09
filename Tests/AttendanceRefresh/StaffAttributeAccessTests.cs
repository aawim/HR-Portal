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
    }

    private static Individual Person(int id, string name) => new() { BusinessEntityId = id, FirstNameEnglish = name };
    private static Job Job(int id, int person) => new() { JobId = id, IndividualID = person, OrganisationID = 10, JobStateId = 4, JoinedDate = DateTime.UtcNow.AddYears(-1) };
    private static JobPosition Position(int id, int job, int position) => new() { JobPositionId = id, JobId = job, PositionId = position, JobPositionStateId = 4, FromDate = DateTime.UtcNow.AddYears(-1) };
    private static async Task Denied(Func<Task> action) { try { await action(); throw new Exception("Expected access denial"); } catch (UnauthorizedAccessException) { } }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
