using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record RecruitmentRequisitionInput(int PositionMatrixId,int RequestedSlots,string EmploymentType,string RecruitmentType,string Reason,int? OrganisationStructureId=null);
public sealed record RecruitmentRequisitionRow(int Id,string Reference,string Status,int PositionMatrixId,int RequestedSlots,string PolicyVersion,DateTime CreatedAtUtc);
public sealed record RecruitmentVacancyInput(int RequisitionId,DateTime ClosingAtUtc,string AdvertisementSnapshotJson);
public interface IRecruitmentRequisitionService { Task<IReadOnlyList<RecruitmentRequisitionRow>> ListAsync(CancellationToken ct=default); Task<ServiceResult> CreateAsync(RecruitmentRequisitionInput input,CancellationToken ct=default); Task<ServiceResult> ApproveAsync(int requisitionId,CancellationToken ct=default); Task<ServiceResult> CreateVacancyAsync(RecruitmentVacancyInput input,CancellationToken ct=default); Task<ServiceResult> PublishVacancyAsync(int vacancyId,CancellationToken ct=default); }
