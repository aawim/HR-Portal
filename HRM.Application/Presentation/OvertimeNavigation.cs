namespace HRM.Application.Presentation;

/// <summary>Applicant updates and reviewer actions have separate portal destinations.</summary>
public static class OvertimeNavigation
{
    public const string StaffUrl = "/online/overtime-requests";
    public const string AdminUrl = "/admin/attendance/manage-overtimes";
    public static bool IsOvertime(string? type, string? url) => url is StaffUrl or AdminUrl ||
        type is "Overtime Claim Request" or "Overtime Preapproval Request" or "Overtime Clock Event Request";
    public static string ReviewUrl(int id) => AdminUrl + "?requestId=" + id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static string? NotificationUrl(string kind, string? type, string? url, int id) => !IsOvertime(type, url) ? null :
        kind == "ActionRequired" ? ReviewUrl(id) : StaffUrl + "?requestId=" + id.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
