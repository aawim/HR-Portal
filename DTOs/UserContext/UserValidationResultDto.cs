using HRM.Enum;

namespace HRM.DTOs.UserContext
{
    public class UserValidationResultDto
    {
      

        public UserEligibility Eligibility { get; set; }

        public List<string> Errors { get; set; } = new();

        public bool IsEligible =>
            Eligibility == UserEligibility.Eligible;
    }
}
