using System.ComponentModel.DataAnnotations;
using HRM.DTOs;
namespace HRM.Application.Contracts.Pages;

public sealed record OutOfOfficeFilter(DateOnly From, DateOnly To, int? StateId = null, int? OrganisationId = null,
    int? TeamId = null, int? StaffId = null);
public sealed record OutOfOfficeRequestRow(int RequestId, string Reference, int StateId, string Status, int StaffId,
    string StaffName, string? SupervisorName, DateOnly Date, TimeOnly ExpectedOut, TimeOnly ExpectedIn,
    DateTime? ActualOut, DateTime? ActualIn, string? Remarks);
public sealed record OutOfOfficeOptions(List<DirectoryOption> Organisations, List<DirectoryOption> Teams,
    List<RequestStateOption> States, List<DirectoryOption> RequestTypes);
public sealed record OutOfOfficeTeamMembers(List<DirectoryOption> Staff, List<DirectoryOption> Supervisors);

public sealed class OutOfOfficeApplication
{
    public int? RequestId { get; set; }
    public int? ExpectedStateId { get; set; }
    public int? ApplicantJobId { get; set; }
    [Range(1, int.MaxValue)] public int RequestTypeId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly ExpectedOut { get; set; }
    public TimeOnly ExpectedIn { get; set; }
    [Required, StringLength(2000)] public string Remarks { get; set; } = "";
}

/// <summary>Derives ownership and approvers on the server. ApplicantJobId is only accepted for authorised on-behalf submissions.</summary>
public interface IOutOfOfficeRequests
{
    Task<OutOfOfficeOptions> GetOptionsAsync(CancellationToken ct = default);
    Task<OutOfOfficeTeamMembers> GetTeamMembersAsync(int teamId, CancellationToken ct = default);
    Task<List<OutOfOfficeRequestRow>> GetMineAsync(OutOfOfficeFilter filter, CancellationToken ct = default);
    Task<List<OutOfOfficeRequestRow>> GetReceivedAsync(OutOfOfficeFilter filter, CancellationToken ct = default);
    Task<OutOfOfficeApplication?> GetEditableAsync(int requestId, CancellationToken ct = default);
    Task<ServiceResult> SubmitAsync(OutOfOfficeApplication application, CancellationToken ct = default);
    Task<ServiceResult> ActAsync(int requestId, int expectedState, string action, string? comment, CancellationToken ct = default);
}
