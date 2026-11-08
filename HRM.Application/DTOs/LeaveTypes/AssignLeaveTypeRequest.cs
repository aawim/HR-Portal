namespace HRM.DTOs.LeaveTypes
{
    public class AssignLeaveTypeCatalogueDto
    {

        public int JobId { get; set; }

        public int? LeaveTypeId { get; set; }

        public DateTime EffectiveFromDate { get; set; }
            = DateTime.Today;

        public DateTime? EffectiveToDate { get; set; }
    }
}
