namespace HRM.WorkPlanning.Commands
{
    public class BuildWorkPlanCommand
    {
        public int IndividualId { get; init; }

        public int JobId { get; init; }

        public int OrganisationBusinessEntityId { get; init; }

        public DateOnly WorkDate { get; init; }

        public int? WorkTemplateId { get; init; }

        public string GenerationSource { get; init; } = "TEMPLATE";

        public int? GeneratedByUserId { get; init; }

        public string? Remarks { get; init; }

        public bool FinalizeAfterGeneration { get; init; }
    }
}
