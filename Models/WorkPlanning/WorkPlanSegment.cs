using Microsoft.EntityFrameworkCore;

namespace HRM.Models.WorkPlanning
{
    public partial class WorkPlanSegment
    {
        public int WorkPlanSegmentId { get; set; }

        public int WorkPlanId { get; set; }

        public int? WorkTemplateSegmentId { get; set; }

        public int WorkSegmentTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SequenceNumber { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public int GraceBeforeMinutes { get; set; }

        public int GraceAfterMinutes { get; set; }

        public bool IsMandatory { get; set; }

        public bool RequiresAttendance { get; set; }

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool IsPaid { get; set; }

        public bool IsCompleted { get; set; }

        public int? AttendanceId { get; set; }

        public bool IsValid { get; set; }

        public int OperationLogId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public virtual WorkPlan WorkPlan { get; set; } = null!;

        public virtual WorkTemplateSegment? WorkTemplateSegment { get; set; }

        public virtual WorkSegmentType WorkSegmentType { get; set; } = null!;

        
    }
}
