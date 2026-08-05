using HRM.DTOs.Profile;

namespace HRM.DTOs.UserContext
{
    public class StaffProfileDto
    {
        //public ProfileHeaderDto Header { get; set; } = new();

        public ActiveJobDto? ActiveJob { get; set; }

        public List<ProfileContactDto> Contacts { get; set; } =
            [];

        public List<ProfileAddressDto> Addresses { get; set; } =
            [];

        public List<ProfileEducationDto> Education { get; set; } =
            [];

        public List<ProfileLeaveTypeDto> LeaveTypes { get; set; } =
            [];

        public List<ProfilePositionDto> Positions { get; set; } =
            [];

        public List<ProfileJobHistoryDto> JobHistory { get; set; } =
            [];
    }
}
