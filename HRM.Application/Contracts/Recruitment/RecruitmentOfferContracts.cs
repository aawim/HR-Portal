using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record CreateRecruitmentOfferInput(int MeritResultId,string TermsSnapshotJson);
public sealed record AcceptRecruitmentOfferInput(
    bool AgreedToTerms,
    bool AgreedToCodeOfConduct,
    bool ConfirmedInformationAccuracy,
    DateTime ConfirmedJoiningDate,
    string? EmergencyContactName=null,
    string? EmergencyContactPhone=null,
    string? BankName=null,
    string? BankAccountNumber=null,
    string? AppointeeNotes=null,
    string? SignatureDataUrl=null,
    bool IsCurrentlyEmployed=false,
    string? CurrentEmployer=null,
    bool RequiresWrittenNotice=false,
    string? NoticePeriod=null);
public sealed record DeclineRecruitmentOfferInput(string? Reason);
public sealed record UpdateOfferTermsInput(string TermsSnapshotJson);
public sealed record HrOfferDecisionInput(string Decision,string? Comment);
public interface IRecruitmentOfferService
{
    Task<ServiceResult> CreateAsync(CreateRecruitmentOfferInput input,CancellationToken ct=default);
    Task<ServiceResult> UpdateTermsAsync(int offerId,string termsSnapshotJson,CancellationToken ct=default);
    Task<ServiceResult> PublishAsync(int offerId,CancellationToken ct=default);
    Task<ServiceResult> AcceptMyOfferAsync(int offerId,AcceptRecruitmentOfferInput? input=null,CancellationToken ct=default);
    Task<ServiceResult> DeclineMyOfferAsync(int offerId,string? reason=null,CancellationToken ct=default);
    Task<ServiceResult> RecordHrDecisionAsync(int offerId,HrOfferDecisionInput input,CancellationToken ct=default);
}
