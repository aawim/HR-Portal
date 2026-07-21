namespace HRM.Resources
{
    public class AttendanceResource : ResourceBase
    {
        public int AttendanceLogID { get; set; }


        public int? OrganisationStructureID { get; set; }


        public int? JobID { get; set; }


        public DateTime AttendanceDate { get; set; }


        public int? AttendanceLogModeID { get; set; }


        public int? AttendanceLogStateID { get; set; }


        public bool IsOwnRecord { get; set; }


        public bool IsSupervisor { get; set; }


        public bool IsHRStaff { get; set; }


        public bool IsHeadOfOrganisation { get; set; }


        public bool IsLocked { get; set; }


        public bool IsPayrollProcessed { get; set; }
    }
}
