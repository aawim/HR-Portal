using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplateSegmentEditViewModel
    {

        public int WorkTemplateSegmentId { get; set; }

        [Required]
        public int WorkTemplateId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a segment type.")]
        public int WorkSegmentTypeId { get; set; }

        [Required(ErrorMessage = "Segment name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sequence number must be greater than zero.")]
        public int SequenceNumber { get; set; } = 1;

        [Range(0, 168, ErrorMessage = "Offset hours must be between 0 and 168.")]
        public int OffsetHours { get; set; }

        [Range(0, 59, ErrorMessage = "Offset minutes must be between 0 and 59.")]
        public int OffsetMinutePart { get; set; }

        [Range(0, 168, ErrorMessage = "Duration hours must be between 0 and 168.")]
        public int DurationHours { get; set; }

        [Range(0, 59, ErrorMessage = "Duration minutes must be between 0 and 59.")]
        public int DurationMinutePart { get; set; }

        [Range(0, 1440)]
        public int GraceBeforeMinutes { get; set; }

        [Range(0, 1440)]
        public int GraceAfterMinutes { get; set; }

        public bool IsMandatory { get; set; } = true;

        public bool RequiresAttendance { get; set; } = true;

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public bool IsActive { get; set; } = true;

        //public int OffsetMinutes =>
        //    (OffsetHours * 60) + OffsetMinutePart;

        //public int DurationMinutes =>
        //    (DurationHours * 60) + DurationMinutePart;



        public int OffsetMinutes
        {
            get => (OffsetHours * 60) + OffsetMinutePart;
            set
            {
                OffsetHours = value / 60;
                OffsetMinutePart = value % 60;
            }
        }
 


        public int DurationMinutes
        {
            get => (DurationHours * 60) + DurationMinutePart;
            set
            {
                DurationHours = value / 60;
                DurationMinutePart = value % 60;
            }
        }

        public int OperationLogID { get; set; }

        public bool IsPaid { get; set; }
    }
}
