namespace HRM.DTOs.Profile
{
    public sealed class ProfilePositionDto
    {
        public int JobPositionId { get; set; }

        public int JobId { get; set; }

        public int PositionId { get; set; }

        public string PositionName { get; set; } =
            string.Empty;

        public string? PositionCode { get; set; }

        public int? JobPositionStateId { get; set; }

        public string JobPositionStateName { get; set; } =
            string.Empty;

        public DateTime FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public bool IsCurrent { get; set; }

        public bool IsActive { get; set; }

        public int? OrganisationStructureId { get; set; }

        public string OrganisationStructureName { get; set; } =
            string.Empty;

        public string EffectivePeriodText { get; set; } =
            string.Empty;
    }
}
