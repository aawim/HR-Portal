namespace HRM.Models.Workflow
{
    public partial class WfWorkflowJobType
    {
        public int WorkflowJobTypeId { get; set; }

        public int WorkflowDefinitionId { get; set; }

        public int JobTypeId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties

        public virtual WfWorkflowDefinition WorkflowDefinition
        { get; set; } = null!;

        public virtual JobType JobType
        { get; set; } = null!;
    }
}
