using HRM.Models;
namespace HRM.Models
{
    public partial class AttendanceResolutionStatus
    {
        public int AttendanceResolutionStatusId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsValid { get; set; }

        public virtual ICollection<AttendanceLogResolution> AttendanceLogResolutions { get; set; }
            = new List<AttendanceLogResolution>();
    }
}
