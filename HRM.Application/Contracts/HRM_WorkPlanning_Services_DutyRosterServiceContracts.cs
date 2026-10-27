using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.WorkPlanning;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Enum;
namespace HRM.WorkPlanning.Services.Contracts;
public abstract class DutyRosterServiceContracts
{
    public const string TransferType = "Duty Transfer Request", SwapType = "Duty Swap Request";

    // Dated exceptions survive regeneration; templates and recurring attachments remain reusable.
    public const string MovementMarker = "Approved duty change ";

public interface IOperations
{
Task<List<DutyPartner>> GetPartnersAsync()
;
Task<List<DutyRosterRow>> GetRosterAsync(DateTime date)
;
Task<ServiceResult> SubmitAsync(DutyChangeApplication model)
;
Task<List<DutyRequestRow>> GetRequestsAsync()
;
Task<HRM.DTOs.ServiceResult> ActAsync(long id, int expectedState, string action, string? comment = null)
;
}
}
