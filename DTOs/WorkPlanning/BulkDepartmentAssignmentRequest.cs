using System.ComponentModel.DataAnnotations;

namespace HRM.DTOs.WorkPlanning
{
    public sealed class BulkDepartmentAssignmentRequest
    {
        public int OrganisationStructureId { get; set; }

        public int? WorkPlanId { get; set; }

        public string AssignmentName { get; set; } =
            string.Empty;

        public DateTime ScheduledStart { get; set; }

        public DateTime ScheduledEnd { get; set; }

        public List<int> SelectedIndividualIds { get; set; } =
            new();

        public int AssignedByUserId { get; set; }

        public string? Remarks { get; set; }


    }
}
