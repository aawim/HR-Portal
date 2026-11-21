using HRM.Application.Contracts.Recruitment;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

/// <summary>Bridges an accepted offer to the existing approval-controlled staff onboarding workflow.</summary>
public sealed class RecruitmentOnboardingHandoffService(
    IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    StaffOnboardingServiceContracts.IOperations onboarding) : IRecruitmentOnboardingHandoffService
{
    public async Task<ServiceResult> SubmitAsync(SubmitRecruitmentOnboardingInput input, CancellationToken ct = default)
    {
        if (input.IndividualId <= 0 || input.OrganisationStructureId <= 0 || input.JobTypeId <= 0 || input.JoinedDate.Year is < 1900 or > 2200)
            return ServiceResult.Fail("Provide valid onboarding details.");
        var organisationId = await access.GetCurrentOrganisationIdAsync();
        if (organisationId <= 0 || !await access.HasOrganisationAccessAsync(organisationId) || !await access.HasPermissionAsync("Recruitment.Manage"))
            throw new UnauthorizedAccessException("Recruitment management permission is required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var offer = await (from item in db.RecruitmentOffers
                           join merit in db.RecruitmentMeritResults on item.RecruitmentMeritResultId equals merit.Id
                           join application in db.RecruitmentApplications on merit.RecruitmentApplicationId equals application.Id
                           join applicant in db.RecruitmentApplicants on application.RecruitmentApplicantId equals applicant.Id
                           join idCard in db.Idcards on applicant.EfaasIdNumber equals idCard.IdcardNumber
                           where item.Id == input.OfferId && item.OrganisationId == organisationId && item.Status == "Accepted" && idCard.BusinessEntityId == input.IndividualId
                           select item).SingleOrDefaultAsync(ct);
        if (offer is null) return ServiceResult.Fail("The accepted offer is not identity-matched to the selected Individual record.");
        if (offer.OnboardingRequestId.HasValue) return ServiceResult.Fail("This accepted offer is already linked to an onboarding request.");
        var result = await onboarding.SubmitAsync(new StaffOnboardingForm
        {
            IndividualId = input.IndividualId, OrganisationStructureId = input.OrganisationStructureId,
            JobTypeId = input.JobTypeId, PositionId = input.PositionId,
            JoinedDate = input.JoinedDate.Date, Remarks = input.Remarks?.Trim() ?? string.Empty
        });
        if (!result.Success) return result;
        var requestId = await db.StaffOnboardingRequests
            .Where(x => x.IndividualId == input.IndividualId && x.OrganisationId == organisationId && x.JoinedDate == input.JoinedDate.Date && x.IsOpen)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => (int?)x.RequestId).FirstOrDefaultAsync(ct);
        if (!requestId.HasValue) return ServiceResult.Fail("Onboarding was submitted but its request could not be linked automatically.");
        offer.OnboardingRequestId = requestId.Value;
        offer.Status = "OnboardingSubmitted";
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Accepted offer submitted to the existing onboarding approval workflow.");
    }
}
