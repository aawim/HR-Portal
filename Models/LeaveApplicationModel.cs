using System.ComponentModel.DataAnnotations;

namespace HRM.Models
{
    public class LeaveApplicationModel
    {
        [Required(ErrorMessage = "Please select a leave type.")]
        public string LeaveType { get; set; } = string.Empty;
        public int LeaveTypeId { get; set; } = 0;
        public DateTime FromDate { get; set; } = DateTime.Today;

        public DateTime ToDate { get; set; } = DateTime.Today;

        public int Duration { get; set; } = 1;

        public int ReasonId { get; set; }

        public string Explanation { get; set; } = string.Empty;

        public int SubTypeId { get; set; }

        public int CountryId { get; set; }

        public int AtollId { get; set; }

        public int IslandId { get; set; }

        public string Address { get; set; } = string.Empty;

        public string Mobile { get; set; } = string.Empty;

        public bool HasAgreedToTerms { get; set; }
    }
}
