using HRM.Enum;

namespace HRM.DTOs.LeaveTypes
{
    public class AvailableLeaveTypeDto
    {
        public int? LeaveTypeId { get; set; }

        public int? LeaveDefinitionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public LeaveFrameworkType Source { get; set; }

        public int SortOrder { get; set; }
    }
}
