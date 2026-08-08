using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.LeaveTypes
{
    public sealed class LeaveDefinitionSaveRequest
    {
        public int? LeaveDefinitionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = "";

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(200)]
        public string? NameDhivehi { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsGlobal { get; set; }

        public bool IsSystemType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
