using HRM.DTOs.JobPosition;
using HRM.DTOs.Profile.ProfileAccess;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Enum;

namespace HRM.DTOs.Profile
{
    public class ProfileOverviewDto
    {
        public string? ImageUrl { get; set; }

        public bool IsIndividualActive { get; set; }

        public int IndividualId { get; set; }

        public int BusinessEntityId { get; set; }

        public string FullName { get; set; } =
            string.Empty;

        public string FullNameDhivehi { get; set; } =
            string.Empty;

        public string? IdentityCardNumber { get; set; }

        public ProfileType ProfileType { get; set; }

        public ActiveJobDto? ActiveJob { get; set; }

        public List<ProfilePositionDto> ActivePositions { get; set; } =
            [];

        public List<ProfileContactDto> Contacts { get; set; } =
            [];

        public List<ProfileAddressDto> Addresses { get; set; } =
            [];

        public List<JobLeaveTypeDto> LeaveTypes { get; set; } =
            [];

        public List<JobPositionHistoryDto> JobHistory { get; set; } =
            [];

        public List<JobPositionHistoryDto> PositionHistory { get; set; } = [];

        public List<ProfileEducationDto> Education { get; set; } =
            [];

        public List<ProfileDocumentDto> Documents { get; set; } =
            [];

        public List<ProfileSupervisingGroupDto> SupervisingTeams { get; set; } = [];

        



        public bool HasActiveJob =>
            ActiveJob?.IsActive == true;

        public bool HasEmploymentHistory =>
            PositionHistory.Count > 0 ||
            HasActiveJob;


        public DateTime DateOfBirth { get; set; }

        public int GenderId { get; set; }

        //public string GenderName { get; set; } =
        //    string.Empty;

        public int? NationalityId { get; set; }

        public string NationalityName { get; set; } = string.Empty;

        public string StaffNo { get; set; } = string.Empty;

        public ProfileAccessDto Access { get; set; } = new();
    }
}
