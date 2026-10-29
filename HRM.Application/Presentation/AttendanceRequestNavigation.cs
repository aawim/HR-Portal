namespace HRM.Application.Presentation;

public static class AttendanceRequestNavigation
{
    public const string AdminUrl = "/admin/attendance/manage-attendance/attendance-requests";
    public static string ReviewUrl(int id) => AdminUrl + "?requestId=" + id.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
