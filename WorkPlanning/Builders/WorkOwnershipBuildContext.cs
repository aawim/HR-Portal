using HRM.Models.WorkPlanning;

namespace HRM.WorkPlanning.Builders
{
    public sealed class WorkOwnershipBuildContext
    {
        public required WorkAssignment Assignment { get; init; }

        public required int IndividualId { get; init; }

        public required int JobId { get; init; }

        public int? AssignedByUserId { get; init; }

        public string OwnershipType { get; init; } = "ORIGINAL";
    }
}
