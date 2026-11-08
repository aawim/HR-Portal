using HRM.Models.WorkPlanning;
namespace HRM.Models;
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

        public int? AttendanceClockTypeId { get; set; }

        public int? WorkPlanSegmentId { get; set; }
        
        public int? OperationLogId { get; set; }

        public bool IsValid { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsPrimaryClassification { get; set; } = true;

        public string? SchedulePurpose { get; set; }

        public string? LogicalEventType { get; set; }

        public string? MatchMethod { get; set; }

        public string? MatchReason { get; set; }

        public string ConfidenceState { get; set; } = "Deterministic";

        public string PolicyVersion { get; set; } = "v2.0-OfficialPriority";

        public long? ReconciliationRunId { get; set; }

        public long? AttendanceObligationId { get; set; }

        public int? OverriddenByUserId { get; set; }

        public string? OverrideReason { get; set; }

        public DateTime? OverriddenAtUtc { get; set; }

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
