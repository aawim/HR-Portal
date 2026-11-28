using HRM.Application.Features.GoodsLoans;
using HRM.DTOs.GoodsLoans;
using HRM.DTOs.Staff;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.GoodsLoans;
using HRM.Services;
using HRM.Services.GoodsLoans;
using HRM.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task StaffBankService_BlocksCrossTenantAndCrossStaffAccess()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        using (var db = factory.CreateDbContext())
        {
            db.Jobs.Add(new Job { JobId = 20, IndividualID = 200, OrganisationID = 2, OrganisationStructureId = 1, JobStateId = 4, JoinedDate = DateTime.Today.AddMonths(-1) });
            await db.SaveChangesAsync();
        }

        // HR user in Org 1 trying to access/modify Staff 200 in Org 2
        var org1Hr = FakeAccess(individualId: 100, userId: 10, orgId: 1, isHr: true);
        var service = new StaffBankService(factory, org1Hr, NullLogger<StaffBankService>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAccountsAsync(200));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveAccountAsync(new StaffBankAccountInputDto
        {
            IndividualId = 200,
            BankName = "BML",
            AccountHolderName = "Other Staff",
            AccountNumber = "7730000123456"
        }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetPrimaryAsync(1, 200));
    }

    [Fact]
    public async Task StaffEmergencyContactService_BlocksCrossTenantAndCrossStaffAccess()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        using (var db = factory.CreateDbContext())
        {
            db.Jobs.Add(new Job { JobId = 20, IndividualID = 200, OrganisationID = 2, OrganisationStructureId = 1, JobStateId = 4, JoinedDate = DateTime.Today.AddMonths(-1) });
            await db.SaveChangesAsync();
        }

        var org1Hr = FakeAccess(individualId: 100, userId: 10, orgId: 1, isHr: true);
        var service = new StaffEmergencyContactService(factory, org1Hr, NullLogger<StaffEmergencyContactService>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetContactsAsync(200));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveContactAsync(new StaffEmergencyContactInputDto
        {
            IndividualId = 200,
            ContactName = "Contact",
            Relationship = "Spouse",
            PrimaryPhone = "7771234"
        }));
    }

    [Fact]
    public async Task GoodsLoanService_BlocksCrossTenantQueriesAndUnauthorizedApprovals()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        using (var db = factory.CreateDbContext())
        {
            db.GoodsLoanStores.Add(new GoodsLoanStore { StoreID = 1, StoreCode = "ST1", StoreName = "Store 1", IsActive = true, CreatedDate = DateTime.UtcNow });
            db.GoodsLoanStoreAgreements.Add(new GoodsLoanStoreAgreement { AgreementID = 1, StoreID = 1, OrganisationBusinessEntityID = 2, AgreementNumber = "AGR-1", EffectiveFrom = DateTime.Today.AddMonths(-1), MinimumMonths = 1, MaximumMonths = 12, MaximumLoanAmount = 10000m, IsActive = true, CreatedDate = DateTime.UtcNow });
            db.GoodsLoans.Add(new GoodsLoan
            {
                LoanID = 1,
                LoanNumber = "GL-ORG2-001",
                OrganisationBusinessEntityID = 2,
                IndividualID = 200,
                JobID = 20,
                StoreID = 1,
                AgreementID = 1,
                PrincipalAmount = 5000m,
                TotalRepayableAmount = 5000m,
                RepaymentMonths = 5,
                FirstDeductionMonth = new DateTime(2026, 12, 1),
                Status = "PendingApproval",
                CreatedDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var org1Hr = FakeAccess(individualId: 100, userId: 10, orgId: 1, isHr: true);
        var calc = new GoodsLoanScheduleCalculator();
        var service = new GoodsLoanService(factory, calc, org1Hr, NullLogger<GoodsLoanService>.Instance);

        // Cross-tenant reads blocked
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStoresAsync(organisationId: 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAgreementsAsync(organisationId: 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetPoliciesAsync(organisationId: 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetActivePolicyAsync(organisationId: 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStoreSettlementsAsync(organisationId: 2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetStoreMonthlyDeductionsReportAsync(organisationId: 2, DateTime.Today));

        // Cross-tenant mutations blocked
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveLoanAsync(1, 10));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RejectLoanAsync(1, 10, "Cross-tenant attempt"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CancelLoanAsync(1, 10, "Cross-tenant attempt"));

        // Non-privileged staff cannot view another staff member's summary in same org
        var regularStaff = FakeAccess(individualId: 100, userId: 10, orgId: 1, isHr: false);
        var staffService = new GoodsLoanService(factory, calc, regularStaff, NullLogger<GoodsLoanService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => staffService.GetStaffWorkspaceSummaryAsync(individualId: 101, organisationId: 1));
    }

    private static IUserAccessService FakeAccess(int individualId, int userId, int orgId, bool isHr)
    {
        var ctx = new UserContextDto
        {
            UserId = userId,
            IndividualId = individualId,
            IsSuperAdministrator = false,
            IsHRStaff = isHr,
            IsHRHead = false,
            IsHRManager = isHr,
            IsOrganisationAdmin = false,
            IsSupervisor = false
        };
        return StubProxy.For<IUserAccessService>((method, args) => method switch
        {
            nameof(IUserAccessService.RequireContextAsync) => Task.FromResult(ctx),
            nameof(IUserAccessService.GetContextAsync) => Task.FromResult<UserContextDto?>(ctx),
            nameof(IUserAccessService.GetCurrentOrganisationIdAsync) => Task.FromResult(orgId),
            nameof(IUserAccessService.HasOrganisationAccessAsync) => Task.FromResult(args.Length > 0 && args[0] is int requested && requested == orgId),
            nameof(IUserAccessService.HasPermissionAsync) => Task.FromResult(false),
            nameof(IUserAccessService.HasUserGroupAsync) => Task.FromResult(false),
            _ => Task.FromResult(false)
        });
    }
}
