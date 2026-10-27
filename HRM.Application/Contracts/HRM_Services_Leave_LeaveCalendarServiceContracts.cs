using System.Data;
using HRM.Components.Shared;
using HRM.DTOs.Leave;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave.Contracts;
public abstract class LeaveCalendarServiceContracts
{

public interface IOperations
{
Task<List<LeaveCalendarEntry>> LoadAsync(DateTime month, bool includeTeam, int? applicantJobId = null)
;
Task SavePlanAsync(DateTime from, DateTime to, string title)
;
Task RemovePlanAsync(int id)
;
}
}
