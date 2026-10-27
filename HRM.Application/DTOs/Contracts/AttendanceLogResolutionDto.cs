namespace HRM.DTOs.Contracts;

public sealed class AttendanceLogResolutionDto
{
    public long AttendanceLogResolutionId { get; set; }
    public int AttendanceLogId { get; set; }
    public long? WorkPlanId { get; set; }
    public long? WorkAssignmentId { get; set; }
    public long? WorkAssignmentSegmentId { get; set; }
    public int? JobId { get; set; }
    public int AttendanceResolutionStatusId { get; set; }
    public DateTime ResolutionDate { get; set; }
    public string? ResolutionMessage { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? DeviceIdentifier { get; set; }
    public bool? IsLocationValidated { get; set; }
    public bool? IsDeviceValidated { get; set; }
    public int? AttendanceClockTypeId { get; set; }
    public int? WorkPlanSegmentId { get; set; }
    public int? OperationLogId { get; set; }
    public bool IsValid { get; set; }
    public DateTime CreatedDate { get; set; }
}
