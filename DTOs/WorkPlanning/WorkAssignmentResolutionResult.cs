namespace HRM.DTOs.WorkPlanning
{
    public class WorkAssignmentResolutionResult
    {



        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public long? WorkPlanId { get; set; }

        public long? WorkAssignmentId { get; set; }

        public long? WorkAssignmentSegmentId { get; set; }

        public int? IndividualId { get; set; }

        public int? JobId { get; set; }

        public string? AssignmentName { get; set; }

        public string? SegmentName { get; set; }

        public DateTime? AssignmentStartDateTime { get; set; }

        public DateTime? AssignmentEndDateTime { get; set; }

        public DateTime? SegmentStartDateTime { get; set; }

        public DateTime? SegmentEndDateTime { get; set; }

        public int GraceBeforeMinutes { get; set; }

        public int GraceAfterMinutes { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresCheckOut { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool IsInsideScheduledPeriod { get; set; }

        public bool IsInsideGracePeriod { get; set; }

        public static WorkAssignmentResolutionResult Failed(string message)
        {
            return new WorkAssignmentResolutionResult
            {
                Success = false,
                Message = message
            };
        }




        // Old code for reference, will be deleted once done the implementation

        //public bool Success { get; set; }

        //public string Message { get; set; } = string.Empty;

        //public int? WorkPlanId { get; set; }

        //public int? WorkAssignmentId { get; set; }

        //public int? WorkAssignmentSegmentId { get; set; }

        //public DateTime? ScheduledStartDateTime { get; set; }

        //public DateTime? ScheduledEndDateTime { get; set; }

        //public int GraceBeforeMinutes { get; set; }

        //public int GraceAfterMinutes { get; set; }

        //public bool RequiresAttendance { get; set; }

        //public bool RequiresLocationValidation { get; set; }

        //public bool RequiresDeviceValidation { get; set; }

        //public static WorkAssignmentResolutionResult Failed(string message)
        //{
        //    return new WorkAssignmentResolutionResult
        //    {
        //        Success = false,
        //        Message = message
        //    };
        //}
    }
}
