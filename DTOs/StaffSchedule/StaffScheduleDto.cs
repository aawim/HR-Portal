namespace HRM.DTOs.StaffSchedule
{
    public sealed class StaffScheduleDto
    {
        public int IndividualId { get; set; }

        public int JobId { get; set; }

        public int OrganisationBusinessEntityId { get; set; }

        public string EmployeeName { get; set; } =
            string.Empty;

        public string PositionName { get; set; } =
            string.Empty;

        public string OrganisationName { get; set; } =
            string.Empty;

        public bool HasActiveJob { get; set; }

        public CurrentWorkTemplateAssignmentDto? CurrentTemplate
        {
            get;
            set;
        }

        public CurrentShiftAssignmentDto? CurrentShift
        {
            get;
            set;
        }

        public List<ManualWorkAssignmentListDto> ManualAssignments
        {
            get;
            set;
        } = [];

        public List<JobWorkTemplateAssignmentHistoryDto> TemplateHistory
        {
            get;
            set;
        } = [];

        public List<ShiftAssignmentHistoryDto> ShiftHistory
        {
            get;
            set;
        } = [];
    }
}
