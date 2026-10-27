
namespace HRM.Models.Workflow;
    public sealed class WfStageApprover
    {
        public int StageApproverId { get; set; }
        public int WorkflowStageId { get; set; }
        public string ApproverType { get; set; } = string.Empty;
        public int? ApproverReferenceId { get; set; }
        public int SequenceNumber { get; set; } = 1;
        public bool IsRequired { get; set; } = true;
        public WfWorkflowStage WorkflowStage { get; set; } = null!;
    }
