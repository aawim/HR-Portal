using HRM.DTOs.Organisation;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class OrganisationResourceSettingsTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static async Task Denied(Func<Task> action) { try { await action(); } catch (UnauthorizedAccessException) { return; } throw new Exception("Expected access denial"); }
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var super = false;
        var access = Proxy.For<IUserAccessService>((m, a) => m switch {
            "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = 1, IndividualId = 100, IsSuperAdministrator = super }),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10), "HasOrganisationAccessAsync" => Task.FromResult((int)a[0]! == 10),
            "HasPermissionAsync" => Task.FromResult((string)a[0]! == "Admin"), _ => throw new Exception(m) });
        var logs = Proxy.For<IOperationLogService>((m, a) => m == "CreateAsync" ? Log((HrmTeContext)a[0]!) : throw new Exception(m));
        var service = new OrganisationResourceSettingsService(factory, access, logs);
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Other" });
            db.OrganisationStructureTypes.Add(new OrganisationStructureType { OrganisationStructureTypeId = 1, TypeName = "Department" });
            db.OrganisationTypes.Add(new OrganisationType { OrganisationTypeId = 1, TypeName = "Government" });
            db.OrganisationStructureRequestTypes.Add(new OrganisationStructureRequestType { OrganisationStructureRequestTypeId = 1, TypeName = "Create structure" });
            db.LocationTypes.Add(new LocationType { LocationTypeId = 1, TypeName = "Office" });
            db.Countries.Add(new Country { CountryId = 1, NameEnglish = "Maldives" });
            db.BusinessEntityLocationTypes.Add(new BusinessEntityLocationType { BusinessEntityLocationTypeId = 1, TypeName = "Head office" });
            db.Locations.AddRange(new Location { LocationId = 1, CountryId = 1, LocationTypeId = 1 }, new Location { LocationId = 2, CountryId = 1, LocationTypeId = 1 });
            db.BusinessEntityLocations.AddRange(new BusinessEntityLocation { BusinessEntityLocationId = 1, BusinessEntityId = 10, LocationId = 1, BusinessEntityLocationTypeId = 1, IsValid = true },
                new BusinessEntityLocation { BusinessEntityLocationId = 2, BusinessEntityId = 20, LocationId = 2, BusinessEntityLocationTypeId = 1, IsValid = true });
            db.Calendars.AddRange(new Calendar { CalenderId = 1, Name = "Public calendar", IsPublic = true }, new Calendar { CalenderId = 2, Name = "Private other tenant" });
            db.BusinessEntityCalendars.Add(new BusinessEntityCalendar { BusinessEntityCalenderId = 1, BusinessEntityId = 20, CalenderId = 2, IsLinked = true, StartDate = new(2026,1,1) });
            db.RequestTypes.Add(new RequestType { RequestTypeId = 1, TypeName = "Test", ViewUrl = "/admin/my/workspace" });
            db.RequestStates.AddRange(new RequestState { RequestStateId = 2, StateName = "Pending Verification", IsProcessingState = true }, new RequestState { RequestStateId = 4, StateName = "Approved", IsFinalState = true });
            db.Requests.AddRange(new Request { RequestId = 1, RequestTypeId = 1, OrganisationBusinessEntityID = 10, ReferenceNumber = "OWN", RequestStateId = 2, ApplicationDate = new(2026,10,22) },
                new Request { RequestId = 2, RequestTypeId = 1, OrganisationBusinessEntityID = 20, ReferenceNumber = "OTHER", RequestStateId = 4, ApplicationDate = new(2026,10,22) });
            await db.SaveChangesAsync();
        }
        var types = await service.LoadAsync(OrganisationResourceKind.StructureTypes);
        Check((await service.LoadAsync(OrganisationResourceKind.StructureRequestTypes)).Rows.Single().Name == "Create structure", "Structure request types used the generic request catalogue");
        Check(types.Rows.Single().Name == "Department" && !types.CanEdit, "Shared type scope or edit policy is incorrect");
        await Denied(() => service.SaveTypeAsync(OrganisationResourceKind.StructureTypes, 0, "Division", null));
        await Denied(() => service.LoadAsync(OrganisationResourceKind.Requests, 20));
        var requests = await service.LoadAsync(OrganisationResourceKind.Requests);
        Check(requests.Rows.Single().Name == "OWN" && requests.Rows[0].Status.StartsWith("Processing"), "Organisation requests leaked across tenants or lost processing status");
        var calendar = await service.LoadAsync(OrganisationResourceKind.Calendar);
        var locations = await service.LoadAsync(OrganisationResourceKind.Locations);
        Check(locations.Rows.Single().Id == 1 && locations.Rows[0].Url == "/admin/settings/org/manage/10/address", "Location view leaked tenants or bypassed the existing address editor");
        Check(calendar.Rows.Count == 0 && calendar.Calendars.Select(c => c.Id).SequenceEqual([1]), "Private calendar leaked across tenants");
        Check(!(await service.LinkCalendarAsync(10, 2, new(2026,1,1), null)).Success, "Foreign private calendar assignment accepted");
        Check((await service.LinkCalendarAsync(10, 1, new(2026,1,1), null)).Success, "Public calendar assignment failed");
        Check(!(await service.LinkCalendarAsync(10, 1, new(2026,6,1), null)).Success, "Overlapping calendar assignment accepted");
        Check((await service.LoadAsync(OrganisationResourceKind.Calendar)).Rows.Single().Name == "Public calendar", "Assigned calendar did not appear");
        super = true;
        Check((await service.LoadAsync(OrganisationResourceKind.Requests, 20)).Rows.Single().Name == "OTHER", "Super Admin organisation selection failed");
        Check((await service.SaveTypeAsync(OrganisationResourceKind.StructureTypes, 0, "Division", null)).Success, "Structure type creation failed");
        Check(!(await service.SaveTypeAsync(OrganisationResourceKind.StructureTypes, 0, " division ", null)).Success, "Duplicate type accepted");
        Check(!(await service.SaveTypeAsync(OrganisationResourceKind.StructureTypes, 1, "Unit", "Old")).Success, "Stale type edit accepted");
        Check((await service.SaveTypeAsync(OrganisationResourceKind.StructureTypes, 1, "Unit", "Department")).Success, "Type edit failed");
        Check((await service.SaveTypeAsync(OrganisationResourceKind.OrganisationTypes, 0, "Agency", null)).Success, "Organisation type creation failed");
        Check((await service.SaveTypeAsync(OrganisationResourceKind.LocationTypes, 0, "Branch", null)).Success, "Location type creation failed");
        Check((await service.SaveTypeAsync(OrganisationResourceKind.StructureRequestTypes, 0, "Restructure", null)).Success, "Structure request type creation failed");
        await using (var db = factory.CreateDbContext()) Check(await db.OperationLogs.CountAsync() == 6, "Resource changes were not audited");
    }
    private static async Task<OperationLog> Log(HrmTeContext db)
    {
        var log = new OperationLog { CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, CreatedByIp = "", UpdatedByIp = "" };
        db.OperationLogs.Add(log); await db.SaveChangesAsync(); return log;
    }
}
