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

public sealed class AssetLifecycleManagementTests
{
    private readonly QueryFactory factory = new(ignoreTransactions: true);
    private readonly FakeAccess access = new();

    private StaffAssetService CreateService() => new(
        factory,
        access,
        StubProxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = 1 })),
        null);

    private async Task SeedLifecycleBaseDataAsync()
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
        access.Permissions.Add(Permissions.AssetServiceRequest);
        access.Permissions.Add(Permissions.AssetServiceManage);
        access.Permissions.Add(Permissions.AssetTechnicianExecute);
        access.Permissions.Add(Permissions.AssetUpgradeAuthorize);
        access.Permissions.Add(Permissions.AssetClearanceAuthorize);

        await using var db = factory.CreateDbContext();

        var org = new Organisation
        {
            BusinessEntityID = 10,
            OrganisationName = "Ministry of Digital Technology"
        };
        db.Organisations.Add(org);

        var dept = new OrganisationStructure
        {
            OrganisationStructureId = 5,
            OrganisationBusinessEntityID = 10,
            Name = "IT Infrastructure"
        };
        db.OrganisationStructures.Add(dept);

        var ind1 = new Individual
        {
            BusinessEntityId = 100,
            FirstNameEnglish = "Ahmed",
            LastNameEnglish = "Ali"
        };
        var ind2 = new Individual
        {
            BusinessEntityId = 200,
            FirstNameEnglish = "Hassan",
            LastNameEnglish = "Rasheed"
        };
        db.Individuals.AddRange(ind1, ind2);

        var user1 = new User
        {
            UserId = 1,
            BusinessEntityID = 100,
            Username = "admin_officer",
            IsActive = true,
            LocalUserStateId = 1
        };
        var user2 = new User
        {
            UserId = 2,
            BusinessEntityID = 200,
            Username = "it_tech_lead",
            IsActive = true,
            LocalUserStateId = 1
        };
        db.Users.AddRange(user1, user2);

        var job1 = new Job
        {
            JobId = 50,
            IndividualID = 100,
            OrganisationID = 10,
            OrganisationStructureId = 5,
            Sapnumber = "EMP-00100",
            JobStateId = 4
        };
        var job2 = new Job
        {
            JobId = 60,
            IndividualID = 200,
            OrganisationID = 10,
            OrganisationStructureId = 5,
            Sapnumber = "EMP-00200",
            JobStateId = 4
        };
        db.Jobs.AddRange(job1, job2);

        var catLaptops = new AssetCategory
        {
            AssetCategoryId = 1,
            Code = "LAP",
            Name = "Laptops",
            RequiresSerialNumber = true,
            RequiresReturn = true,
            IsActive = true
        };
        db.AssetCategories.Add(catLaptops);

        var condBrandNew = new AssetCondition { AssetConditionId = 1, Code = "NEW", Name = "Brand New", SortOrder = 1, IsActive = true };
        var condGood = new AssetCondition { AssetConditionId = 2, Code = "GOOD", Name = "Good", SortOrder = 2, IsActive = true };
        var condDamaged = new AssetCondition { AssetConditionId = 4, Code = "DMG", Name = "Damaged", SortOrder = 4, IsActive = true };
        db.AssetConditions.AddRange(condBrandNew, condGood, condDamaged);

        // Laptop 1: Currently assigned to Ahmed Ali (Custody 500)
        var asset1 = new AssetItem
        {
            AssetItemId = 1001,
            CategoryId = 1,
            AssetCode = "LAP-001",
            SerialNumber = "SN-LAP-001",
            Name = "ThinkPad P1 Gen 5",
            Brand = "Lenovo",
            Model = "ThinkPad P1",
            OwnerOrganisationId = 10,
            OwnerStructureId = 5,
            Location = "HQ Floor 3",
            Status = AssetInventoryStatuses.Assigned,
            CurrentConditionId = 2,
            DefaultAccessories = "135W Slim Tip Charger, Sleeve",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Laptop 2: In Available pool (can be used as temporary loaner)
        var asset2 = new AssetItem
        {
            AssetItemId = 1002,
            CategoryId = 1,
            AssetCode = "LAP-002",
            SerialNumber = "SN-LAP-002",
            Name = "ThinkPad T14 Gen 3 (Loaner)",
            Brand = "Lenovo",
            Model = "ThinkPad T14",
            OwnerOrganisationId = 10,
            OwnerStructureId = 5,
            Location = "IT Store Bin A2",
            Status = AssetInventoryStatuses.Available,
            CurrentConditionId = 1,
            DefaultAccessories = "65W USB-C Charger",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Laptop 3: In Available pool (can be used as replacement device)
        var asset3 = new AssetItem
        {
            AssetItemId = 1003,
            CategoryId = 1,
            AssetCode = "LAP-003",
            SerialNumber = "SN-LAP-003",
            Name = "MacBook Pro 16 M3 Pro",
            Brand = "Apple",
            Model = "MacBook Pro 16",
            OwnerOrganisationId = 10,
            OwnerStructureId = 5,
            Location = "IT Store Bin B1",
            Status = AssetInventoryStatuses.Available,
            CurrentConditionId = 1,
            DefaultAccessories = "140W MagSafe Charger",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.AssetItems.AddRange(asset1, asset2, asset3);

        var activeCustody = new AssetCustody
        {
            AssetCustodyId = 500,
            AssetItemId = 1001,
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            CustodyFromUtc = DateTime.UtcNow.AddDays(-60),
            Status = AssetCustodyStatuses.Active,
            IdempotencyKey = "INIT-CUSTODY-500"
        };
        db.AssetCustodies.Add(activeCustody);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ServiceRequest_Creation_And_Technician_Assignment_Generates_WorkOrder()
    {
        await SeedLifecycleBaseDataAsync();
        var service = CreateService();

        var requestInput = new AssetServiceRequestCreateInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            RequestType = AssetServiceRequestTypes.FaultCorrective,
            Priority = "High",
            Title = "Display Flickering and Thermal Throttling",
            Description = "Screen flickers under high GPU load; laptop shuts down unexpectedly.",
            Items =
            [
                new AssetServiceRequestItemInput
                {
                    AssetItemId = 1001,
                    AssetCustodyId = 500,
                    ItemRequestType = AssetServiceRequestTypes.FaultCorrective,
                    ReportedFaultOrRequirement = "Display flickering and overheating"
                }
            ]
        };

        var reqResult = await service.CreateServiceRequestAsync(requestInput);
        Assert.True(reqResult.Success, reqResult.Message);

        await using var db = factory.CreateDbContext();
        var request = await db.AssetServiceRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.IndividualId == 100);

        Assert.NotNull(request);
        Assert.StartsWith("SR-", request.ReferenceNumber);
        Assert.Equal(AssetServiceRequestStatuses.Submitted, request.Status);
        Assert.Single(request.Items);
        var requestItem = request.Items.First();

        // Assign technician to work order
        var assignInput = new AssetTechnicianAssignInput
        {
            AssetServiceRequestId = request.AssetServiceRequestId,
            AssetServiceRequestItemId = requestItem.AssetServiceRequestItemId,
            AssetItemId = 1001,
            TechnicianUserId = 2,
            JobType = AssetMaintenanceJobTypes.Corrective,
            Priority = "High",
            InitialDiagnosis = "Bench thermal stress test and LCD cable inspection scheduled."
        };

        var assignResult = await service.AssignTechnicianAsync(assignInput);
        Assert.True(assignResult.Success, assignResult.Message);

        await using (var verifyDb = factory.CreateDbContext())
        {
            var updatedReq = await verifyDb.AssetServiceRequests
                .Include(r => r.MaintenanceJobs)
                .FirstOrDefaultAsync(r => r.AssetServiceRequestId == request.AssetServiceRequestId);

            Assert.NotNull(updatedReq);
            Assert.Equal(AssetServiceRequestStatuses.Triage, updatedReq.Status);
            Assert.Single(updatedReq.MaintenanceJobs);

            var job = updatedReq.MaintenanceJobs.First();
            Assert.StartsWith("WO-", job.WorkOrderNumber);
            Assert.Equal(2, job.AssignedTechnicianUserId);
            Assert.Equal(AssetMaintenanceJobStatuses.Assigned, job.Status);
        }
    }

    [Fact]
    public async Task MaintenanceJob_Completion_With_BenchDiagnostics_And_Sha256_Document()
    {
        await SeedLifecycleBaseDataAsync();
        var service = CreateService();

        // Create request and assign technician
        var reqResult = await service.CreateServiceRequestAsync(new AssetServiceRequestCreateInput
        {
            IndividualId = 100,
            JobId = 50,
            OrganisationId = 10,
            RequestType = AssetServiceRequestTypes.FaultCorrective,
            Title = "Fan Replacement & Thermal Repaste",
            Items = [new AssetServiceRequestItemInput { AssetItemId = 1001, ReportedFaultOrRequirement = "Fan bearing noise" }]
        });
        Assert.True(reqResult.Success);

        await using var db = factory.CreateDbContext();
        var request = await db.AssetServiceRequests.Include(r => r.Items).FirstAsync(r => r.IndividualId == 100);

        await service.AssignTechnicianAsync(new AssetTechnicianAssignInput
        {
            AssetServiceRequestId = request.AssetServiceRequestId,
            AssetServiceRequestItemId = request.Items.First().AssetServiceRequestItemId,
            AssetItemId = 1001,
            TechnicianUserId = 2,
            JobType = AssetMaintenanceJobTypes.Corrective
        });

        var job = await db.AssetMaintenanceJobs.FirstAsync(j => j.AssetServiceRequestId == request.AssetServiceRequestId);

        // Complete the maintenance job with bench testing checklist and costs
        var completeInput = new AssetMaintenanceJobCompleteInput
        {
            AssetMaintenanceJobId = job.AssetMaintenanceJobId,
            DiagnosticSummary = "Left cooling fan bearing worn; thermal paste degraded.",
            WorkPerformedSummary = "Replaced dual vapor chamber fans, cleaned exhaust fins, applied Arctic MX-6 paste.",
            HardwareTestSummary = "Thermal benchmark 30-min sustained stress test peaked at 76°C (safe limit 95°C).",
            MemTestPassed = true,
            SmartCheckPassed = true,
            ThermalBenchmarkPassed = true,
            PortsCheckPassed = true,
            DataSanitizationVerified = false,
            VendorWarrantyClaimNumber = "LEN-RMA-2026-992",
            PartsCost = 120.00m,
            LabourCost = 65.00m,
            ResolutionSummary = "All bench diagnostics passed; cooling performance restored."
        };

        var completeResult = await service.CompleteMaintenanceJobAsync(completeInput);
        Assert.True(completeResult.Success, completeResult.Message);

        await using (var verifyDb = factory.CreateDbContext())
        {
            var updatedJob = await verifyDb.AssetMaintenanceJobs.FindAsync(job.AssetMaintenanceJobId);
            Assert.NotNull(updatedJob);
            Assert.Equal(AssetMaintenanceJobStatuses.ReadyForHandover, updatedJob.Status);
            Assert.Equal(185.00m, updatedJob.TotalCost);
            Assert.Contains("Thermal benchmark", updatedJob.HardwareTestSummary);
        }

        // Fetch Printable Document (Service Report) and verify SHA-256 seal
        var doc = await service.GetServicePrintableDocumentAsync(job.AssetMaintenanceJobId, AssetServiceDocumentTypes.ServiceReport);
        Assert.NotNull(doc);
        Assert.Equal(AssetServiceDocumentTypes.ServiceReport, doc.DocumentType);
        Assert.StartsWith("SRV-DOC-", doc.DocumentNumber);
        Assert.NotEmpty(doc.Sha256Hash);
        Assert.Equal(64, doc.Sha256Hash.Length); // 64 hex characters for SHA-256
        Assert.Contains("Functional bench test verified", doc.ChecklistItems);
    }

    [Fact]
    public async Task HardwareUpgrade_Preserves_AssetItemId_And_ActiveCustody_While_Capturing_Snapshot()
    {
        await SeedLifecycleBaseDataAsync();
        var service = CreateService();

        // Seed initial 8GB RAM module
        await using (var db = factory.CreateDbContext())
        {
            var initialRam = new AssetComponent
            {
                AssetComponentId = 701,
                AssetItemId = 1001,
                ComponentType = AssetComponentTypes.RAM,
                Brand = "Samsung",
                Model = "DDR5-4800",
                CapacityOrRating = "8 GB",
                Specification = "8GB DDR5 4800MHz SODIMM",
                SlotOrPortLocation = "SODIMM Slot 1",
                Status = AssetComponentStatuses.Installed,
                InstalledAtUtc = DateTime.UtcNow.AddDays(-60)
            };
            db.AssetComponents.Add(initialRam);
            await db.SaveChangesAsync();
        }

        // Perform upgrade: upgrade to 16GB RAM ($85 cost), retain removed 8GB RAM in spare stock
        var upgradeInput = new AssetHardwareUpgradeInput
        {
            AssetItemId = 1001,
            ComponentType = AssetComponentTypes.RAM,
            SlotOrPosition = "SODIMM Slot 1",
            RemovedAssetComponentId = 701,
            RemovedPartDisposition = "RetainedSpare",
            NewComponentBrand = "Crucial",
            NewComponentModel = "CT16G56C46S5",
            NewComponentSerialNumber = "SN-CRU-16GB-8841",
            NewCapacityOrRating = "16 GB",
            NewComponentSpecification = "16GB DDR5 5600MHz CL46 SODIMM",
            NewComponentCost = 85.00m,
            UpgradeRemarks = "Memory capacity upgrade for virtualization workload"
        };

        var upgradeResult = await service.PerformHardwareUpgradeAsync(upgradeInput);
        Assert.True(upgradeResult.Success, upgradeResult.Message);

        await using (var db = factory.CreateDbContext())
        {
            // 1. AssetItemId 1001 must be preserved
            var asset = await db.AssetItems.FindAsync(1001);
            Assert.NotNull(asset);
            Assert.Equal("LAP-001", asset.AssetCode);

            // 2. Active Custody 500 must remain active
            var custody = await db.AssetCustodies.FindAsync(500);
            Assert.NotNull(custody);
            Assert.Equal(AssetCustodyStatuses.Active, custody.Status);
            Assert.Null(custody.CustodyToUtc);

            // 3. Old component marked RetainedSpare / StockSpare
            var oldComp = await db.AssetComponents.FindAsync(701);
            Assert.NotNull(oldComp);
            Assert.Equal(AssetComponentStatuses.StockSpare, oldComp.Status);
            Assert.NotNull(oldComp.RemovedAtUtc);

            // 4. New component installed
            var newComp = await db.AssetComponents
                .FirstOrDefaultAsync(c => c.AssetItemId == 1001 && c.Status == AssetComponentStatuses.Installed);
            Assert.NotNull(newComp);
            Assert.Equal("Crucial", newComp.Brand);
            Assert.Equal("16 GB", newComp.CapacityOrRating);

            // 5. Component transaction logged
            var tx = await db.AssetComponentTransactions
                .FirstOrDefaultAsync(t => t.AssetComponentId == newComp.AssetComponentId);
            Assert.NotNull(tx);
            Assert.Equal(AssetComponentTransactionTypes.InstalledAsUpgrade, tx.TransactionType);

            // 6. Configuration snapshot captured
            var snapshot = await db.AssetConfigurationSnapshots
                .FirstOrDefaultAsync(s => s.AssetItemId == 1001);
            Assert.NotNull(snapshot);
            Assert.Equal(AssetConfigurationTriggerEvents.PostUpgrade, snapshot.TriggerEvent);
            Assert.Contains("16GB", snapshot.RamSummary);
        }

        // Test configuration history timeline DTO
        var timeline = await service.GetAssetConfigurationHistoryAsync(1001);
        Assert.NotNull(timeline);
        Assert.Equal("LAP-001", timeline.AssetCode);
        Assert.Contains("16GB", timeline.CurrentRamSummary);
        Assert.Single(timeline.Snapshots);
    }

    [Fact]
    public async Task EquipmentReplacement_Links_Original_And_Replacement_Without_Prematurely_Closing_Custody()
    {
        await SeedLifecycleBaseDataAsync();
        var service = CreateService();

        // Create replacement link from LAP-001 to LAP-003
        var replacementInput = new AssetReplacementLinkInput
        {
            OriginalAssetItemId = 1001,
            OriginalAssetCustodyId = 500,
            ReplacementAssetItemId = 1003,
            ReplacementType = AssetReplacementTypes.CompleteDeviceReplacement,
            ReplacementReason = AssetReplacementReasons.BeyondEconomicalRepair,
            OriginalAssetDisposition = "PendingReturn",
            StaffRemarks = "Replacement device issued; original pending return to store."
        };

        var result = await service.RecordReplacementLinkAsync(replacementInput);
        Assert.True(result.Success, result.Message);

        await using var db = factory.CreateDbContext();
        var link = await db.AssetReplacementLinks
            .Include(l => l.OriginalAssetItem)
            .Include(l => l.ReplacementAssetItem)
            .FirstOrDefaultAsync(l => l.OriginalAssetItemId == 1001 && l.ReplacementAssetItemId == 1003);

        Assert.NotNull(link);
        Assert.Equal(AssetReplacementTypes.CompleteDeviceReplacement, link.ReplacementType);
        Assert.Equal(AssetReplacementReasons.BeyondEconomicalRepair, link.ReplacementReason);
        Assert.False(link.StaffAcknowledged);

        // Core business rule: Original custody must NOT be closed merely because replacement was approved
        var originalCustody = await db.AssetCustodies.FindAsync(500);
        Assert.NotNull(originalCustody);
        Assert.Equal(AssetCustodyStatuses.Active, originalCustody.Status);
        Assert.Null(originalCustody.CustodyToUtc);

        // Printable Replacement Certificate generates with SHA-256 seal
        var cert = await service.GetServicePrintableDocumentAsync(link.AssetReplacementLinkId, AssetServiceDocumentTypes.ReplacementCertificate);
        Assert.NotNull(cert);
        Assert.Equal(AssetServiceDocumentTypes.ReplacementCertificate, cert.DocumentType);
        Assert.Equal(64, cert.Sha256Hash.Length);
    }

    [Fact]
    public async Task TemporaryLoaner_Issuance_And_Return_Manages_InventoryPool_Transitions()
    {
        await SeedLifecycleBaseDataAsync();
        var service = CreateService();

        // 1. Issue LAP-002 as temporary loaner
        var loanerInput = new AssetLoanerIssueInput
        {
            OrganisationId = 10,
            IndividualId = 100,
            JobId = 50,
            PrimaryAssetItemId = 1001,
            LoanerAssetItemId = 1002,
            ExpectedReturnDate = DateTime.UtcNow.AddDays(14).Date,
            ConditionOutId = 1,
            Notes = "Loaner laptop issued while LAP-001 is undergoing motherboard repair."
        };

        var issueResult = await service.IssueLoanerAssetAsync(loanerInput);
        Assert.True(issueResult.Success, issueResult.Message);

        await using (var db = factory.CreateDbContext())
        {
            // Loaner inventory status must transition to Assigned
            var loanerAsset = await db.AssetItems.FindAsync(1002);
            Assert.NotNull(loanerAsset);
            Assert.Equal(AssetInventoryStatuses.Assigned, loanerAsset.Status);

            // Temporary handover record created with ActiveLoaner status
            var handover = await db.AssetTemporaryHandovers
                .FirstOrDefaultAsync(h => h.LoanerAssetItemId == 1002 && h.Status == AssetTemporaryHandoverStatuses.ActiveLoaner);
            Assert.NotNull(handover);
            Assert.Equal(100, handover.IndividualId);
        }

        // Printable Loaner Agreement generates with SHA-256 seal
        await using (var db = factory.CreateDbContext())
        {
            var handover = await db.AssetTemporaryHandovers.FirstAsync(h => h.LoanerAssetItemId == 1002);
            var agreement = await service.GetServicePrintableDocumentAsync(handover.AssetTemporaryHandoverId, AssetServiceDocumentTypes.LoanerAgreement);
            Assert.NotNull(agreement);
            Assert.Equal(AssetServiceDocumentTypes.LoanerAgreement, agreement.DocumentType);
            Assert.Equal(64, agreement.Sha256Hash.Length);
        }

        // 2. Return loaner equipment
        await using (var db = factory.CreateDbContext())
        {
            var handover = await db.AssetTemporaryHandovers.FirstAsync(h => h.LoanerAssetItemId == 1002);
            var returnResult = await service.ReturnLoanerAssetAsync(new AssetLoanerReturnInput
            {
                AssetTemporaryHandoverId = handover.AssetTemporaryHandoverId,
                ConditionInId = 1,
                Notes = "Returned in pristine condition; wiped and restored to pool."
            });
            Assert.True(returnResult.Success, returnResult.Message);
        }

        // Verify loaner status restored to Available
        await using (var db = factory.CreateDbContext())
        {
            var loanerAsset = await db.AssetItems.FindAsync(1002);
            Assert.NotNull(loanerAsset);
            Assert.Equal(AssetInventoryStatuses.Available, loanerAsset.Status);

            var handover = await db.AssetTemporaryHandovers.FirstAsync(h => h.LoanerAssetItemId == 1002);
            Assert.Equal(AssetTemporaryHandoverStatuses.ReturnedInGoodOrder, handover.Status);
            Assert.NotNull(handover.ActualReturnedAtUtc);
        }
    }

    [Fact]
    public void Staff_Service_PostHandover_Acknowledgement_Requires_Rejection_Comments()
    {
        // Test validator: rejection requires comments
        var invalidInput = new AssetServiceStaffAcknowledgeInput
        {
            AssetServiceRequestItemId = 1,
            StaffDecision = "Rejected",
            StaffRemarks = "" // Empty remarks must fail
        };
        var failErrors = StaffAssetValidator.ValidateServiceStaffAcknowledge(invalidInput);
        Assert.NotEmpty(failErrors);
        Assert.Contains(failErrors, e => e.Contains("Mandatory remarks/comments are required when rejecting"));

        var validInput = new AssetServiceStaffAcknowledgeInput
        {
            AssetServiceRequestItemId = 1,
            StaffDecision = "Rejected",
            StaffRemarks = "Display still flickers after sleep wakeup."
        };
        var passErrors = StaffAssetValidator.ValidateServiceStaffAcknowledge(validInput);
        Assert.Empty(passErrors);

        var acceptInput = new AssetServiceStaffAcknowledgeInput
        {
            AssetServiceRequestItemId = 1,
            StaffDecision = "Accepted"
        };
        var acceptErrors = StaffAssetValidator.ValidateServiceStaffAcknowledge(acceptInput);
        Assert.Empty(acceptErrors);
    }

    [Fact]
    public async Task StaffClearance_Blocks_When_Obligations_Exist_And_Clears_When_Resolved()
    {
        await SeedLifecycleBaseDataAsync();
        var service = CreateService();

        // Ahmed Ali (Individual 100) currently has active custody of LAP-001
        var blockedCheck = await service.CheckStaffAssetClearanceAsync(100);
        Assert.NotNull(blockedCheck);
        Assert.False(blockedCheck.IsEligibleForClearance);
        Assert.Equal(1, blockedCheck.ActiveCustodiesCount);
        Assert.Contains("Clearance blocked", blockedCheck.ClearanceSummary);

        // Close custody (e.g. following return inspection)
        await using (var db = factory.CreateDbContext())
        {
            var custody = await db.AssetCustodies.FindAsync(500);
            Assert.NotNull(custody);
            custody.CustodyToUtc = DateTime.UtcNow;
            custody.Status = AssetCustodyStatuses.Returned;
            await db.SaveChangesAsync();
        }

        // Re-evaluate clearance: all liabilities resolved
        var clearCheck = await service.CheckStaffAssetClearanceAsync(100);
        Assert.NotNull(clearCheck);
        Assert.True(clearCheck.IsEligibleForClearance);
        Assert.Equal(0, clearCheck.ActiveCustodiesCount);
        Assert.Equal(0, clearCheck.ActiveLoanersCount);
        Assert.Equal(0, clearCheck.OpenServiceRequestsCount);
        Assert.Contains("zero outstanding asset liabilities", clearCheck.ClearanceSummary);

        // Generate printable official Clearance Certificate with SHA-256 seal
        var clearanceDoc = await service.GetServicePrintableDocumentAsync(100, AssetServiceDocumentTypes.TransferClearanceReport);
        Assert.NotNull(clearanceDoc);
        Assert.Equal(AssetServiceDocumentTypes.TransferClearanceReport, clearanceDoc.DocumentType);
        Assert.Equal(64, clearanceDoc.Sha256Hash.Length);
        Assert.Contains("Cryptographic hash verified", clearanceDoc.ChecklistItems);
    }
}
