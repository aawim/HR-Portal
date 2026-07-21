using HRM.Models;

namespace HRM.DTOs.Leave
{
    public class JobLeaveTypeDto
    {
        public int JobLeaveTypeId { get; set; }

        public int JobId { get; set; }

        public int LeaveTypeID { get; set; }

        public bool IsLeaveInfoUpdated { get; set; }

        public string LeaveTypeName { get; set; } = "";

        public int? RemainingDays { get; set; }
  
        public bool IsValid { get; set; }

        public int LeaveTypeId { get; set; }

        public DateTime? LastLeaveTakenDate { get; set; }

        public DateTime? RenewedDate { get; set; }

        public DateTime? EffectiveFromDate { get; set; }

        public DateTime? EffectiveToDate { get; set; }

 

    }
}
