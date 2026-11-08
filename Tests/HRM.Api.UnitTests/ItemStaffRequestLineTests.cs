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

        var userOrg = new UserOrganisation
        {
            UserOrganisationID = 1,
            BusinessEntityID = 10,
            Organisation = org
        };
        db.UserOrganisations.Add(userOrg);

        var adminUser = new User
        {
            UserId = 1,
            BusinessEntityID = 999,
            Username = "admin_user",
            IsActive = true,
            LocalUserStateId = 1,
            AdminPortalAccessEnabled = true,
            UserOrganisation = userOrg
        };
        var staffUser = new User
        {
            UserId = 2,
            BusinessEntityID = 100,
            Username = "jane_doe",
            IsActive = true,
            LocalUserStateId = 1,
            AdminPortalAccessEnabled = false,
            UserOrganisation = userOrg
        };
        db.Users.AddRange(adminUser, staffUser);

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
    private void SetActorStaff(int individualId = 100, int userId = 2)
    {
        access.Actor = new UserContextDto
        {
            UserId = userId,
            IndividualId = individualId,
            BusinessEntityId = individualId,
            Username = "jane_doe",
            IsSuperAdministrator = false,
            IsHRStaff = false,
            IsSupervisor = false
        };
        access.Organisation = 10;
        access.Permissions.Clear();
    }

    private void SetActorAdmin(int individualId = 999, int userId = 1)
    {
        access.Actor = new UserContextDto
        {
            UserId = userId,
            IndividualId = individualId,
            BusinessEntityId = individualId,
            Username = "admin_user",
            IsSuperAdministrator = true
        };
        access.Organisation = 10;
        access.Permissions.Clear();
        access.Permissions.Add("Admin");
    }

    [Fact]
    public async Task PersonalItemRequisition_StaffSubmits_ApproverNotified_SenderNotNotified()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        // Staff member Jane Doe (individual 100, user 2) submits personal items requisition
        SetActorStaff(100, 2);

        var input = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            Purpose = "Common",
            Kind = "Requisition",
            Reason = "Need office consumables: notebooks and ballpoint pens",
            Lines =
            [
                new() { ItemName = "Ballpoint Pens (Box of 10)", Quantity = 2 },
                new() { ItemName = "A4 Lined Notebook", Quantity = 5 }
            ]
        };

        var result = await service.SubmitStaffRequestAsync(input);
        Assert.True(result.Success, result.Message);

        await using var db = factory.CreateDbContext();
        var req = await db.ItemStaffRequests.Include(r => r.Lines).OrderByDescending(r => r.RequestId).FirstOrDefaultAsync();
        Assert.NotNull(req);
        Assert.Equal("Submitted", req.Status);
        Assert.Equal("Common", req.Purpose);
        Assert.Equal("Requisition", req.Kind);

        // Verification of Notifications:
        // 1. Approver (UserId = 1) MUST receive StaffNotification & RequestNotification
        var approverStaffNotif = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == 1);
        Assert.NotNull(approverStaffNotif);
        Assert.Equal("ItemRequest", approverStaffNotif.Kind);
        Assert.Contains("Requisition request", approverStaffNotif.Message);

        var approverReqNotif = await db.RequestNotifications.FirstOrDefaultAsync(n => n.UserId == 1);
        Assert.NotNull(approverReqNotif);
        Assert.Equal("ActionRequired", approverReqNotif.NotificationKind);

        // 2. Sender (UserId = 2, IndividualId = 100) MUST NEVER receive notification on submit
        var senderStaffNotifs = await db.StaffNotifications.Where(n => n.UserId == 2).ToListAsync();
        Assert.Empty(senderStaffNotifs);

        var senderReqNotifs = await db.RequestNotifications.Where(n => n.UserId == 2).ToListAsync();
        Assert.Empty(senderReqNotifs);
    }

    [Fact]
    public async Task JobRelatedUpgradeRequest_StaffSubmits_ReplacesExisting_DoesNotCreateAssetIssuancePrematurely()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        // Step 1: Seed an existing job-related laptop currently in use by staff Jane Doe (Id 801 to avoid ID 901 collision)
        await using (var db = factory.CreateDbContext())
        {
            var initialReq = new ItemStaffRequest
            {
                RequestId = 801,
                OrganisationId = 10,
                IndividualId = 100,
                JobId = 50,
                Purpose = "Job related",
                Kind = "Issue",
                Reason = "Original issued laptop",
                Status = "InUse",
                Quantity = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-6),
                AcceptedAtUtc = DateTime.UtcNow.AddMonths(-6),
                Lines =
                [
                    new ItemStaffRequestLine
                    {
                        ItemId = 200,
                        ItemUnitId = 301,
                        ItemName = "Dell Latitude 5420",
                        InventoryNumber = "INV-2026-001",
                        Quantity = 1
                    }
                ]
            };
            db.ItemStaffRequests.Add(initialReq);

            var unit = await db.ItemUnits.SingleAsync(u => u.ItemUnitId == 301);
            unit.Status = "Issued";
            unit.IssuedToIndividualId = 100;
            await db.SaveChangesAsync();
        }

        // Step 2: Staff member Jane Doe submits an Upgrade request
        SetActorStaff(100, 2);

        var upgradeInput = new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            Purpose = "Job related",
            Kind = "Upgrade",
            ReplacesRequestId = 801,
            Reason = "Requesting upgrade to 32GB RAM model for heavy Docker and development workloads",
            Lines =
            [
                new()
                {
                    ItemName = "Dell Precision 5570 32GB",
                    Quantity = 1
                }
            ]
        };

        var submitResult = await service.SubmitStaffRequestAsync(upgradeInput);
        Assert.True(submitResult.Success, submitResult.Message);

        await using (var db = factory.CreateDbContext())
        {
            var upgradeReq = await db.ItemStaffRequests.FirstOrDefaultAsync(r => r.Kind == "Upgrade");
            Assert.NotNull(upgradeReq);
            Assert.Equal("Submitted", upgradeReq.Status);
            Assert.Equal(801, upgradeReq.ReplacesRequestId);

            // Asset Service Request (Assets) must be synchronized
            var assetReq = await db.AssetServiceRequests.FirstOrDefaultAsync(r => r.RequestId == upgradeReq.RequestId);
            Assert.NotNull(assetReq);
            Assert.Equal("HardwareUpgrade", assetReq.RequestType);

            // CRITICAL CHECK: Asset issuance must NOT be created on upgrade request submission
            var issuance = await db.AssetIssuances.FirstOrDefaultAsync(i => i.RequestId == upgradeReq.RequestId);
            Assert.Null(issuance);

            // SENDER NOTIFICATION CHECK: Jane Doe (user 2) must NOT receive a notification on submission
            var janeNotif = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == 2);
            Assert.Null(janeNotif);

            // APPROVER NOTIFICATION CHECK: Admin (user 1) MUST receive a notification
            var adminNotif = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == 1);
            Assert.NotNull(adminNotif);
            Assert.Contains("upgrade request", adminNotif.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task JobRelatedUpgradeRequest_HrApproves_And_StaffAccepts_EndToEndLifecycle()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        // Step 1: Initial item in use
        await using (var db = factory.CreateDbContext())
        {
            var initialReq = new ItemStaffRequest
            {
                RequestId = 801,
                OrganisationId = 10,
                IndividualId = 100,
                JobId = 50,
                Purpose = "Job related",
                Kind = "Issue",
                Reason = "Original laptop",
                Status = "InUse",
                Quantity = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-6),
                AcceptedAtUtc = DateTime.UtcNow.AddMonths(-6),
                Lines =
                [
                    new ItemStaffRequestLine
                    {
                        ItemId = 200,
                        ItemUnitId = 301,
                        ItemName = "Dell Latitude 5420",
                        InventoryNumber = "INV-2026-001",
                        Quantity = 1
                    }
                ]
            };
            db.ItemStaffRequests.Add(initialReq);
            await db.SaveChangesAsync();
        }

        // Step 2: Staff submits upgrade request
        SetActorStaff(100, 2);
        var submitResult = await service.SubmitStaffRequestAsync(new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            Purpose = "Job related",
            Kind = "Upgrade",
            ReplacesRequestId = 801,
            Reason = "Need faster machine",
            Lines = [ new() { ItemId = 200, ItemUnitId = 302, ItemName = "Dell Latitude 5420", InventoryNumber = "INV-2026-002", Quantity = 1 } ]
        });
        Assert.True(submitResult.Success);

        int upgradeRequestId;
        await using (var db = factory.CreateDbContext())
        {
            var req = await db.ItemStaffRequests.FirstAsync(r => r.Kind == "Upgrade");
            upgradeRequestId = req.RequestId;
            // Clear notifications from submission to isolate approval notifications
            db.StaffNotifications.RemoveRange(db.StaffNotifications);
            db.RequestNotifications.RemoveRange(db.RequestNotifications);
            await db.SaveChangesAsync();
        }

        // Step 3: HR Admin approves the upgrade
        SetActorAdmin(999, 1);
        var approveResult = await service.ActStaffRequestAsync(upgradeRequestId, 2, "Approve", "Upgrade approved and equipment allocated.");
        Assert.True(approveResult.Success, approveResult.Message);

        await using (var db = factory.CreateDbContext())
        {
            var upgradeReq = await db.ItemStaffRequests.Include(r => r.Lines).FirstAsync(r => r.RequestId == upgradeRequestId);
            Assert.Equal("InUse", upgradeReq.Status);

            // Hardware upgrade preserves custody: no issuance/handover is created
            var issuance = await db.AssetIssuances.FirstOrDefaultAsync(i => i.RequestId == upgradeRequestId);
            Assert.Null(issuance);

            // Asset Service Request is marked Approved
            var assetReq = await db.AssetServiceRequests.FirstOrDefaultAsync(r => r.RequestId == upgradeRequestId);
            Assert.NotNull(assetReq);
            Assert.Equal("Approved", assetReq.Status);

            // Original item remains InUse (not returned)
            var oldReq = await db.ItemStaffRequests.FirstAsync(r => r.RequestId == 801);
            Assert.Equal("InUse", oldReq.Status);

            // Staff member (user 2) must be notified of approval
            var staffNotif = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == 2);
            Assert.NotNull(staffNotif);
            Assert.Contains("approved", staffNotif.Message, StringComparison.OrdinalIgnoreCase);

            // HR Admin (user 1, who approved) must NOT be notified of their own action
            var adminNotif = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == 1);
            Assert.Null(adminNotif);
        }
    }

    [Fact]
    public async Task ItemRequest_HrRejection_NotifiesStaff_WithReason_DoesNotNotifyAdmin()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        // Staff submits requisition
        SetActorStaff(100, 2);
        await service.SubmitStaffRequestAsync(new ItemStaffRequestInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            Purpose = "Common",
            Kind = "Requisition",
            Reason = "Special ergonomic mouse",
            Lines = [ new() { ItemName = "Vertical Ergonomic Mouse", Quantity = 1 } ]
        });

        int reqId;
        await using (var db = factory.CreateDbContext())
        {
            var req = await db.ItemStaffRequests.FirstAsync();
            reqId = req.RequestId;
            db.StaffNotifications.RemoveRange(db.StaffNotifications);
            await db.SaveChangesAsync();
        }

        // HR Admin rejects request with a reason
        SetActorAdmin(999, 1);
        var rejectResult = await service.ActStaffRequestAsync(reqId, 2, "Reject", "Requested model is out of stock. Please select standard mouse.");
        Assert.True(rejectResult.Success);

        await using (var db = factory.CreateDbContext())
        {
            var req = await db.ItemStaffRequests.FirstAsync(r => r.RequestId == reqId);
            Assert.Equal("Rejected", req.Status);

            // Staff member (user 2) must be notified with rejection comment
            var staffNotif = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == 2);
            Assert.NotNull(staffNotif);
            Assert.Contains("rejected", staffNotif.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("out of stock", staffNotif.Message, StringComparison.OrdinalIgnoreCase);

            // HR Admin (user 1) must NOT be notified
            var adminNotif = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == 1);
            Assert.Null(adminNotif);
        }
    }

    [Fact]
    public async Task ItemRequests_SupervisorAndHrRoles_CanManageItems()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        // Verify HR Staff role has manage permissions
        access.Actor = new UserContextDto
        {
            UserId = 10,
            IndividualId = 555,
            BusinessEntityId = 555,
            Username = "hr_officer",
            IsSuperAdministrator = false,
            IsHRStaff = true
        };
        access.Organisation = 10;
        access.Permissions.Clear();

        var canManage = await service.CanManageStaffItemsAsync();
        Assert.True(canManage);

        // Verify Supervisor role has manage permissions
        access.Actor = new UserContextDto
        {
            UserId = 11,
            IndividualId = 556,
            BusinessEntityId = 556,
            Username = "team_lead",
            IsSuperAdministrator = false,
            IsSupervisor = true
        };
        Assert.True(await service.CanManageStaffItemsAsync());
    }
}