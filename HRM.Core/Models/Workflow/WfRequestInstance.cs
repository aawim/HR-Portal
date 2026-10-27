
namespace HRM.Models.Workflow;
    public sealed class WfRequestInstance
    {
        public long WorkflowRequestInstanceId { get; set; }
        public int RequestId { get; set; }
        public int WorkflowDefinitionId { get; set; }
        public long? CurrentRequestStageInstanceId { get; set; }
        public string WorkflowStatus { get; set; } = "InProgress";
        public string ExecutionStatus { get; set; } = "NotStarted";
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public WfWorkflowDefinition WorkflowDefinition { get; set; } = null!;
        public WfRequestStageInstance? CurrentStage { get; set; }
        public ICollection<WfRequestStageInstance> StageInstances { get; set; } = new List<WfRequestStageInstance>();
        public ICollection<WfRequestAction> Actions { get; set; } = new List<WfRequestAction>();
    }
