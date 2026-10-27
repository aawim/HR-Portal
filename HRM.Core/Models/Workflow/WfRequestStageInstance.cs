
namespace HRM.Models.Workflow;
    public sealed class WfRequestStageInstance
    {
        public long RequestStageInstanceId { get; set; }
        public long WorkflowRequestInstanceId { get; set; }
        public int WorkflowStageId { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public string StageStatus { get; set; } = "Pending";
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public WfRequestInstance WorkflowRequestInstance { get; set; } = null!;
        public WfWorkflowStage WorkflowStage { get; set; } = null!;
        public ICollection<WfRequestApproval> Approvals { get; set; } = new List<WfRequestApproval>();
    }
