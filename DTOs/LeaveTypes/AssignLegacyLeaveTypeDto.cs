namespace HRM.DTOs.LeaveTypes
{
    public class AssignLegacyLeaveTypeDto
    {
        public int JobId { get; set; }

        public int LeaveTypeId { get; set; }



        public int? MigratedLeaveDefinitionId { get; set; }

        
        public DateTime EffectiveFromDate { get; set; }
            = DateTime.Today;

        public DateTime? EffectiveToDate { get; set; }

        public int RemainingDays { get; set; }

        public bool IsValid { get; set; } = true;
    }
}
