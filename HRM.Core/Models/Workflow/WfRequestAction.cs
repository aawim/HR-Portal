
namespace HRM.Models.Workflow;
    public sealed class WfRequestAction
    {
        public long RequestActionId { get; set; }
        public long WorkflowRequestInstanceId { get; set; }
        public long? RequestStageInstanceId { get; set; }
        public int? PerformedByUserId { get; set; }
        public string ActionCode { get; set; } = string.Empty;
        public int? FromRequestStateId { get; set; }
        public int? ToRequestStateId { get; set; }
        public string? Comments { get; set; }
        public DateTime PerformedAt { get; set; }
        public WfRequestInstance WorkflowRequestInstance { get; set; } = null!;
        public WfRequestStageInstance? RequestStageInstance { get; set; }
    }
