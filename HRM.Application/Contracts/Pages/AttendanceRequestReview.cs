using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;

public sealed record AttendanceReview(int RequestId,int StateId,string Stage,bool CanApprove,bool CanCancel,string ActionLabel,string Message);
public interface IAttendanceRequestReview
{
    Task<AttendanceReview> GetAsync(int requestId);
    Task<ServiceResult> ActAsync(int requestId,int expectedState,string action,string? comment);
}
