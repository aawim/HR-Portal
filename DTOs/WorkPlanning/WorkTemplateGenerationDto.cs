namespace HRM.DTOs.WorkPlanning
{
    public class WorkTemplateGenerationDto
    {
        public int WorkTemplateId { get; init; }

        public int WorkTemplateTypeId { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public string? Description { get; init; }

        public List<WorkTemplateSegmentGenerationDto> Segments
        {
            get;
            init;
        } = [];
    }


}
