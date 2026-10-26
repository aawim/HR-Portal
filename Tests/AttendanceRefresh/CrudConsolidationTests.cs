using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class CrudConsolidationTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static async Task Denied(Func<Task> action) { try { await action(); } catch (UnauthorizedAccessException) { return; } throw new Exception("Expected scope denial"); }
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var authorised = true;
        var access = Proxy.For<IUserAccessService>((m, a) => m switch {
            "RequireContextAsync" or "GetContextAsync" => Task.FromResult(new UserContextDto { UserId = 1, IndividualId = 100, Organisation = new UserOrganisationDto { OrganisationBusinessEntityId = 10 } }),
            "HasPermissionAsync" => Task.FromResult(authorised && (string)a[0]! == "Admin"),
            "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == 10), "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            _ => throw new Exception(m) });
        var logs = Proxy.For<IOperationLogService>((m, a) => m == "CreateAsync" ? Log((HrmTeContext)a[0]!) : throw new Exception(m));
        var contexts = Proxy.For<IUserContextService>((m, a) => m == "InvalidateAsync" ? Task.CompletedTask : throw new Exception(m));
        var individual = Proxy.For<IIndividual>((m, a) => m == "GetIndividualID" ? Task.FromResult(100) : throw new Exception(m));
        var accounts = new UserSettingsService(factory, access, logs, contexts);
        var organisations = new OrganisationSettingsService(factory, access, logs);
        var personal = new AddressService(factory, logs, individual, access);
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "First" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Other" });
            db.Individuals.Add(new Individual { BusinessEntityId = 100, FirstNameEnglish = "Employee" });
            db.UserOrganisations.AddRange(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 }, new UserOrganisation { UserOrganisationID = 2, BusinessEntityID = 20 });
            db.Users.AddRange(User(), new User { UserId = 2, BusinessEntityID = 200, Username = "A2", UserOrganisationId = 2, IsActive = true, LocalUserStateId = 1, FullName = "Other", Email = "other@example.test" });
            db.LocalUserStates.Add(new LocalUserState { LocalUserStateId = 1, StateName = "Active" });
            db.Countries.Add(new HRM.Models.Archives.Country { CountryId = 1, NameEnglish = "Maldives" });
            db.LocationTypes.Add(new HRM.Models.Archives.LocationType { LocationTypeId = 1, TypeName = "Address" });
            db.AddressBaseTypes.Add(new AddressBaseType { AddressBaseTypeId = 1, TypeName = "House" });
            db.AddressInstanceTypes.Add(new AddressInstanceType { AddressInstanceTypeId = 1, TypeName = "Home" });
            db.BusinessEntityLocationTypes.Add(new BusinessEntityLocationType { BusinessEntityLocationTypeId = 1, TypeName = "Current address" });
            await db.SaveChangesAsync();
        }
        Check((await personal.CreateAddressAsync(Address("Home one"))).Success, "Personal address creation failed");
        Check((await organisations.SaveAddressAsync(10, new OrganisationAddressDto { UsageId = 1, BaseTypeId = 1, CountryId = 1, Line1 = "Organisation office" })).Success, "Organisation address creation failed");
        Check((await personal.CreateAddressAsync(Address("Home two"))).Success, "Second personal address creation failed");
        int orgLink;
        await using (var db = factory.CreateDbContext())
        {
            Check(await db.Locations.CountAsync() == 3 && await db.AddressInstances.CountAsync() == 3 && await db.AddressBases.CountAsync() == 3, "Shared address writer duplicated physical records");
            Check((await db.AddressInstances.Include(a => a.AddressBase).SingleAsync(a => a.Name == "Home one")).AddressBase.IsCurrent == false, "Old personal address remained current");
            Check((await db.AddressInstances.Include(a => a.AddressBase).SingleAsync(a => a.Name == "Home two")).AddressBase.IsCurrent == true, "New personal address was cleared by its own save");
            Check((await db.AddressBases.SingleAsync(a => a.AddressLine1 == "Organisation office")).IsCurrent == true, "Personal address change altered another owner's address");
            orgLink = (await db.BusinessEntityLocations.SingleAsync(l => l.BusinessEntityId == 10)).BusinessEntityLocationId;
            Check(await db.Locations.AllAsync(l => l.OperationLogId != null && l.OperationLogId > 0), "Address audit links were not applied");
        }
        await Denied(() => organisations.SaveAddressAsync(20, new OrganisationAddressDto { LinkId = orgLink, UsageId = 1, BaseTypeId = 1, CountryId = 1, Line1 = "Attempted move" }));
        Check(!(await personal.CreateAddressAsync(new AddressCreateDto { Name = "Invalid", CountryId = 999, AddressBaseTypeId = 1, BusinessEntityLocationTypeId = 1 })).Success, "Invalid address references accepted");
        await Denied(() => organisations.DeleteAddressAsync(20, orgLink));
        Check((await organisations.DeleteAddressAsync(10, orgLink)).Success, "Canonical address removal failed");
        await using (var db = factory.CreateDbContext())
        {
            var employee = new EmployeeService(db, accounts);
            Check(await employee.UpdateEmployeeAsync(User("Changed through legacy facade")), "Legacy employee update did not use canonical account editor");
            var clock = new AttendanceLogService(db, null!, logs, factory, access, accounts);
            Check(await clock.UpdateUserAsync(User("Changed through attendance facade")), "Legacy attendance account update did not delegate");
            var invalid = User(); invalid.Username = "FORGED";
            Check(!await employee.UpdateEmployeeAsync(invalid), "Legacy whole-entity write changed the login identity");
        }
        var profiles = new UserService(factory, access, accounts);
        Check(await profiles.UpdateUserAsync(new UserProfileDto { UserId = 1, IndividualID = 100, Username = "A1", Mobile = "12345", Email = "employee@example.test" }), "Profile compatibility update failed");
        Check(!await profiles.UpdateUserProfileAsync(new UserProfileDto { UserId = 1, IndividualID = 200, Username = "A1" }), "Profile write rebound the employee identity");
        try { await accounts.SaveLegacyProfileAsync(new UserProfileDto { UserId = 2, IndividualID = 200, Username = "A2" }); throw new Exception("Foreign account was accessible"); }
        catch (InvalidOperationException ex) when (ex.Message == "Account was not found in this organisation.") { }
        authorised = false;
        await Denied(() => accounts.SaveLegacyUserAsync(User()));
        await using (var db = factory.CreateDbContext())
        {
            var saved = await db.Users.SingleAsync(u => u.UserId == 1);
            Check(saved.Username == "A1" && saved.BusinessEntityID == 100 && saved.Mobile == "12345", "Consolidated account update changed identity or lost contact data");
        }
    }
    private static User User(string name = "Employee") => new() { UserId = 1, BusinessEntityID = 100, Username = "A1", UserOrganisationId = 1,
        IsActive = true, IsApproved = true, LocalUserStateId = 1, FullName = name, Email = "employee@example.test", Mobile = "555" };
    private static AddressCreateDto Address(string name) => new() { Name = name, CountryId = 1, AddressBaseTypeId = 1, AddressInstanceTypeId = 1, BusinessEntityLocationTypeId = 1, IsCurrent = true };
    private static async Task<OperationLog> Log(HrmTeContext db)
    {
        var row = new OperationLog { CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, CreatedByIp = "", UpdatedByIp = "" };
        db.OperationLogs.Add(row); await db.SaveChangesAsync(); return row;
    }
}
