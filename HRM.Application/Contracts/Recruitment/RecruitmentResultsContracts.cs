using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record PublishRecruitmentResultsInput(int VacancyId,DateTime ObjectionClosesAtUtc);
public sealed record UpdateObjectionClosingDateInput(DateTime ObjectionClosesAtUtc);
public sealed record SubmitRecruitmentObjectionInput(int PublicationId,int ApplicationId,string Grounds);
public sealed record ResolveRecruitmentObjectionInput(int ObjectionId,string Resolution,bool IsUpheld);
public interface IRecruitmentResultService
{
    Task<ServiceResult> PublishAsync(PublishRecruitmentResultsInput input,CancellationToken ct=default);
    Task<ServiceResult> UpdateObjectionClosingDateAsync(int publicationId,DateTime objectionClosesAtUtc,CancellationToken ct=default);
    Task<ServiceResult> SubmitObjectionAsync(SubmitRecruitmentObjectionInput input,CancellationToken ct=default);
    Task<ServiceResult> ResolveObjectionAsync(ResolveRecruitmentObjectionInput input,CancellationToken ct=default);
    Task<ServiceResult> FinalizeAsync(int publicationId,CancellationToken ct=default);
}
