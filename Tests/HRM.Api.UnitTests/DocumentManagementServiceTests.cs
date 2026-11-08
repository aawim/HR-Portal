using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRM.Authorization;
using HRM.DTOs;
using HRM.DTOs.Documents;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class DocumentManagementServiceTests
{
    private static IOperationLogService CreateLogsStub() =>
        StubProxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = 1 }));

    private static IRequestService CreateRequestsStub() =>
        StubProxy.For<IRequestService>((_, args) =>
        {
            var dto = args.OfType<CreateRequestDto>().First();
            var req = new Request
            {
                RequestId = Random.Shared.Next(200, 9999),
                RequestTypeId = dto.RequestTypeId,
                ApplicantBusinessEntityId = dto.ApplicantBusinessEntityId ?? 0,
                OrganisationBusinessEntityID = dto.OrganisationBusinessEntityId ?? 0,
                ApplicationDate = DateTime.UtcNow,
                RequestStateId = 2,
                ReferenceNumber = $"DMR-2026-{Random.Shared.Next(1000, 9999)}"
            };
            var db = args.OfType<HrmTeContext>().FirstOrDefault();
            db?.Requests.Add(req);
            return Task.FromResult(req);
        });

    private static byte[] CreateValidPdfBytes(string body = "Official Document Content") =>
        Encoding.ASCII.GetBytes($"%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n% {body}\n%%EOF");

    private static async Task<(
        QueryFactory Factory,
        FakeAccess Access,
        Services.PrivateSqlDocumentStorageProvider Storage,
        Services.DocumentManagementService Service,
        int Org1Id,
        int Org2Id,
        int Employee1Id,
        int Employee2Id,
        int HrUserId)> CreateTestEnvironmentAsync()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var org1Id = 10;
        var org2Id = 20;
        var employee1Id = 101;
        var employee2Id = 102;
        var hrUserId = 500;

        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.AddRange(
                new Organisation { BusinessEntityID = org1Id, OrganisationName = "Ministry of Civil Service", RegistrationNumber = "MCS-01", OrganisationTypeId = 1, CountryId = 1 },
                new Organisation { BusinessEntityID = org2Id, OrganisationName = "Ministry of Finance", RegistrationNumber = "MOF-02", OrganisationTypeId = 1, CountryId = 1 }
            );

            db.OrganisationStructures.Add(new OrganisationStructure
            {
                OrganisationStructureId = 1,
                OrganisationBusinessEntityID = org1Id,
                Name = "Human Resources Department",
                OrganisationStructureStateId = 1,
                OrganisationStructureTypeId = 1,
                OperationLogId = 1
            });

            db.Individuals.AddRange(
                new Individual
                {
                    BusinessEntityId = employee1Id,
                    FirstNameEnglish = "Aminath",
                    LastNameEnglish = "Shareef",
                    GenderTypeId = 2,
                    Idcards = [new Idcard { IdcardId = 1, BusinessEntityId = employee1Id, IdcardNumber = "A123456", IdcardStateId = 1 }]
                },
                new Individual
                {
                    BusinessEntityId = employee2Id,
                    FirstNameEnglish = "Ibrahim",
                    LastNameEnglish = "Nazeer",
                    GenderTypeId = 1,
                    Idcards = [new Idcard { IdcardId = 2, BusinessEntityId = employee2Id, IdcardNumber = "A654321", IdcardStateId = 1 }]
                }
            );

            db.Staffs.AddRange(
                new Staff { IndividualId = employee1Id, EmployeeNumber = "EMP-101", OperationLogId = 1 },
                new Staff { IndividualId = employee2Id, EmployeeNumber = "EMP-102", OperationLogId = 1 }
            );

            var pos = new Position { PositionId = 1, Name = "Principal HR Analyst", IsActive = true };
            db.Positions.Add(pos);

            var job = new Job
            {
                JobId = 10,
                IndividualID = employee1Id,
                OrganisationID = org1Id,
                OrganisationStructureId = 1,
                JoinedDate = new DateTime(2020, 3, 15),
                JobStateId = 4,
                JobTypeId = 1,
                BasicSalary = 22000m,
                OperationLogId = 1
            };
            var jp = new JobPosition
            {
                JobPositionId = 10,
                JobId = 10,
                PositionId = 1,
                FromDate = new DateTime(2020, 3, 15),
                JobPositionStateId = 4,
                OperationLogId = 1
            };
            job.JobPositions.Add(jp);
            db.Jobs.Add(job);
            db.JobPositions.Add(jp);

            await db.SaveChangesAsync();
        }

        var access = new FakeAccess
        {
            Organisation = org1Id,
            Actor = new UserContextDto
            {
                UserId = hrUserId,
                IndividualId = 900,
                BusinessEntityId = 900,
                Username = "hr.admin",
                FullName = "HR Director",
                UserOrganisationId = org1Id,
                IsHRManager = true,
                IsOrganisationAdmin = true
            }
        };

        var storage = new Services.PrivateSqlDocumentStorageProvider(
            factory,
            NullLogger<Services.PrivateSqlDocumentStorageProvider>.Instance);

        var service = new Services.DocumentManagementService(
            factory,
            access,
            storage,
            CreateLogsStub(),
            CreateRequestsStub(),
            NullLogger<Services.DocumentManagementService>.Instance);

        return (factory, access, storage, service, org1Id, org2Id, employee1Id, employee2Id, hrUserId);
    }

    [Fact]
    public async Task UploadSecurity_ValidatesMagicBytes_RejectsMalwareAndOversizedFiles_AndVerifiesSha256Integrity()
    {
        var (_, _, storage, service, org1Id, _, employee1Id, _, _) = await CreateTestEnvironmentAsync();
        var types = await service.GetDocumentTypesAsync(org1Id);
        var idType = types.First(t => t.Code == "NATIONAL_ID_PASSPORT");

        // 1. Reject executable disguised as .pdf
        var fakeExeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 }; // MZ header
        var badHeaderRes = await service.UploadDocumentAsync(new DmsDocumentUploadInput
        {
            DmsDocumentTypeId = idType.DmsDocumentTypeId,
            Title = "Disguised Executable",
            FileName = "passport.pdf",
            FileContent = fakeExeBytes
        });
        Assert.False(badHeaderRes.Success);
        Assert.Contains("Executable", badHeaderRes.Message, StringComparison.OrdinalIgnoreCase);

        // 2. Reject PDF containing embedded /JavaScript malicious payload
        var maliciousPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n<< /OpenAction << /S /JavaScript /JS (app.alert(1)) >> >>\n%%EOF");
        var malwareRes = await service.UploadDocumentAsync(new DmsDocumentUploadInput
        {
            DmsDocumentTypeId = idType.DmsDocumentTypeId,
            Title = "Scripted PDF",
            FileName = "scripted.pdf",
            FileContent = maliciousPdf
        });
        Assert.False(malwareRes.Success);
        Assert.Contains("Security scan rejected", malwareRes.Message, StringComparison.OrdinalIgnoreCase);

        // 3. Reject file exceeding MaxFileSizeMb via storage provider
        var oversizedRes = await storage.StoreAsync(new DocumentStorageWriteRequest
        {
            OrganisationId = org1Id,
            OriginalFileName = "huge.pdf",
            Content = new byte[2 * 1024 * 1024], // 2 MB
            PermittedExtensionsCsv = ".pdf",
            MaxFileSizeMb = 1, // 1 MB limit
            ActorUserId = 1
        });
        Assert.False(oversizedRes.Success);
        Assert.Contains("exceeds the maximum permitted limit", oversizedRes.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // 4. Valid PDF upload succeeds, computes SHA-256, and verifies integrity on download
        var validPdf = CreateValidPdfBytes("Verified National ID Scan");
        var okRes = await service.UploadDocumentAsync(new DmsDocumentUploadInput
        {
            DmsDocumentTypeId = idType.DmsDocumentTypeId,
            SubjectIndividualId = employee1Id,
            Title = "National ID Card - Aminath Shareef",
            FileName = "nid_aminath.pdf",
            FileContent = validPdf
        });
        Assert.True(okRes.Success);

        var docs = await service.GetStaffDocumentsAsync(employee1Id);
        Assert.Single(docs);
        var doc = docs[0];

        var download = await service.DownloadDocumentVersionAsync(doc.DocumentId);
        Assert.True(download.Success);
        Assert.True(download.IntegrityVerified);
        Assert.Equal(validPdf, download.Content);
        Assert.NotEmpty(download.Sha256Hash);
    }

    [Fact]
    public async Task Versioning_PreservesHistoricalVersions_SupportsCheckOutCheckIn_AndRestoresPreviousVersion()
    {
        var (factory, _, _, service, org1Id, _, employee1Id, _, _) = await CreateTestEnvironmentAsync();
        var types = await service.GetDocumentTypesAsync(org1Id);
        var qualType = types.First(t => t.Code == "ACADEMIC_QUALIFICATION");

        var v1Bytes = CreateValidPdfBytes("Version 1 Degree Certificate");
        var v2Bytes = CreateValidPdfBytes("Version 2 Attested Degree Certificate");

        await service.UploadDocumentAsync(new DmsDocumentUploadInput
        {
            DmsDocumentTypeId = qualType.DmsDocumentTypeId,
            SubjectIndividualId = employee1Id,
            Title = "MSc Information Systems",
            FileName = "msc_v1.pdf",
            FileContent = v1Bytes
        });

        var docSummary = (await service.GetStaffDocumentsAsync(employee1Id)).Single();

        // Check out document
        var checkOutRes = await service.ExecuteLifecycleActionAsync(new DmsDocumentLifecycleActionInput
        {
            DocumentId = docSummary.DocumentId,
            Action = "CheckOut",
            Comments = "Updating with MQA attestation stamp"
        });
        Assert.True(checkOutRes.Success);

        // Upload Version 2
        var v2Res = await service.UploadNewVersionAsync(new DmsDocumentNewVersionInput
        {
            DocumentId = docSummary.DocumentId,
            FileName = "msc_v2_attested.pdf",
            FileContent = v2Bytes,
            ChangeComments = "Added MQA attestation page",
            CheckInAfterUpload = true
        });
        Assert.True(v2Res.Success);

        var detail = await service.GetDocumentDetailAsync(docSummary.DocumentId);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.Summary.CurrentVersionNumber);
        Assert.False(detail.Summary.IsLockedForEdit);
        Assert.Equal(2, detail.Versions.Count);

        // Verify v1 binary is preserved intact and not overwritten
        var dlV1 = await service.DownloadDocumentVersionAsync(docSummary.DocumentId, versionNumber: 1);
        var dlV2 = await service.DownloadDocumentVersionAsync(docSummary.DocumentId, versionNumber: 2);
        Assert.Equal(v1Bytes, dlV1.Content);
        Assert.Equal(v2Bytes, dlV2.Content);
        Assert.NotEqual(dlV1.Sha256Hash, dlV2.Sha256Hash);

        // Restore v1 as v3
        var restoreRes = await service.ExecuteLifecycleActionAsync(new DmsDocumentLifecycleActionInput
        {
            DocumentId = docSummary.DocumentId,
            Action = "RestoreVersion",
            TargetVersionNumber = 1,
            Comments = "Reverting to original scan"
        });
        Assert.True(restoreRes.Success);

        await using var db = factory.CreateDbContext();
        Assert.Equal(2, await db.DmsStoredObjects.CountAsync()); // Restored version reuses immutable StoredObject without duplicating binary
        var dlV3 = await service.DownloadDocumentVersionAsync(docSummary.DocumentId, versionNumber: 3);
        Assert.Equal(v1Bytes, dlV3.Content);
    }

    [Fact]
    public async Task SecurityAndMultiRecordLinking_EnforcesOrganisationIsolation_AndPerDocumentAccessPolicy()
    {
        var (factory, access, _, service, org1Id, org2Id, employee1Id, employee2Id, _) = await CreateTestEnvironmentAsync();
        var types = await service.GetDocumentTypesAsync(org1Id);
        var evalType = types.First(t => t.Code == "PERFORMANCE_APPRAISAL_RECORD"); // Restricted / SupervisorAndHr

        await service.UploadDocumentAsync(new DmsDocumentUploadInput
        {
            DmsDocumentTypeId = evalType.DmsDocumentTypeId,
            SubjectIndividualId = employee1Id,
            Title = "Restricted Appraisal 2026 - Aminath",
            Classification = "Restricted",
            AccessScope = "HrOnly",
            FileName = "appraisal_2026.pdf",
            FileContent = CreateValidPdfBytes("Confidential HR Assessment")
        });

        var hrDocs = await service.SearchDocumentsAsync(new DmsDocumentSearchFilterDto { OrganisationId = org1Id });
        var docId = hrDocs.Single().DocumentId;

        // Link the same single document to a Project (#500) and a Transfer (#600) without duplicating physical file
        Assert.True((await service.LinkDocumentToRecordAsync(new DmsDocumentLinkInput
        {
            DocumentId = docId,
            TargetEntityType = "Project",
            TargetEntityId = 500,
            TargetDisplayName = "Digital HR Project"
        })).Success);

        Assert.True((await service.LinkDocumentToRecordAsync(new DmsDocumentLinkInput
        {
            DocumentId = docId,
            TargetEntityType = "Transfer",
            TargetEntityId = 600,
            TargetDisplayName = "Internal Transfer #600"
        })).Success);

        await using (var db = factory.CreateDbContext())
        {
            Assert.Equal(1, await db.DmsStoredObjects.CountAsync()); // Single physical stored binary
            Assert.Equal(3, await db.DmsDocumentLinks.CountAsync(l => l.DocumentId == docId && l.IsActive));
        }

        // Switch context to Employee 2 (ordinary employee in Org 1 who might view Project #500)
        // Requirement 7: A document linked to multiple records must not automatically become visible to every viewer of those records.
        access.Actor = new UserContextDto
        {
            UserId = 202,
            IndividualId = employee2Id,
            BusinessEntityId = employee2Id,
            Username = "ibrahim.nazeer",
            UserOrganisationId = org1Id,
            IsHRStaff = false,
            IsSupervisor = false
        };

        var linkedForEmp2 = await service.GetLinkedEntityDocumentsAsync("Project", 500);
        Assert.Empty(linkedForEmp2); // Filtered out by HrOnly effective access policy!

        var dlDenied = await service.DownloadDocumentVersionAsync(docId);
        Assert.False(dlDenied.Success);

        // Switch context to Organisation 2 — verify strict tenant isolation
        access.Organisation = org2Id;
        access.Actor = new UserContextDto
        {
            UserId = 999,
            IndividualId = 999,
            BusinessEntityId = 999,
            Username = "org2.hr",
            UserOrganisationId = org2Id,
            IsHRManager = true
        };

        var org2Search = await service.SearchDocumentsAsync(new DmsDocumentSearchFilterDto { OrganisationId = org2Id });
        Assert.Empty(org2Search);
    }

    [Fact]
    public async Task TemplatesAndWorkflow_ResolvesVerifiedPlaceholders_BlocksUnsafeTokens_AndIssuesImmutableOfficialDocument()
    {
        var (_, access, _, service, org1Id, _, employee1Id, _, hrUserId) = await CreateTestEnvironmentAsync();

        // 1. Block arbitrary / unsafe template expressions
        var badTplRes = await service.SaveTemplateAsync(new DmsDocumentTemplateDto
        {
            Code = "TPL-UNSAFE",
            Name = "Unsafe Template",
            DocumentTitlePattern = "Letter",
            BodyTemplate = "Hello {{EmployeeName}}, secret={{System.Environment.MachineName}}"
        });
        Assert.False(badTplRes.Success);
        Assert.Contains("unauthorised or unsafe placeholder", badTplRes.Message, StringComparison.OrdinalIgnoreCase);

        // 2. Employee submits Official Document Request
        var templates = await service.GetTemplatesAsync(org1Id);
        var verifyTpl = templates.First(t => t.Code == "TPL-EMP-VERIFY");

        access.Actor = new UserContextDto
        {
            UserId = 101,
            IndividualId = employee1Id,
            BusinessEntityId = employee1Id,
            Username = "aminath.shareef",
            FullName = "Aminath Shareef",
            UserOrganisationId = org1Id
        };

        var submitRes = await service.SubmitDocumentRequestAsync(new DmsDocumentRequestSubmitInput
        {
            DocumentTemplateId = verifyTpl.DocumentTemplateId,
            RequestKind = "EmploymentVerificationLetter",
            Purpose = "Housing Development Finance Corporation Loan",
            AddressedTo = "HDFC Maldives"
        });
        Assert.True(submitRes.Success);

        var myReqs = await service.GetMyDocumentRequestsAsync();
        Assert.Single(myReqs);
        var reqId = myReqs[0].DocumentRequestId;

        // 3. Employee cannot approve and issue their own official document request
        var selfApproveRes = await service.ProcessDocumentRequestActionAsync(new DmsDocumentRequestActionInput
        {
            DocumentRequestId = reqId,
            Action = "ApproveAndIssue"
        });
        Assert.False(selfApproveRes.Success);

        // 4. HR Approver reviews and issues official document
        access.Actor = new UserContextDto
        {
            UserId = hrUserId,
            IndividualId = 900,
            BusinessEntityId = 900,
            Username = "hr.admin",
            FullName = "HR Director",
            UserOrganisationId = org1Id,
            IsHRManager = true
        };

        var preview = await service.ResolveTemplatePreviewAsync(
            verifyTpl.DocumentTemplateId,
            employee1Id,
            "Housing Development Finance Corporation Loan",
            "HDFC Maldives");

        Assert.Contains("Aminath Shareef", preview.ResolvedBodyText);
        Assert.Contains("Principal HR Analyst", preview.ResolvedBodyText);
        Assert.Contains("Ministry of Civil Service", preview.ResolvedBodyText);

        var issueRes = await service.ProcessDocumentRequestActionAsync(new DmsDocumentRequestActionInput
        {
            DocumentRequestId = reqId,
            Action = "ApproveAndIssue",
            SignatoryName = "Hassan Rasheed",
            SignatoryTitle = "Permanent Secretary",
            Comments = "Verified employment record and issued."
        });
        Assert.True(issueRes.Success);

        var updatedReq = (await service.GetAdminDocumentRequestsAsync(org1Id)).Single(r => r.DocumentRequestId == reqId);
        Assert.Equal("Issued", updatedReq.Status);
        Assert.NotNull(updatedReq.IssuedDocumentId);

        // 5. Verify issued official document is immutable and cannot be overwritten with a new version
        var issuedDetail = await service.GetDocumentDetailAsync(updatedReq.IssuedDocumentId!.Value);
        Assert.NotNull(issuedDetail);
        Assert.True(issuedDetail.Summary.IsOfficialRecord);
        Assert.True(issuedDetail.Summary.IsImmutable);
        Assert.Single(issuedDetail.Signatures);

        var mutateRes = await service.UploadNewVersionAsync(new DmsDocumentNewVersionInput
        {
            DocumentId = issuedDetail.Summary.DocumentId,
            FileName = "tampered.pdf",
            FileContent = CreateValidPdfBytes("Tampered")
        });
        Assert.False(mutateRes.Success);
        Assert.Contains("immutable", mutateRes.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Governance_LegalHoldProtectsRecordFromDisposal_UntilReleased()
    {
        var (_, _, _, service, org1Id, _, _, _, _) = await CreateTestEnvironmentAsync();
        var types = await service.GetDocumentTypesAsync(org1Id);
        var docType = types.First(t => t.Code == "PROJECT_DELIVERABLE_SPEC");

        await service.UploadDocumentAsync(new DmsDocumentUploadInput
        {
            DmsDocumentTypeId = docType.DmsDocumentTypeId,
            Title = "System Architecture Deliverable",
            FileName = "arch_spec.pdf",
            FileContent = CreateValidPdfBytes("Architecture v1")
        });

        var doc = (await service.SearchDocumentsAsync(new DmsDocumentSearchFilterDto { OrganisationId = org1Id })).Single();

        // Place Legal Hold
        var holdRes = await service.ManageLegalHoldAsync(new DmsLegalHoldInput
        {
            DocumentId = doc.DocumentId,
            HoldReference = "LH-2026-AUDIT-09",
            CaseOrMatterName = "Auditor General Compliance Review",
            HoldReason = "Preserve all system specifications during audit."
        });
        Assert.True(holdRes.Success);

        // Attempt disposal while Legal Hold is active -> MUST FAIL
        var blockedDispose = await service.ExecuteLifecycleActionAsync(new DmsDocumentLifecycleActionInput
        {
            DocumentId = doc.DocumentId,
            Action = "Dispose",
            Comments = "Attempted disposal during hold"
        });
        Assert.False(blockedDispose.Success);
        Assert.Contains("LEGAL HOLD", blockedDispose.Message, StringComparison.OrdinalIgnoreCase);

        // Release Legal Hold
        var releaseRes = await service.ManageLegalHoldAsync(new DmsLegalHoldInput
        {
            DocumentId = doc.DocumentId,
            Release = true,
            ReleaseNotes = "Audit concluded; hold lifted."
        });
        Assert.True(releaseRes.Success);

        // Formal disposal now succeeds with disposal certificate reference
        var allowedDispose = await service.ExecuteLifecycleActionAsync(new DmsDocumentLifecycleActionInput
        {
            DocumentId = doc.DocumentId,
            Action = "Dispose",
            Comments = "Approved disposal per retention schedule RET-OPS-3YR"
        });
        Assert.True(allowedDispose.Success);

        var detailAfterDispose = await service.GetDocumentDetailAsync(doc.DocumentId);
        Assert.NotNull(detailAfterDispose);
        Assert.Equal("Disposed", detailAfterDispose.Summary.Status);
        Assert.NotNull(detailAfterDispose.DisposalCertificateRef);
    }
}
