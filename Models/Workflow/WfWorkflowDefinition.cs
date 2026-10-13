using HRM.Models.Archives;

namespace HRM.Models.Workflow
{
    public partial class WfWorkflowDefinition
    {
        public int WorkflowDefinitionId { get; set; }

        public int RequestTypeId { get; set; }

        public int? OrganisationBusinessEntityId { get; set; }

        public int VersionNumber { get; set; }

        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateOnly? EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties

        public virtual RequestType RequestType { get; set; } = null!;

        public virtual ICollection<WfWorkflowJobType> WorkflowJobTypes
        { get; set; } = new List<WfWorkflowJobType>();

        public virtual ICollection<WfWorkflowStage> Stages
        { get; set; } = new List<WfWorkflowStage>();

        public virtual ICollection<WfRequestInstance> RequestInstances
        { get; set; } = new List<WfRequestInstance>();
    }
}
