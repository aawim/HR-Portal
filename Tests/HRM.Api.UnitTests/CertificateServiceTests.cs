using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRM.Authorization;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Certificates;
using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CertificateServiceTests
{
    private static IOperationLogService CreateLogsStub() =>
        StubProxy.For<IOperationLogService>((_, _) => Task.FromResult(new OperationLog { OperationLogId = 1 }));

    private static IRequestService CreateRequestsStub() =>
        StubProxy.For<IRequestService>((name, args) =>
        {
            var db = (HrmTeContext)args[0]!;
            var dto = (CreateRequestDto)args[1]!;
            var req = new Request
            {
                RequestId = 100,
                RequestTypeId = dto.RequestTypeId,
                ApplicantBusinessEntityId = dto.ApplicantBusinessEntityId,
                OrganisationBusinessEntityID = dto.OrganisationBusinessEntityId,
                RequestEffectiveDate = dto.EffectiveDate,
                ApplicationDate = DateTime.UtcNow,
                RequestStateId = 1,
                ReferenceNumber = "REQ-00100"
            };
            db.Requests.Add(req);
            return Task.FromResult(req);
        });

    [Fact]
    public void QrCodeGenerator_Generates_Valid_Svg()
    {
        var url = "https://hrm.example.gov.mv/verify/certificate/abc123def456";
        var matrix = QrCodeGenerator.GenerateMatrix(url);
        var svg = QrCodeGenerator.ToSvg(matrix, pixelSize: 4);

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg.Trim());
        Assert.EndsWith("</svg>", svg.Trim());
        Assert.Contains("viewBox=\"0 0", svg);
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void QrCodeGenerator_Generates_Valid_Bmp_Bytes()
    {
        var url = "https://hrm.example.gov.mv/verify/certificate/abc123def456";
        var matrix = QrCodeGenerator.GenerateMatrix(url);
        var bmpBytes = QrCodeGenerator.ToBmp(matrix, pixelSize: 4);

        Assert.NotNull(bmpBytes);
        Assert.True(bmpBytes.Length > 54);
        // Standard BMP file signature: 'B', 'M' (0x42, 0x4D)
        Assert.Equal((byte)'B', bmpBytes[0]);
        Assert.Equal((byte)'M', bmpBytes[1]);
        // 24-bit per pixel check in BMP header (offset 28)
        var bpp = BitConverter.ToInt16(bmpBytes, 28);
        Assert.Equal(24, bpp);
    }

    private static async Task<(QueryFactory factory, FakeAccess access, int employeeId, int orgId, int hrUserId)> SeedDatabaseAsync()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var orgId = 10;
        var employeeId = 100;
        var hrUserId = 200;

        await using var db = factory.CreateDbContext();

        var org = new Organisation
        {
            BusinessEntityID = orgId,
            OrganisationName = "Ministry of Digital Development",
            RegistrationNumber = "MDD-01",
            OrganisationTypeId = 1,
            CountryId = 1
        };
        db.Organisations.Add(org);

        var dept = new OrganisationStructure
        {
            OrganisationStructureId = 1,
            OrganisationBusinessEntityID = orgId,
            Name = "Software Development Department",
            OrganisationStructureStateId = 1,
            OrganisationStructureTypeId = 1,
            OperationLogId = 1
        };
        db.OrganisationStructures.Add(dept);

        var individual = new Individual
        {
            BusinessEntityId = employeeId,
            FirstNameEnglish = "Ahmed",
            LastNameEnglish = "Rasheed",
            GenderTypeId = 1
        };
        db.Individuals.Add(individual);

        var staff = new Staff
        {
            IndividualId = employeeId,
            EmployeeNumber = "EMP-100",
            OperationLogId = 1
        };
        db.Staffs.Add(staff);

        var hrUser = new User
        {
            UserId = hrUserId,
            BusinessEntityID = 300,
            Username = "hr_officer",
            IsActive = true,
            AdminPortalAccessOrganisationId = orgId
        };
        db.Users.Add(hrUser);

        var employeeUser = new User
        {
            UserId = 1,
            BusinessEntityID = employeeId,
            Username = "ahmed.rasheed",
            IsActive = true,
            AdminPortalAccessOrganisationId = orgId
        };
        db.Users.Add(employeeUser);

        var pos1 = new Position
        {
            PositionId = 10,
            Name = "Software Engineer",
            IsActive = true,
            Descriptions = "- Design and develop scalable microservices.\n- Maintain relational databases and APIs."
        };
        var pos2 = new Position
        {
            PositionId = 11,
            Name = "Senior Software Engineer",
            IsActive = true,
            Descriptions = "- Lead engineering sprints.\n- Mentor junior software engineers."
        };
        db.Positions.AddRange(pos1, pos2);

        var job = new Job
        {
            JobId = 50,
            IndividualID = employeeId,
            OrganisationID = orgId,
            OrganisationStructureId = 1,
            JoinedDate = new DateTime(2021, 1, 1),
            TerminatedDate = null,
            JobStateId = 4,
            JobTypeId = 1,
            BasicSalary = 25000m,
            OperationLogId = 1
        };
        db.Jobs.Add(job);

        var jp1 = new JobPosition
        {
            JobPositionId = 101,
            JobId = 50,
            PositionId = 10,
            FromDate = new DateTime(2021, 1, 1),
            ToDate = new DateTime(2023, 1, 1),
            JobPositionStateId = 4,
            OperationLogId = 1
        };
        var jp2 = new JobPosition
        {
            JobPositionId = 102,
            JobId = 50,
            PositionId = 11,
            FromDate = new DateTime(2023, 1, 2),
            ToDate = null,
            JobPositionStateId = 4,
            OperationLogId = 1
        };
        db.JobPositions.AddRange(jp1, jp2);
        job.JobPositions.Add(jp1);
        job.JobPositions.Add(jp2);

        var project = new Project
        {
            ProjectId = 1,
            OrganisationId = orgId,
            Name = "Digital HR Platform",
            Reference = "DHR-2024",
            Status = "Active"
        };
        db.Projects.Add(project);

        var projectMember = new ProjectMember
        {
            ProjectMemberId = 1,
            ProjectId = 1,
            StaffId = employeeId,
            StaffName = "Ahmed Rasheed",
            Role = "Technical Lead",
            AssignedDate = new DateTime(2023, 2, 1),
            IsActive = true,
            Project = project
        };
        db.ProjectMembers.Add(projectMember);

        await db.SaveChangesAsync();

        var access = new FakeAccess
        {
            Organisation = orgId,
            Actor = new UserContextDto
            {
                UserId = 1,
                IndividualId = employeeId,
                BusinessEntityId = employeeId,
                Username = "ahmed.rasheed"
            }
        };

        return (factory, access, employeeId, orgId, hrUserId);
    }

    [Fact]
    public async Task GetVerifiedEmploymentHistory_Accurately_Builds_Positions_And_Duration()
    {
        var (factory, access, employeeId, orgId, _) = await SeedDatabaseAsync();
        var service = new Services.CertificateService(factory, access, CreateLogsStub(), CreateRequestsStub(), NullLogger<Services.CertificateService>.Instance);

        var history = await service.GetVerifiedEmploymentHistoryAsync(employeeId);

        Assert.NotNull(history);
        Assert.Equal(employeeId, history.IndividualId);
        Assert.Equal("EMP-100", history.EmployeeNumber);
        Assert.Equal("Ahmed Rasheed", history.FullName);
        Assert.Equal("Ministry of Digital Development", history.OrganisationName);
        Assert.Equal(new DateTime(2021, 1, 1), history.JoinedDate);
        Assert.True(history.IsCurrentlyEmployed);
        Assert.Equal(2, history.Positions.Count);
        Assert.Contains(history.Positions, p => p.PositionTitle == "Software Engineer" && p.OfficialResponsibilities.Count > 0);
        Assert.Contains(history.Positions, p => p.PositionTitle == "Senior Software Engineer");
        Assert.Single(history.Projects);
        Assert.Equal("Digital HR Platform", history.Projects[0].ProjectName);
        Assert.Equal("Technical Lead", history.Projects[0].Role);
    }

    [Fact]
    public async Task SubmitCertificateRequest_Creates_Request_And_Sends_Deduplicated_Notification()
    {
        var (factory, access, employeeId, orgId, hrUserId) = await SeedDatabaseAsync();
        var service = new Services.CertificateService(factory, access, CreateLogsStub(), CreateRequestsStub(), NullLogger<Services.CertificateService>.Instance);

        var input = new CertificateRequestInput
        {
            IndividualId = employeeId,
            CertificateType = "StandardService",
            Purpose = "Higher Education Application",
            EmployeeComments = "Need certificate by next Monday."
        };

        var result = await service.SubmitCertificateRequestAsync(input);

        Assert.True(result.Success);

        await using var db = factory.CreateDbContext();
        var request = await db.CertificateRequests
            .Include(r => r.Request)
            .FirstOrDefaultAsync(r => r.IndividualId == employeeId);

        Assert.NotNull(request);
        Assert.Equal("Higher Education Application", request.Purpose);
        Assert.Equal("Submitted", request.Status);
        Assert.NotNull(request.Request);
        Assert.Equal(SharedConfig.RequestTypes.EMPLOYMENT_CERTIFICATE_REQUEST, request.Request.RequestTypeId);

        // Verify Notifications:
        // Actor (UserId = 1) must NOT receive notification (addresses: "the user gets two notification for one action")
        // Only HR reviewers (hrUserId) receive it, and recipient list must be distinct
        var notifications = await db.StaffNotifications.ToListAsync();
        Assert.NotEmpty(notifications);
        Assert.DoesNotContain(notifications, n => n.UserId == 1);
        Assert.Contains(notifications, n => n.UserId == hrUserId);
        Assert.Equal(notifications.Select(n => n.UserId).Distinct().Count(), notifications.Count);
    }

    [Fact]
    public async Task ReviewAction_Approve_Generates_IssuedCertificate_With_Sha256_And_VerificationToken()
    {
        var (factory, access, employeeId, orgId, hrUserId) = await SeedDatabaseAsync();
        var service = new Services.CertificateService(factory, access, CreateLogsStub(), CreateRequestsStub(), NullLogger<Services.CertificateService>.Instance);

        // 1. Submit request as employee
        await service.SubmitCertificateRequestAsync(new CertificateRequestInput
        {
            IndividualId = employeeId,
            CertificateType = "DetailedExperience",
            Purpose = "Visa Application",
            IncludeResponsibilities = true,
            IncludeProjects = true
        });

        int requestId;
        await using (var db = factory.CreateDbContext())
        {
            requestId = (await db.CertificateRequests.FirstAsync()).CertificateRequestId;
        }

        // 2. Switch context to HR reviewer
        access.Actor = new UserContextDto
        {
            UserId = hrUserId,
            IndividualId = 300,
            BusinessEntityId = 300,
            Username = "hr_officer",
            IsHRStaff = true
        };
        access.Permissions.Add(Permissions.CertificateApprove);

        // 3. Approve request
        var approveResult = await service.ReviewActionAsync(new CertificateReviewActionInput
        {
            CertificateRequestId = requestId,
            Action = "Approve",
            CorrectedSignatoryTitle = "Director of Human Resources",
            Comments = "Verified and approved."
        });

        Assert.True(approveResult.Success);

        // 4. Verify Issued Certificate in database
        await using (var db = factory.CreateDbContext())
        {
            var issued = await db.IssuedCertificates.FirstOrDefaultAsync(c => c.CertificateRequestId == requestId);
            Assert.NotNull(issued);
            Assert.StartsWith("CERT-", issued.CertificateNumber);
            Assert.False(string.IsNullOrWhiteSpace(issued.VerificationToken));
            Assert.Equal(32, issued.VerificationToken.Length);
            Assert.False(string.IsNullOrWhiteSpace(issued.Sha256Hash));
            Assert.Equal("Active", issued.Status);
            Assert.Contains("Ahmed Rasheed", issued.SnapshotJson);

            // 5. Test Public Verification (Safe public exposure check)
            var publicVerification = await service.VerifyCertificateAsync(issued.VerificationToken);
            Assert.True(publicVerification.IsValid);
            Assert.Equal(issued.CertificateNumber, publicVerification.CertificateNumber);
            Assert.Equal("Ahmed Rasheed", publicVerification.EmployeeFullName);
            Assert.Equal("Ministry of Digital Development", publicVerification.OrganisationName);
            Assert.Equal("Active", publicVerification.Status);

            // 6. Test Revocation
            var revokeResult = await service.RevokeCertificateAsync(new CertificateRevokeInput
            {
                IssuedCertificateId = issued.IssuedCertificateId,
                Reason = "Incorrect date range specified on original document."
            });
            Assert.True(revokeResult.Success);

            // 7. Verify Revoked State
            var verifyAfterRevoke = await service.VerifyCertificateAsync(issued.VerificationToken);
            Assert.False(verifyAfterRevoke.IsValid);
            Assert.Equal("Revoked", verifyAfterRevoke.Status);
            Assert.Contains("Incorrect date range", verifyAfterRevoke.RevokedReason);
        }
    }
}
