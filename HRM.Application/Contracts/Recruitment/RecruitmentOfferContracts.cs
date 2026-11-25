using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record CreateRecruitmentOfferInput(int MeritResultId,string TermsSnapshotJson);
public sealed record DeclineRecruitmentOfferInput(string? Reason);
public interface IRecruitmentOfferService
{
    Task<ServiceResult> CreateAsync(CreateRecruitmentOfferInput input,CancellationToken ct=default);
    Task<ServiceResult> AcceptMyOfferAsync(int offerId,CancellationToken ct=default);
    Task<ServiceResult> DeclineMyOfferAsync(int offerId,string? reason=null,CancellationToken ct=default);
}
