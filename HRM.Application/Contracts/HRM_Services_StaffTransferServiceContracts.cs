using HRM.DTOs;
using HRM.DTOs.StaffTransfer;

namespace HRM.Services.Contracts;

public abstract class StaffTransferServiceContracts
{
    public const string RequestTypeName = "Internal Staff Transfer Request";
    public const string PageUrl = "/admin/hr/staff-transfers";
    public const string OnlineConfirmationUrl = "/online/staff-transfers";

    public interface IOperations
    {
        Task<StaffTransferDashboardData> LoadDashboardAsync(int? organisationId = null, string? statusFilter = null);
        Task<StaffTransferDetailDto> LoadDetailAsync(int? staffTransferId = null, int? individualId = null, int? jobId = null);
        Task<ServiceResult> SaveAsync(StaffTransferSaveInput input);
        Task<ServiceResult> AddCommentAsync(int staffTransferId, string commentKind, string commentText, bool isVisibleToEmployee = true);
        Task<ServiceResult> SubmitEmployeeConfirmationAsync(StaffTransferEmployeeConfirmationInput input);
        Task<ServiceResult> SubmitReceivingDecisionAsync(StaffTransferDecisionInput input);
        Task<TransferActivationResult> ActivateTransferAsync(int staffTransferId);
        Task<ServiceResult> UpdateDoorAccessProvisioningAsync(int staffTransferDoorAccessId, string provisioningState, string? notes);
    }
}
