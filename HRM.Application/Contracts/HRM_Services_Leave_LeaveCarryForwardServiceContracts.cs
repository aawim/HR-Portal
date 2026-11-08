using System.Data;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave.Contracts;
public abstract class LeaveCarryForwardServiceContracts
{

public interface IOperations
{
Task<List<CarryForwardReview>> PendingAsync()
;
Task ProcessAsync(CarryForwardReview preview, decimal days, string reason)
;
Task<List<global::HRM.DTOs.Contracts.LeaveCarryForwardDto>> HistoryAsync()
;
}
}
