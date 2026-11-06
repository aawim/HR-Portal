using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.Items;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class ItemStaffRequestLineTests
{
    private readonly QueryFactory factory = new(ignoreTransactions: true);
    private readonly FakeAccess access = new();
    private static DateTime Today => DateTime.UtcNow.AddHours(5).Date;

    private async Task SeedBaseDataAsync()
    {
        access.Actor = new UserContextDto
        {
            UserId = 1,
            IndividualId = 999,
            BusinessEntityId = 999,
            Username = "admin_user",
            IsSuperAdministrator = true
        };
        access.Organisation = 10;
        access.Permissions.Add("Admin");

        await using var db = factory.CreateDbContext();

        var org = new Organisation
        {
            BusinessEntityID = 10,
            OrganisationName = "Test HQ"
        };
        db.Organisations.Add(org);

        var adminIndividual = new Individual
        {
            BusinessEntityId = 999,
            FirstNameEnglish = "Admin",
            LastNameEnglish = "User"
        };
        var individual = new Individual
        {
            BusinessEntityId = 100,
            FirstNameEnglish = "Jane",
            LastNameEnglish = "Doe"
        };
        db.Individuals.AddRange(adminIndividual, individual);

        var job = new Job
        {
            JobId = 50,
            OrganisationID = 10,
            IndividualID = 100,
            JobStateId = 4,
            JoinedDate = Today.AddYears(-1),
            TerminatedDate = null
        };
        db.Jobs.Add(job);

        var statePending = new RequestState { RequestStateId = 2, StateName = "Pending" };
        var stateApproved = new RequestState { RequestStateId = 4, StateName = "Approved" };
        db.RequestStates.AddRange(statePending, stateApproved);

        var reqTypeRequisition = new RequestType { RequestTypeId = 20, TypeName = HRM.Services.Contracts.ItemCatalogueServiceContracts.RequisitionTypeName };
        var reqTypeIssue = new RequestType { RequestTypeId = 21, TypeName = HRM.Services.Contracts.ItemCatalogueServiceContracts.IssueTypeName };
        db.RequestTypes.AddRange(reqTypeRequisition, reqTypeIssue);

        var cat = new ItemCategory
        {
            ItemCategoryId = 1,
            OrganisationId = 10,
            Name = "IT Hardware",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ItemCategories.Add(cat);

        var item = new Item
        {
            ItemId = 200,
            OrganisationId = 10,
            ItemCategoryId = 1,
            Name = "Dell Latitude 5420",
            Kind = "Asset",
            UnitOfMeasure = "Each",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Items.Add(item);

        var unit1 = new ItemUnit
        {
            ItemUnitId = 301,
            OrganisationId = 10,
            ItemId = 200,
            InventoryNumber = "INV-2026-001",
            SerialNumber = "SN-DELL-001",
            Status = "Available",
            Condition = "Good",
            CreatedAtUtc = DateTime.UtcNow
        };
        var unit2 = new ItemUnit
        {
            ItemUnitId = 302,
            OrganisationId = 10,
            ItemId = 200,
            InventoryNumber = "INV-2026-002",
            SerialNumber = "SN-DELL-002",
            Status = "Available",
            Condition = "Good",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ItemUnits.AddRange(unit1, unit2);

        await db.SaveChangesAsync();
    }

    private ItemCatalogueService CreateService(int nextRequestId = 901)
    {
        var requests = StubProxy.For<IRequestService>((name, args) =>
        {
            if (name == "CreateAsync")
            {
                var db = (HrmTeContext)args[0]!;
                var dto = (CreateRequestDto)args[1]!;
                var req = new Request
                {
                    RequestId = nextRequestId++,
                    RequestTypeId = dto.RequestTypeId,
                    ApplicantBusinessEntityId = dto.ApplicantBusinessEntityId,
                    OrganisationBusinessEntityID = dto.OrganisationBusinessEntityId,
                    RequestStateId = 2,
                    ReferenceNumber = $"IM-{nextRequestId}"
                };
                db.Requests.Add(req);
                return Task.FromResult(req);
            }
            throw new InvalidOperationException(name);
        });

        var logs = StubProxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = 1 }));
        var workflow = StubProxy.For<IWorkflowEngine>((_, _) => Task.FromResult(1L));
        var approvers = StubProxy.For<IWorkflowApproverResolver>((_, _) => Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>([]));
        var contexts = StubProxy.For<IWorkflowRequestContextResolver>((_, _) => Task.FromResult(new WorkflowRequestContextDto()));

        return new ItemCatalogueService(factory, access, logs, requests, workflow, approvers, contexts);
    }

    [Fact]
    public async Task ItemStaffRequest_Saves_And_Retrieves_InventoryNumber_And_Details()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var input = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            Purpose = "Job related",
            Kind = "Issue",
            Reason = "Equipment assignment for new job role duties",
            Lines =
            [
                new()
                {
                    ItemId = 200,
                    ItemUnitId = 301,
                    ItemName = "Dell Latitude 5420",
                    InventoryNumber = "INV-2026-001",
                    SerialNumber = "SN-DELL-001",
                    Condition = "Good",
                    ExpectedReturnDate = DateOnly.FromDateTime(Today.AddMonths(12)),
                    Remarks = "Includes 65W USB-C charger and carry sleeve",
                    Quantity = 1
                }
            ]
        };

        var submitResult = await service.SubmitStaffRequestAsync(input);
        Assert.True(submitResult.Success);

        var list = await service.GetStaffRequestsAsync(10, 100);
        Assert.Single(list);

        var item = list[0];
        Assert.Equal("Job related", item.Purpose);
        Assert.Equal("Issue", item.Kind);
        Assert.Single(item.Lines);

        var line = item.Lines[0];
        Assert.Equal("Dell Latitude 5420", line.ItemName);
        Assert.Equal("INV-2026-001", line.InventoryNumber);
        Assert.Equal("SN-DELL-001", line.SerialNumber);
        Assert.Equal("Good", line.Condition);
        Assert.Equal(DateOnly.FromDateTime(Today.AddMonths(12)), line.ExpectedReturnDate);
        Assert.Equal("Includes 65W USB-C charger and carry sleeve", line.Remarks);
        Assert.Equal(1, line.Quantity);
    }

    [Fact]
    public async Task ItemStaffRequest_Allows_Multiple_Lines_With_Distinct_InventoryNumbers()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var input = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            Purpose = "Job related",
            Kind = "Issue",
            Reason = "Issuing dual laptops with separate inventory numbers",
            Lines =
            [
                new()
                {
                    ItemId = 200,
                    ItemUnitId = 301,
                    ItemName = "Dell Latitude 5420",
                    InventoryNumber = "INV-2026-001",
                    SerialNumber = "SN-DELL-001",
                    Condition = "Good",
                    Quantity = 1
                },
                new()
                {
                    ItemId = 200,
                    ItemUnitId = 302,
                    ItemName = "Dell Latitude 5420",
                    InventoryNumber = "INV-2026-002",
                    SerialNumber = "SN-DELL-002",
                    Condition = "Good",
                    Quantity = 1
                }
            ]
        };

        var submitResult = await service.SubmitStaffRequestAsync(input);
        Assert.True(submitResult.Success);

        var list = await service.GetStaffRequestsAsync(10, 100);
        Assert.Single(list);
        Assert.Equal(2, list[0].Lines.Count);
        Assert.Contains(list[0].Lines, l => l.InventoryNumber == "INV-2026-001");
        Assert.Contains(list[0].Lines, l => l.InventoryNumber == "INV-2026-002");
    }

    [Fact]
    public async Task ItemStaffRequest_MarkReturned_Updates_ReturnedAtUtc_And_UnitStatus()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var input = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            Purpose = "Job related",
            Kind = "Issue",
            Reason = "Laptop assignment for project",
            Lines =
            [
                new()
                {
                    ItemId = 200,
                    ItemUnitId = 301,
                    ItemName = "Dell Latitude 5420",
                    InventoryNumber = "INV-2026-001",
                    Quantity = 1
                }
            ]
        };

        await service.SubmitStaffRequestAsync(input);

        await using var db = factory.CreateDbContext();
        var req = await db.ItemStaffRequests.Include(r => r.Lines).FirstAsync();
        req.Status = "InUse";
        req.IssuedAtUtc = DateTime.UtcNow;
        req.AcceptedAtUtc = DateTime.UtcNow;
        var unit = await db.ItemUnits.SingleAsync(u => u.ItemUnitId == 301);
        unit.Status = "Issued";
        unit.IssuedToIndividualId = 100;
        await db.SaveChangesAsync();

        var returnResult = await service.ActStaffRequestAsync(req.RequestId, 2, "ConfirmReturn:Good", "Staff completed project, handed back equipment.");
        Assert.True(returnResult.Success);

        await using var dbVerify = factory.CreateDbContext();
        var verifiedReq = await dbVerify.ItemStaffRequests.FirstAsync();
        Assert.NotNull(verifiedReq.ReturnedAtUtc);

        var verifiedUnit = await dbVerify.ItemUnits.SingleAsync(u => u.ItemUnitId == 301);
        Assert.Equal("Available", verifiedUnit.Status);
        Assert.Equal("Good", verifiedUnit.Condition);
        Assert.Null(verifiedUnit.IssuedToIndividualId);
    }

    [Fact]
    public async Task ItemStaffRequest_WhenHrAssignsToThemselves_FailsWithSelfAssignError()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var input = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 999, // actor.Person
            Purpose = "Job related",
            Kind = "Issue",
            Reason = "Self-assigning laptop",
            Lines =
            [
                new()
                {
                    ItemId = 200,
                    ItemName = "Dell Latitude 5420",
                    Quantity = 1
                }
            ]
        };

        var result = await service.SubmitStaffRequestAsync(input);
        Assert.False(result.Success);
        Assert.Equal("Staff raise item requests for themselves. HR assigns job-related items from the staff workspace.", result.Message);
    }

    [Fact]
    public async Task ItemStaffRequest_WhenHrAssignsCommonPersonalItemsToStaff_PreservesCommonPurpose()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var input = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100, // another staff
            Purpose = "Common",
            Kind = "Issue",
            Reason = "Issuing stationary and consumables to staff",
            Lines =
            [
                new()
                {
                    ItemName = "Spiral Notebook A4",
                    Quantity = 3
                }
            ]
        };

        var result = await service.SubmitStaffRequestAsync(input);
        Assert.True(result.Success, result.Message);

        var list = await service.GetStaffRequestsAsync(10, 100);
        var item = Assert.Single(list);
        Assert.Equal("Common", item.Purpose);
        Assert.Equal("Issue", item.Kind);
        Assert.Equal(3, item.Lines[0].Quantity);
    }

    [Fact]
    public async Task ItemStaffRequest_WhenHrRaisesRequisitionForStaff_Succeeds()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var input = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100, // another staff
            Purpose = "Common",
            Kind = "Requisition",
            Reason = "HR requisitioning office supplies on behalf of staff",
            Lines =
            [
                new()
                {
                    ItemName = "Sticky Notes Pack",
                    Quantity = 5
                }
            ]
        };

        var result = await service.SubmitStaffRequestAsync(input);
        Assert.True(result.Success, result.Message);

        var list = await service.GetStaffRequestsAsync(10, 100);
        var item = Assert.Single(list);
        Assert.Equal("Common", item.Purpose);
        Assert.Equal("Requisition", item.Kind);
    }
}
