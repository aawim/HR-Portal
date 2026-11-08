using HRM.DTOs;
using HRM.Enum;
using HRM.Services;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class WorkspaceEmployeeSearchTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Employee_search_excludes_ended_employment_including_for_superadministrators(bool super)
    {
        var factory = new QueryFactory();
        var access = new FakeAccess();
        access.Actor!.IsSuperAdministrator = super;
        var today = FormerStaffPolicy.Today;
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.AddRange(
                new() { BusinessEntityId = 820, FirstNameEnglish = "Ahmed", LastNameEnglish = "Farish" },
                new() { BusinessEntityId = 821, FirstNameEnglish = "Ahmed", LastNameEnglish = "Active" },
                new() { BusinessEntityId = 822, FirstNameEnglish = "Ahmed", LastNameEnglish = "EndedPosition" });
            db.OrganisationStructures.Add(new() { OrganisationStructureId = 1, Name = "Department" });
            db.Jobs.AddRange(
                new() { JobId = 918, IndividualID = 820, OrganisationID = 10, OrganisationStructureId = 1, JobStateId = 8, JoinedDate = today.AddMonths(-1), TerminatedDate = today },
                new() { JobId = 919, IndividualID = 821, OrganisationID = 10, OrganisationStructureId = 1, JobStateId = 4, JoinedDate = today.AddMonths(-1) },
                new() { JobId = 920, IndividualID = 822, OrganisationID = 10, OrganisationStructureId = 1, JobStateId = 4, JoinedDate = today.AddMonths(-1) });
            db.JobPositions.Add(new() { JobPositionId = 1, JobId = 920, JobPositionStateId = 4, FromDate = today.AddMonths(-1), ToDate = today });
            await db.SaveChangesAsync();
        }
        var service = new WorkspaceDirectoryService(factory, access);
        foreach (var staffOnly in new[] { false, true })
        {
            var page = await service.SearchAsync(new WorkspaceSearchFilter("Ahmed", StaffOnly: staffOnly));
            Assert.Equal(821, Assert.Single(page.Staff).Id);
        }
        var recent = new WorkspaceRecentSearchService(factory, access, null!);
        Assert.Null(await recent.ResolveAsync(WorkspaceEntityType.Staff, 820));
        Assert.Null(await recent.ResolveAsync(WorkspaceEntityType.Staff, 822));
        Assert.Equal(821, (await recent.ResolveAsync(WorkspaceEntityType.Staff, 821))!.Id);
        await using var check = factory.CreateDbContext();
        Assert.True(await FormerStaffPolicy.IsFormerAsync(check, 820, 10));
    }
}
