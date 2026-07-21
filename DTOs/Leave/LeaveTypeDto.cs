namespace HRM.DTOs.Leave
{
    public class LeaveTypeDto
    {
        public int LeaveTypeID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? NameDhivehi { get; set; }

        public int? Duration { get; set; }

        public bool IncludeHolidays { get; set; }

        public bool IncludePay { get; set; }

        public bool IsPublic { get; set; }

        public bool IsGlobal { get; set; }

        public bool IsLocationRequired { get; set; }

        public int ServiceDurationMonths { get; set; }

        public bool IsRenewed { get; set; }

        public bool IsStaffWideAvailable { get; set; }

        public double? PayPercentage { get; set; }

        public int StartInMonth { get; set; }

        public int RepeatedEveryInMonth { get; set; }
    }
}
