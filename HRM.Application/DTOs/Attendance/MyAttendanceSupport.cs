using HRM.Helpers;
namespace HRM.DTOs.Attendance;
public sealed record ApprovedAttendanceLeave(DateTime FromDate, DateTime ToDate, string? Type);
public sealed record MyAttendanceSupport(HashSet<DateTime> RestDays, Dictionary<DateTime, AttendanceEligibilityDay> Eligibility, List<ApprovedAttendanceLeave> ApprovedLeaves);
