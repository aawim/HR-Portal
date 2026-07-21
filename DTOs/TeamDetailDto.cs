using System;
using System.Collections.Generic;

namespace HRM.DTOs
{
    //public class TeamDetailDto
    //{
    //    public int TeamId { get; set; }
    //    public int OrganisationId { get; set; }
    //    public string Name { get; set; } = string.Empty;
    //    public string NameDhivehi { get; set; } = string.Empty;

    //    public DateTime StartDate { get; set; }
    //    public DateTime? EndDate { get; set; }
    //    public bool IsValid { get; set; }
    //    public int TotalActiveMembers { get; set; }

    //    public List<TeamStaffDto> TeamStaff { get; set; } = new();
    //}

    public class TeamStaffDto
    {
        public int TeamStaffsId { get; set; }
        public int TeamId { get; set; }
        public int StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool IsSuperVisor { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsValid { get; set; }
        public int OperationLogId { get; set; }

    }
}