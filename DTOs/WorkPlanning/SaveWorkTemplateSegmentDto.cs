using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.WorkPlanning
{
    public class SaveWorkTemplateSegmentDto
    {
        public int WorkTemplateSegmentId { get; set; }

        [Required]
        public int WorkTemplateId { get; set; }

        [Required(ErrorMessage = "Segment name is required.")]
        [StringLength(
            150,
            ErrorMessage = "Segment name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(
            500,
            ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
         
        public DateOnly? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsMandatory { get; set; }

        public bool IsPaid { get; set; } = true;

        public bool RequiresAttendance { get; set; } = true;

        public bool RequiresLocationValidation { get; set; }

        public bool RequiresDeviceValidation { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public int OperationLogID { get; set; }



        [Required(ErrorMessage = "Please select a segment type.")]
        [Range(1, int.MaxValue)]
        public int WorkSegmentTypeId { get; set; }

    

        [Range(1, int.MaxValue)]
        public int SequenceNumber { get; set; }

        [Range(0, int.MaxValue)]
        public int OffsetMinutes { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than zero.")]
        public int DurationMinutes { get; set; }

        [Range(0, int.MaxValue)]
        public int GraceBeforeMinutes { get; set; }

        [Range(0, int.MaxValue)]
        public int GraceAfterMinutes { get; set; }

      

     
    }
}
