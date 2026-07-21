using HRM.Models.WorkPlanning;

namespace HRM.Models
{
    public partial class AttendanceLogResolution
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

        public int? OperationLogId { get; set; }

        public bool IsValid { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual AttendanceLog AttendanceLog { get; set; } = null!;

        public virtual AttendanceResolutionStatus
            AttendanceResolutionStatus
        { get; set; } = null!;

        public virtual WorkPlan? WorkPlan { get; set; }

        public virtual WorkAssignment? WorkAssignment { get; set; }

        public virtual WorkAssignmentSegment?
            WorkAssignmentSegment
        { get; set; }
    }
}
