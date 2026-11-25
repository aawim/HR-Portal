using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record ApplicantRegistrationInput(string FullName,string Email,string? Mobile);
public sealed record SubmitRecruitmentApplicationInput(int ApplicantId,int VacancyId,string ProfileSnapshotJson);
public sealed record PublishedRecruitmentVacancyRow(int Id,string Reference,int VacancySlots,DateTime ClosingAtUtc,string PositionName="",int OrganisationId=0,string? OrganisationName=null,string? EmploymentType=null,bool HasPublishedResults=false,int? PublicationId=null,bool IsResultFinal=false,DateTime? ObjectionClosesAtUtc=null);
public sealed record OfficeVacanciesResponse(int OrganisationId,string OrganisationName,string OfficeSlug,IReadOnlyList<PublishedRecruitmentVacancyRow> Vacancies);
public sealed record PublicMeritSheetResponse(int PublicationId,string VacancyReference,string? PositionName,string? OrganisationName,bool IsFinal,DateTime PublishedAtUtc,DateTime ObjectionClosesAtUtc,IReadOnlyList<PublishedMeritSheetEntry> MeritSheet);
public interface IRecruitmentApplicationService { Task<IReadOnlyList<PublishedRecruitmentVacancyRow>> ListPublishedVacanciesAsync(CancellationToken ct=default); Task<ServiceResult> RegisterAsync(ApplicantRegistrationInput input,CancellationToken ct=default); Task<ServiceResult> SubmitAsync(SubmitRecruitmentApplicationInput input,CancellationToken ct=default); }
