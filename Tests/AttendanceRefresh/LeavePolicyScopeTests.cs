using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

static class LeavePolicyScopeTests
{
    public static async Task RunAsync()
    {
        var factory = new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false)).Options);
        var org = 10;
        var allowed = true;
        var actor = new UserContextDto { UserId = 1, IndividualId = 100 };
        var access = Proxy.For<IUserAccessService>((m, a) => m switch {
            "RequireContextAsync" => Task.FromResult(actor),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(org),
            "HasOrganisationAccessAsync" => Task.FromResult(allowed && (int)a[0]! == org),
            _ => throw new Exception("Unexpected legacy access method: " + m)
        });
        await using (var db = factory.CreateDbContext())
        {
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual", Code = "AL", IsActive = true, IsGlobal = true });
            db.LeavePolicies.AddRange(
                new LeavePolicy { LeavePolicyId = 1, LeaveTypeId = 1, OrganisationId = 10, Name = "Current tenant", IsActive = true },
                new LeavePolicy { LeavePolicyId = 2, LeaveTypeId = 1, OrganisationId = 20, Name = "Other tenant", IsActive = true });
            // No UserOrganisation row: organisation IDs must never be looked up as membership IDs.
            await db.SaveChangesAsync();
        }
        var service = new LeavePolicyService(factory, access, NullLogger<LeavePolicyService>.Instance, null!);
        var policies = await service.GetOrganisationPoliciesAsync();
        if (policies.Count != 1 || policies[0].LeavePolicyId != 1) throw new Exception("Current tenant policy load failed or leaked another tenant");
        if (await service.GetByIdAsync(2) != null) throw new Exception("Foreign policy was accessible");
        org = 20; actor.IsSuperAdministrator = true;
        if ((await service.GetOrganisationPoliciesAsync()).Single().LeavePolicyId != 2) throw new Exception("Selected Super Administrator organisation was ignored");
        allowed = false;
        try { await service.GetOrganisationPoliciesAsync(); throw new Exception("Inaccessible organisation was allowed"); }
        catch (UnauthorizedAccessException) { }
        allowed = true; org = 0;
        try { await service.GetOrganisationPoliciesAsync(); throw new Exception("Missing organisation was allowed"); }
        catch (UnauthorizedAccessException) { }
    }
}
