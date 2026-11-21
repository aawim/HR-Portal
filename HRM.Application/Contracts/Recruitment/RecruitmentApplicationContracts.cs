using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record ApplicantRegistrationInput(string FullName,string Email,string? Mobile);
public sealed record SubmitRecruitmentApplicationInput(int ApplicantId,int VacancyId,string ProfileSnapshotJson);
public sealed record PublishedRecruitmentVacancyRow(int Id,string Reference,int VacancySlots,DateTime ClosingAtUtc);
public interface IRecruitmentApplicationService { Task<IReadOnlyList<PublishedRecruitmentVacancyRow>> ListPublishedVacanciesAsync(CancellationToken ct=default); Task<ServiceResult> RegisterAsync(ApplicantRegistrationInput input,CancellationToken ct=default); Task<ServiceResult> SubmitAsync(SubmitRecruitmentApplicationInput input,CancellationToken ct=default); }
