using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Separation;

namespace HRM.Services.Contracts;

public abstract class StaffSeparationServiceContracts
{
    public const string RequestTypeName = "Staff Separation and Offboarding";
    public const string AdminPageUrl = "/admin/hr/separations";
    public const string OnlinePageUrl = "/online/staff-separation";

    public interface IOperations
    {
        Task<StaffSeparationDashboardData> LoadDashboardAsync(int? organisationId = null, string? statusFilter = null, string? search = null);
        Task<StaffSeparationFormLookupsDto> LoadFormLookupsAsync(int? organisationId = null);
        Task<StaffSeparationDetailDto> LoadDetailAsync(int? staffSeparationId = null, int? individualId = null, int? jobId = null);
        Task<ServiceResult> InitiateSeparationAsync(StaffSeparationInitiateInput input);
        Task<ServiceResult> AcknowledgeExitAsync(StaffSeparationEmployeeAcknowledgeInput input);
        Task<ServiceResult> SaveHandoverItemAsync(StaffSeparationHandoverItemInput input);
        Task<ServiceResult> DeleteHandoverItemAsync(int workHandoverItemId);
        Task<ServiceResult> ReviewHandoverAsync(StaffSeparationHandoverReviewInput input);
        Task<ServiceResult> InspectAssetClearanceAsync(StaffSeparationAssetInspectionInput input);
        Task<ServiceResult> SaveFinanceItemAsync(StaffSeparationFinanceItemInput input);
        Task<ServiceResult> DeleteFinanceItemAsync(int separationFinanceItemId);
        Task<ServiceResult> ProcessClearanceTaskAsync(StaffSeparationClearanceTaskActionInput input);
        Task<ServiceResult> ProcessAccessRevocationAsync(StaffSeparationAccessRevocationActionInput input);
        Task<ServiceResult> FinalReviewAndExecuteAsync(StaffSeparationFinalReviewInput input);
        Task<SeparationDocumentDto> GenerateDocumentAsync(int staffSeparationId, string documentVariant);
    }
}
