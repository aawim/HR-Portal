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

        public List<ProfileLeaveTypeDto> LeaveTypes { get; set; } =
            [];

        public List<ProfileJobHistoryDto> JobHistory { get; set; } =
            [];

        public List<ProfileEducationDto> Education { get; set; } =
            [];

        public List<ProfileDocumentDto> Documents { get; set; } =
            [];

        public bool HasActiveJob =>
            ActiveJob?.IsActive == true;

        public bool HasEmploymentHistory =>
            JobHistory.Count > 0 ||
            HasActiveJob;

        //public int IndividualId { get; set; }

        //public int BusinessEntityId { get; set; }

        //public string FullName { get; set; } = string.Empty;

        //public string? FullNameDhivehi { get; set; }

        //public string? IdentityCardNumber { get; set; }

        //public DateTime? DateOfBirth { get; set; }

        //public string? GenderName { get; set; }

        //public string? NationalityName { get; set; }

        //public string? ImageUrl { get; set; }

        //public bool IsIndividualActive { get; set; }

        //public ProfileType ProfileType { get; set; }

        //// Personal sections
        //public List<ProfileContactDto> Contacts { get; set; } = [];

        //public List<ProfileAddressDto> Addresses { get; set; } = [];

        //public List<ProfileEducationDto> Education { get; set; } = [];

        //public List<ProfileDocumentDto> Documents { get; set; } = [];

        //// Employment sections
        //public ActiveJobDto? ActiveJob { get; set; }

        //public List<ProfilePositionDto> ActivePositions { get; set; } = [];

        //public List<ProfileLeaveTypeDto> LeaveTypes { get; set; } = [];

        //public List<ProfileJobHistoryDto> JobHistory { get; set; } = [];

        //public bool HasActiveJob => ActiveJob is not null;

        //public bool HasEmploymentHistory =>
        //    ActiveJob is not null || JobHistory.Count > 0;

        //public bool IsStaff =>
        //    ProfileType == ProfileType.Staff;

        //public bool IsFormerStaff =>
        //    ProfileType == ProfileType.FormerStaff;
    }
}
