using HRM.DTOs.Attendance;
namespace HRM.Application.Contracts.Pages;

public sealed record RequestStateOption(int Id, string Name, bool IsProcessingState, bool IsFinalState);
public sealed record AttendanceRequestFilter(DateOnly From, DateOnly To, int? StateId = null, int? OrganisationId = null, int? StaffId = null);
public sealed record AttendanceRequestRow(int RequestId, string Reference, string Kind, DateTime ClockTime,
    int StateId, string Status, string StaffName, string OrganisationName, string Mode, string? Comments);
public sealed record AttendanceRequestPage(List<RequestStateOption> States, List<AttendanceRequestRow> Requests);

/// <summary>Personal reads derive the applicant from the authenticated session. Received reads require attendance permission.</summary>
public interface IAttendanceRequestQueries
{
    Task<AttendanceRequestPage> GetMineAsync(AttendanceRequestFilter filter, CancellationToken ct = default);
    Task<AttendanceRequestPage> GetReceivedAsync(AttendanceRequestFilter filter, CancellationToken ct = default);
}
