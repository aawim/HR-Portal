using System.Security.Cryptography;
using System.Text;
using HRM.Authorization;
using HRM.DTOs;
using HRM.DTOs.Assets;
using HRM.DTOs.UserContext;
using HRM.Enums;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class StaffAssetManagementTests
{
    private readonly QueryFactory factory = new(ignoreTransactions: true);
    private readonly FakeAccess access = new();
    private static DateTime Today => DateTime.UtcNow.AddHours(5).Date;

    private StaffAssetService CreateService() => new(
        factory,
        access,
        StubProxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = 1 })),
        null);

    private async Task SeedBaseDataAsync()
    {
        access.Actor = new UserContextDto
        {
            UserId = 1,
            IndividualId = 100,
            BusinessEntityId = 100,
            Username = "admin_officer",
            IsSuperAdministrator = true
        };
        access.Organisation = 10;
        access.Permissions.Add(Permissions.AssetView);
        access.Permissions.Add(Permissions.AssetInventoryManage);
        access.Permissions.Add(Permissions.AssetIssuanceInitiate);
        access.Permissions.Add(Permissions.AssetAcknowledge);
        access.Permissions.Add(Permissions.AssetHrApprove);
        access.Permissions.Add(Permissions.AssetReturnInspect);
        access.Permissions.Add(Permissions.AssetAuditView);
        access.Permissions.Add(Permissions.AssetExportRestrictedPdf);

        await using var db = factory.CreateDbContext();

        var org = new Organisation
        {
            BusinessEntityID = 10,
            OrganisationName = "Test Ministry"
        };
        db.Organisations.Add(org);

        var dept = new OrganisationStructure
        {
            OrganisationStructureId = 5,
            OrganisationBusinessEntityID = 10,
            Name = "Information Technology"
        };
        db.OrganisationStructures.Add(dept);

        var dept2 = new OrganisationStructure
        {
            OrganisationStructureId = 6,
            OrganisationBusinessEntityID = 10,
            Name = "Human Resources"
        };
        db.OrganisationStructures.Add(dept2);

        var individual = new Individual
        {
            BusinessEntityId = 100,
            FirstNameEnglish = "Ahmed",
            LastNameEnglish = "Ali"
        };
        db.Individuals.Add(individual);

        var otherIndividual = new Individual
        {
            BusinessEntityId = 200,
            FirstNameEnglish = "Mariyam",
            LastNameEnglish = "Hussain"
        };
        db.Individuals.Add(otherIndividual);

        var job = new Job
        {
            JobId = 50,
            IndividualID = 100,
            OrganisationID = 10,
            OrganisationStructureId = 5,
            Sapnumber = "EMP-00100"
        };
        db.Jobs.Add(job);

        var catLaptops = new AssetCategory
        {
            AssetCategoryId = 1,
            Name = "Laptops",
            RequiresSerialNumber = true,
            RequiresReturn = true,
            IsActive = true
        };
        var catMonitors = new AssetCategory
        {
            AssetCategoryId = 2,
            Name = "Monitors",
            RequiresSerialNumber = true,
            RequiresReturn = true,
            IsActive = true
        };
        db.AssetCategories.AddRange(catLaptops, catMonitors);

        var condBrandNew = new AssetCondition { AssetConditionId = 1, Name = "Brand New", SortOrder = 1, IsActive = true };
        var condGood = new AssetCondition { AssetConditionId = 2, Name = "Good", SortOrder = 2, IsActive = true };
        var condFair = new AssetCondition { AssetConditionId = 3, Name = "Fair", SortOrder = 3, IsActive = true };
        var condDamaged = new AssetCondition { AssetConditionId = 4, Name = "Damaged", SortOrder = 4, IsActive = true };
        db.AssetConditions.AddRange(condBrandNew, condGood, condFair, condDamaged);

        var asset1 = new AssetItem
        {
            AssetItemId = 1001,
            CategoryId = 1,
            AssetCode = "LAP-001",
            SerialNumber = "SN-LAP-001",
            Name = "Dell Latitude 5520",
            Brand = "Dell",
            Model = "Latitude 5520",
            OwnerOrganisationId = 10,
            OwnerStructureId = 5,
            Location = "IT Dept",
            Status = AssetInventoryStatuses.Available,
            CurrentConditionId = 1,
            DefaultAccessories = "65W USB-C Charger, Sleeve",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var asset2 = new AssetItem
        {
            AssetItemId = 1002,
            CategoryId = 2,
            AssetCode = "MON-001",
            SerialNumber = "SN-MON-001",
            Name = "Dell 27-inch Monitor",
            Brand = "Dell",
            Model = "P2720D",
            OwnerOrganisationId = 10,
            OwnerStructureId = 5,
            Location = "IT Dept",
            Status = AssetInventoryStatuses.Available,
            CurrentConditionId = 1,
            DefaultAccessories = "HDMI Cable, Power Cable",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var asset3 = new AssetItem
        {
            AssetItemId = 1003,
            CategoryId = 1,
            AssetCode = "LAP-002",
            SerialNumber = "SN-LAP-002",
            Name = "ThinkPad T14",
            Brand = "Lenovo",
            Model = "T14 Gen 2",
            OwnerOrganisationId = 10,
            OwnerStructureId = 5,
            Location = "IT Dept",
            Status = AssetInventoryStatuses.Available,
            CurrentConditionId = 1,
            DefaultAccessories = "Charger",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.AssetItems.AddRange(asset1, asset2, asset3);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Dashboard_Loads_KPIs_And_Assets_Accurately()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var dashboard = await service.LoadDashboardAsync();

        Assert.NotNull(dashboard);
        Assert.Equal(3, dashboard.TotalAssetsCount);
        Assert.Equal(3, dashboard.AvailableAssetsCount);
        Assert.Equal(0, dashboard.AssignedAssetsCount);
        Assert.Equal(3, dashboard.Assets.Count);
        Assert.Equal(2, dashboard.Categories.Count);
        Assert.Equal(4, dashboard.Conditions.Count);
    }

    [Fact]
    public async Task Issuance_Creation_Reserves_Assets_And_Prevents_Double_Reservation()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            StructureId = 5,
            Reason = "New Onboarding Equipment",
            IssueDate = Today,
            ReserveImmediately = true,
            VerifyHandoverImmediately = false,
            Lines =
            [
                new() { AssetItemId = 1001, ConditionAtIssueId = 1, Accessories = "Charger" },
                new() { AssetItemId = 1002, ConditionAtIssueId = 1, Accessories = "HDMI" }
            ]
        };

        var res = await service.SaveIssuanceDraftAsync(draft);
        Assert.True(res.Success, res.Message);

        await using var db = factory.CreateDbContext();
        var a1 = await db.AssetItems.FindAsync(1001);
        var a2 = await db.AssetItems.FindAsync(1002);
        Assert.Equal(AssetInventoryStatuses.Reserved, a1!.Status);
        Assert.Equal(AssetInventoryStatuses.Reserved, a2!.Status);

        // Attempting to reserve asset 1001 again in a second issuance must fail
        var secondDraft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            Reason = "Second attempt with same asset",
            IssueDate = Today,
            ReserveImmediately = true,
            Lines = [new() { AssetItemId = 1001, ConditionAtIssueId = 1 }]
        };

        var secondRes = await service.SaveIssuanceDraftAsync(secondDraft);
        Assert.False(secondRes.Success);
        Assert.Contains("not Available for reservation", secondRes.Message);
    }

    [Fact]
    public async Task Handover_Verification_Moves_Lines_To_HandedOver_Before_Staff_Decision()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            Reason = "Staff Equipment",
            IssueDate = Today,
            VerifyHandoverImmediately = false,
            Lines = [new() { AssetItemId = 1001, ConditionAtIssueId = 1, Accessories = "Charger" }]
        };

        var res = await service.SaveIssuanceDraftAsync(draft);
        Assert.True(res.Success);

        await using var db1 = factory.CreateDbContext();
        var issuance = await db1.AssetIssuances.Include(i => i.Lines).FirstAsync();
        var line = issuance.Lines.First();
        Assert.Equal(AssetIssuanceStatuses.Draft, issuance.Status);
        Assert.Equal(AssetIssuanceLineStatuses.Proposed, line.Status);

        // Staff tries to submit decision while line is Proposed -> must fail
        var batchPremature = new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions = [new() { AssetIssuanceItemId = line.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Accepted }]
        };
        var prematureRes = await service.SubmitStaffDecisionsAsync(batchPremature);
        Assert.False(prematureRes.Success);
        Assert.Contains("awaiting staff acknowledgement", prematureRes.Message);

        // Perform physical handover verification
        var handoverRes = await service.VerifyHandoverAsync(new AssetHandoverInput
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            Lines = [new() { AssetIssuanceItemId = line.AssetIssuanceItemId, ConditionAtIssueId = 1, Accessories = "Charger + Bag" }]
        });
        Assert.True(handoverRes.Success);

        await using var db2 = factory.CreateDbContext();
        var updatedIssuance = await db2.AssetIssuances.Include(i => i.Lines).FirstAsync();
        var updatedLine = updatedIssuance.Lines.First();
        var updatedAsset = await db2.AssetItems.FindAsync(1001);

        Assert.Equal(AssetIssuanceStatuses.PendingStaffAcknowledgement, updatedIssuance.Status);
        Assert.Equal(AssetIssuanceLineStatuses.HandedOver, updatedLine.Status);
        Assert.Equal(AssetInventoryStatuses.PendingHandover, updatedAsset!.Status);
        Assert.NotNull(updatedLine.HandedOverAtUtc);
        Assert.Equal("Charger + Bag", updatedLine.Accessories);
    }

    [Fact]
    public async Task Staff_Can_Accept_One_Line_And_Reject_Another_With_Mandatory_Remarks()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            Reason = "Dual Gear",
            IssueDate = Today,
            VerifyHandoverImmediately = true,
            Lines =
            [
                new() { AssetItemId = 1001, ConditionAtIssueId = 1, Accessories = "Laptop Kit" },
                new() { AssetItemId = 1002, ConditionAtIssueId = 1, Accessories = "Monitor Kit" }
            ]
        };

        var createRes = await service.SaveIssuanceDraftAsync(draft);
        Assert.True(createRes.Success);

        await using var db1 = factory.CreateDbContext();
        var issuance = await db1.AssetIssuances.Include(i => i.Lines).FirstAsync();
        var line1 = issuance.Lines.First(l => l.AssetItemId == 1001);
        var line2 = issuance.Lines.First(l => l.AssetItemId == 1002);

        // Attempting to reject line 2 without remarks must fail validation
        var batchNoRemarks = new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions =
            [
                new() { AssetIssuanceItemId = line1.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Accepted },
                new() { AssetIssuanceItemId = line2.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Rejected, RejectionReasonCode = AssetRejectionReasons.DamagedOrDefective, Remarks = "   " }
            ]
        };
        var failRes = await service.SubmitStaffDecisionsAsync(batchNoRemarks);
        Assert.False(failRes.Success);
        Assert.Contains("remarks", failRes.Message, StringComparison.OrdinalIgnoreCase);

        // Submitting with valid rejection remarks succeeds
        var batchValid = new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions =
            [
                new() { AssetIssuanceItemId = line1.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Accepted, Remarks = "Received in good working order." },
                new() { AssetIssuanceItemId = line2.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Rejected, RejectionReasonCode = AssetRejectionReasons.DamagedOrDefective, Remarks = "Cracked monitor screen on bezel." }
            ]
        };

        var submitRes = await service.SubmitStaffDecisionsAsync(batchValid);
        Assert.True(submitRes.Success, submitRes.Message);

        await using var db2 = factory.CreateDbContext();
        var postIssuance = await db2.AssetIssuances.Include(i => i.Lines).FirstAsync();
        var postLine1 = postIssuance.Lines.First(l => l.AssetItemId == 1001);
        var postLine2 = postIssuance.Lines.First(l => l.AssetItemId == 1002);

        Assert.Equal(AssetIssuanceStatuses.PendingHRApproval, postIssuance.Status);
        Assert.Equal(AssetIssuanceLineStatuses.Accepted, postLine1.Status);
        Assert.Equal(AssetIssuanceLineStatuses.Rejected, postLine2.Status);

        var acks = await db2.AssetAcknowledgements.ToListAsync();
        Assert.Equal(2, acks.Count);
        Assert.Contains(acks, a => a.IssuanceItemId == line1.AssetIssuanceItemId && a.Decision == AssetStaffDecisions.Accepted);
        Assert.Contains(acks, a => a.IssuanceItemId == line2.AssetIssuanceItemId && a.Decision == AssetStaffDecisions.Rejected && a.Remarks != null && a.Remarks.Contains("Cracked"));
    }

    [Fact]
    public async Task Staff_Cannot_Submit_Decisions_For_Another_Employees_Issuance()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100, // Issued to Ahmed (100)
            JobId = 50,
            OrganisationId = 10,
            Reason = "Staff Equipment",
            IssueDate = Today,
            VerifyHandoverImmediately = true,
            Lines = [new() { AssetItemId = 1001, ConditionAtIssueId = 1 }]
        };
        await service.SaveIssuanceDraftAsync(draft);

        await using var db1 = factory.CreateDbContext();
        var line = await db1.AssetIssuanceItems.FirstAsync();

        // Switch logged in identity to Mariyam (200), not super admin
        access.Actor = new UserContextDto
        {
            UserId = 2,
            IndividualId = 200,
            BusinessEntityId = 200,
            Username = "mariyam",
            IsSuperAdministrator = false
        };

        var unauthorizedBatch = new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = line.IssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions = [new() { AssetIssuanceItemId = line.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Accepted }]
        };

        var res = await service.SubmitStaffDecisionsAsync(unauthorizedBatch);
        Assert.False(res.Success);
        Assert.Contains("your own issued assets", res.Message);
    }

    [Fact]
    public async Task HR_Approval_Creates_Effective_Dated_Active_Custody_Idempotently()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            Reason = "Dual Issuance",
            IssueDate = Today,
            VerifyHandoverImmediately = true,
            Lines =
            [
                new() { AssetItemId = 1001, ConditionAtIssueId = 1, Accessories = "Laptop" },
                new() { AssetItemId = 1002, ConditionAtIssueId = 1, Accessories = "Monitor" }
            ]
        };
        await service.SaveIssuanceDraftAsync(draft);

        await using var db1 = factory.CreateDbContext();
        var line1 = await db1.AssetIssuanceItems.FirstAsync(l => l.AssetItemId == 1001);
        var line2 = await db1.AssetIssuanceItems.FirstAsync(l => l.AssetItemId == 1002);

        // Staff accepts line 1, rejects line 2
        await service.SubmitStaffDecisionsAsync(new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = line1.IssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions =
            [
                new() { AssetIssuanceItemId = line1.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Accepted },
                new() { AssetIssuanceItemId = line2.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Rejected, RejectionReasonCode = AssetRejectionReasons.NotPhysicallyReceived, Remarks = "Not handed over" }
            ]
        });

        // HR approves accepted line 1, and closes rejected line 2
        var hrReview = new AssetHrReviewBatchInput
        {
            AssetIssuanceId = line1.IssuanceId,
            IdempotencyKey = "TEST-IDEMP-001",
            Decisions =
            [
                new() { AssetIssuanceItemId = line1.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetHrDecisionKinds.HRApproved, Comments = "Authorized" },
                new() { AssetIssuanceItemId = line2.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetHrDecisionKinds.Closed, Comments = "Closing rejected line" }
            ]
        };

        var hrRes = await service.SubmitHrReviewAsync(hrReview);
        Assert.True(hrRes.Success, hrRes.Message);

        await using var db2 = factory.CreateDbContext();
        var issuance = await db2.AssetIssuances.FindAsync(line1.IssuanceId);
        var a1 = await db2.AssetItems.FindAsync(1001);
        var a2 = await db2.AssetItems.FindAsync(1002);
        var custodies = await db2.AssetCustodies.ToListAsync();

        Assert.Equal(AssetIssuanceStatuses.PartiallyCompleted, issuance!.Status);
        Assert.Equal(AssetInventoryStatuses.Assigned, a1!.Status);
        Assert.Equal(AssetInventoryStatuses.Available, a2!.Status); // Line 2 cleared back to Available

        var singleCustody = Assert.Single(custodies);
        Assert.Equal(1001, singleCustody.AssetItemId);
        Assert.Equal(100, singleCustody.IndividualId);
        Assert.Equal(50, singleCustody.JobId);
        Assert.Equal(AssetCustodyStatuses.Active, singleCustody.Status);
        Assert.Null(singleCustody.CustodyToUtc);

        // Idempotency check: Re-submitting the exact same review must succeed and NOT create duplicate custody
        var idempotentRes = await service.SubmitHrReviewAsync(hrReview);
        Assert.True(idempotentRes.Success);

        await using var db3 = factory.CreateDbContext();
        var custodiesAfterRetry = await db3.AssetCustodies.ToListAsync();
        Assert.Single(custodiesAfterRetry);
    }

    [Fact]
    public async Task Line_Replacement_Increments_Version_And_Allows_Fresh_Acknowledgement()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            Reason = "Equipment to replace",
            IssueDate = Today,
            VerifyHandoverImmediately = true,
            Lines = [new() { AssetItemId = 1001, ConditionAtIssueId = 1, Accessories = "Laptop" }]
        };
        await service.SaveIssuanceDraftAsync(draft);

        await using var db1 = factory.CreateDbContext();
        var origLine = await db1.AssetIssuanceItems.FirstAsync();

        // Employee rejects the laptop
        await service.SubmitStaffDecisionsAsync(new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = origLine.IssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions =
            [
                new() { AssetIssuanceItemId = origLine.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Rejected, RejectionReasonCode = AssetRejectionReasons.DamagedOrDefective, Remarks = "Screen flicker" }
            ]
        });

        // HR initiates line replacement with asset 1003 (ThinkPad)
        var replaceInput = new AssetReplaceLineInput
        {
            AssetIssuanceId = origLine.IssuanceId,
            RejectedOrReturnedIssuanceItemId = origLine.AssetIssuanceItemId,
            ReplacementAssetItemId = 1003,
            ConditionAtIssueId = 1,
            Accessories = "Lenovo Charger",
            VerifyHandoverImmediately = true
        };

        var replaceRes = await service.ReplaceIssuanceLineAsync(replaceInput);
        Assert.True(replaceRes.Success, replaceRes.Message);

        await using var db2 = factory.CreateDbContext();
        var issuance = await db2.AssetIssuances.Include(i => i.Lines).FirstAsync();
        var lines = issuance.Lines.OrderBy(l => l.AssetIssuanceItemId).ToList();
        Assert.Equal(2, lines.Count);

        var firstLine = lines[0];
        var replacedLine = lines[1];

        Assert.Equal(AssetIssuanceLineStatuses.Closed, firstLine.Status);
        Assert.Equal(1, firstLine.Version);

        Assert.Equal(1003, replacedLine.AssetItemId);
        Assert.Equal(2, replacedLine.Version);
        Assert.Equal(AssetIssuanceLineStatuses.HandedOver, replacedLine.Status);
        Assert.Equal(AssetIssuanceStatuses.PendingStaffAcknowledgement, issuance.Status);

        // Employee can now accept the replacement line
        var acceptReplacement = new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = issuance.AssetIssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions =
            [
                new() { AssetIssuanceItemId = replacedLine.AssetIssuanceItemId, ExpectedVersion = 2, Decision = AssetStaffDecisions.Accepted, Remarks = "Lenovo laptop is working great." }
            ]
        };
        var accRes = await service.SubmitStaffDecisionsAsync(acceptReplacement);
        Assert.True(accRes.Success, accRes.Message);
    }

    [Fact]
    public async Task Return_Lifecycle_Closes_Custody_And_Routes_Item_According_To_Inspection()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        // Issue and approve asset 1001 into custody
        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            Reason = "Returnable Asset",
            IssueDate = Today,
            VerifyHandoverImmediately = true,
            Lines = [new() { AssetItemId = 1001, ConditionAtIssueId = 1 }]
        };
        await service.SaveIssuanceDraftAsync(draft);

        await using var db1 = factory.CreateDbContext();
        var line = await db1.AssetIssuanceItems.FirstAsync();

        await service.SubmitStaffDecisionsAsync(new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = line.IssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions = [new() { AssetIssuanceItemId = line.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Accepted }]
        });

        await service.SubmitHrReviewAsync(new AssetHrReviewBatchInput
        {
            AssetIssuanceId = line.IssuanceId,
            Decisions = [new() { AssetIssuanceItemId = line.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetHrDecisionKinds.HRApproved }]
        });

        await using var db2 = factory.CreateDbContext();
        var custody = await db2.AssetCustodies.FirstAsync();
        Assert.Equal(AssetCustodyStatuses.Active, custody.Status);

        // Employee / HR initiates return request
        var returnInput = new AssetReturnCreateInput
        {
            OrganisationId = 10,
            ReturnType = AssetReturnTypes.StandardReturn,
            Notes = "Project completion return",
            AssetCustodyIds = [custody.AssetCustodyId]
        };
        var createRetRes = await service.CreateReturnAsync(returnInput);
        Assert.True(createRetRes.Success, createRetRes.Message);

        await using var db3 = factory.CreateDbContext();
        var ret = await db3.AssetReturns.Include(r => r.Items).FirstAsync();
        var itemToInspect = ret.Items.First();
        var assetInDb = await db3.AssetItems.FindAsync(1001);
        Assert.Equal(AssetInventoryStatuses.ReturnPending, assetInDb!.Status);

        // Asset officer performs return inspection with DamagedMaintenance disposition
        var inspectInput = new AssetReturnInspectInput
        {
            AssetReturnId = ret.AssetReturnId,
            Items =
            [
                new()
                {
                    AssetReturnItemId = itemToInspect.AssetReturnItemId,
                    ConditionAtReturnId = 4, // Damaged
                    InspectionResult = AssetInspectionResults.DamagedMaintenance,
                    DamageOrLossEvidence = "Battery swelling detected",
                    MissingAccessories = "Charger missing"
                }
            ]
        };

        var inspectRes = await service.InspectReturnAsync(inspectInput);
        Assert.True(inspectRes.Success, inspectRes.Message);

        await using var db4 = factory.CreateDbContext();
        var finalCustody = await db4.AssetCustodies.FindAsync(custody.AssetCustodyId);
        var finalAsset = await db4.AssetItems.FindAsync(1001);
        var finalReturn = await db4.AssetReturns.FindAsync(ret.AssetReturnId);

        Assert.Equal(AssetCustodyStatuses.Returned, finalCustody!.Status);
        Assert.NotNull(finalCustody.CustodyToUtc);
        Assert.Equal(AssetReturnStatuses.Closed, finalReturn!.Status);
        Assert.Equal(AssetInventoryStatuses.Maintenance, finalAsset!.Status);
        Assert.Equal(4, finalAsset.CurrentConditionId);
    }

    [Fact]
    public async Task Transfer_Dispositions_Handle_Retain_And_Department_Change()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        // Create active custody for asset 1001
        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            StructureId = 5,
            Reason = "Transferable Asset",
            IssueDate = Today,
            VerifyHandoverImmediately = true,
            Lines = [new() { AssetItemId = 1001, ConditionAtIssueId = 1 }]
        };
        await service.SaveIssuanceDraftAsync(draft);

        await using var db1 = factory.CreateDbContext();
        var line = await db1.AssetIssuanceItems.FirstAsync();

        await service.SubmitStaffDecisionsAsync(new StaffAssetDecisionBatchInput
        {
            AssetIssuanceId = line.IssuanceId,
            ExplicitStatementConfirmed = true,
            Decisions = [new() { AssetIssuanceItemId = line.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetStaffDecisions.Accepted }]
        });

        await service.SubmitHrReviewAsync(new AssetHrReviewBatchInput
        {
            AssetIssuanceId = line.IssuanceId,
            Decisions = [new() { AssetIssuanceItemId = line.AssetIssuanceItemId, ExpectedVersion = 1, Decision = AssetHrDecisionKinds.HRApproved }]
        });

        await using var db2 = factory.CreateDbContext();
        var custody = await db2.AssetCustodies.FirstAsync();

        // Execute job transfer disposition: ChangeOwningDepartment to HR (Dept 6) with new Job 51
        var transferInput = new AssetTransferDispositionInput
        {
            AssetCustodyId = custody.AssetCustodyId,
            ToJobId = 51,
            ToOrganisationId = 10,
            ToStructureId = 6,
            Decision = AssetTransferDecisions.ChangeOwningDepartment,
            Remarks = "Transferred to HR department",
            ExecuteImmediately = true
        };

        var transferRes = await service.RecordTransferDispositionAsync(transferInput);
        Assert.True(transferRes.Success, transferRes.Message);

        await using var db3 = factory.CreateDbContext();
        var updatedCustody = await db3.AssetCustodies.FindAsync(custody.AssetCustodyId);
        var updatedAsset = await db3.AssetItems.FindAsync(1001);

        Assert.Equal(6, updatedCustody!.StructureId);
        Assert.Equal(6, updatedAsset!.OwnerStructureId);
        Assert.Equal(AssetCustodyStatuses.Active, updatedCustody.Status);
    }

    [Fact]
    public async Task Printable_Document_Computes_Immutable_SHA256_Hash_And_Restricted_Variant()
    {
        await SeedBaseDataAsync();
        var service = CreateService();

        var draft = new AssetIssuanceDraftInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            Reason = "Printable Record Asset",
            IssueDate = Today,
            VerifyHandoverImmediately = true,
            Lines = [new() { AssetItemId = 1001, ConditionAtIssueId = 1, Accessories = "Laptop + Adapter" }]
        };
        await service.SaveIssuanceDraftAsync(draft);

        await using var db1 = factory.CreateDbContext();
        var issuance = await db1.AssetIssuances.FirstAsync();

        // Standard Handover Record
        var standardDoc = await service.GetPrintableDocumentAsync(issuance.AssetIssuanceId, AssetDocumentVariants.StandardHandoverRecord);
        Assert.NotNull(standardDoc);
        Assert.Equal(AssetDocumentVariants.StandardHandoverRecord, standardDoc.DocumentVariant);
        Assert.False(string.IsNullOrWhiteSpace(standardDoc.Sha256Hash));
        Assert.Single(standardDoc.Lines);

        // Restricted Security Clearance Record
        var restrictedDoc = await service.GetPrintableDocumentAsync(issuance.AssetIssuanceId, AssetDocumentVariants.RestrictedSecurityRecord);
        Assert.NotNull(restrictedDoc);
        Assert.Equal(AssetDocumentVariants.RestrictedSecurityRecord, restrictedDoc.DocumentVariant);
        Assert.False(string.IsNullOrWhiteSpace(restrictedDoc.Sha256Hash));
    }
}
