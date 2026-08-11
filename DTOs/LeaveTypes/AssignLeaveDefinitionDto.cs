namespace HRM.DTOs.LeaveTypes
{
    public class AssignLeaveDefinitionDto
    {

        public int JobId { get; set; }

        public int? LeaveDefinitionId { get; set; }

        public DateTime EffectiveFromDate { get; set; }
            = DateTime.Today;

        public DateTime? EffectiveToDate { get; set; }
    }
}
