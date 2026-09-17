using HRM.Enum;

namespace HRM.DTOs.Attendance
{
    public class AttendanceLogDto
    {
        public int AttendanceLogID { get; set; }

        public int? AttendanceDeviceID { get; set; }

        public int IndividualID { get; set; }

        public int OrganisationID { get; set; }

        public int? OrganisationStructureID { get; set; }

        // Legacy / physical device value.
        // Keep for compatibility, but don't use it to determine
        // Check In / Check Out in the new attendance system.
        public int InOutModeID { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public int Second { get; set; }

        public DateTime Date { get; set; }

        public Guid UIDStamp { get; set; }

        public int AttendanceLogModeID { get; set; }

        public int AttendanceLogStateID { get; set; }

        public int? OperationLogID { get; set; }

        public int? RelatedAttendanceLogID { get; set; }

        public int? ActualInOutMode { get; set; }


        // =====================================================
        // New WorkPlanning / Resolution properties
        // =====================================================

        public long? AttendanceLogResolutionID { get; set; }

        public long? WorkPlanID { get; set; }

        public long? WorkAssignmentID { get; set; }

        public long? WorkAssignmentSegmentID { get; set; }

        public int? WorkPlanSegmentID { get; set; }

        public int? JobID { get; set; }

        public AttendanceClockType? ResolvedClockType { get; set; }

        public string? ResolutionStatusName { get; set; }

        public string? SegmentName { get; set; }


        // =====================================================
        // Display Properties
        // =====================================================

        public string? IndividualName { get; set; }

        public string? OrganisationName { get; set; }

        public string? OrganisationStructureName { get; set; }

        public string? InOutModeName { get; set; }

        public string? AttendanceLogModeName { get; set; }

        public string? AttendanceLogStateName { get; set; }


        // =====================================================
        // Computed Properties
        // =====================================================

        public bool IsValid =>
            AttendanceLogStateName?.Equals(
                "Valid",
                StringComparison.OrdinalIgnoreCase) == true;

        public bool IsInvalid =>
            AttendanceLogStateName?.Equals(
                "Invalid",
                StringComparison.OrdinalIgnoreCase) == true;

        public TimeOnly Time =>
            new(Hour, Minute, Second);


        // =====================================================
        // New computed attendance properties
        // =====================================================
      


        //public bool IsResolved =>
        //    ResolvedClockType.HasValue &&
        //    ResolvedClockType != AttendanceClockType.Unresolved;


        public bool IsCheckIn =>
            ResolvedClockType == AttendanceClockType.CheckIn;

        public bool IsCheckOut =>
            ResolvedClockType == AttendanceClockType.CheckOut;

        public bool IsIgnored =>
            ResolvedClockType == AttendanceClockType.Ignored;

        public bool IsResolved =>
            ResolvedClockType == AttendanceClockType.CheckIn ||
            ResolvedClockType == AttendanceClockType.CheckOut;
    }
}
