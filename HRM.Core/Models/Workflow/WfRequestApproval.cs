
namespace HRM.Models.Workflow;
    public sealed class WfRequestApproval
    {
        public long RequestApprovalId { get; set; }
        public long RequestStageInstanceId { get; set; }
        public int ApproverUserId { get; set; }
        public int? StageApproverId { get; set; }
        public int ApprovalSequence { get; set; } = 1;
        public string Decision { get; set; } = "Pending";
        public DateTime? DecisionAt { get; set; }
        public string? Comments { get; set; }
        public DateTime AssignedAt { get; set; }
        public WfRequestStageInstance RequestStageInstance { get; set; } = null!;
        public WfStageApprover? StageApprover { get; set; }
    }
