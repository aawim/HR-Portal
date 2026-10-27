namespace HRM.DTOs.Leave;

public sealed class LeaveReceiptDto
{
    public int LeaveId { get; set; }
    public string Reference { get; set; } = "";
    public string Organisation { get; set; } = "";
    public string? OrganisationDhivehi { get; set; }
    public string? RegistrationNumber { get; set; }
    public string StaffName { get; set; } = "";
    public string? StaffNumber { get; set; }
    public string LeaveType { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime CalendarReturnDate => ToDate.Date.AddDays(1);
    public int Days { get; set; }
    public decimal? CurrentBalance { get; set; }
    public DateTime? ReportingDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
