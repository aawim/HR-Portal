namespace HRM.Models.Workflow
{
    public sealed class WfWorkflowStage
    {
        public int WorkflowStageId { get; set; }
        public int WorkflowDefinitionId { get; set; }
        public int StageOrder { get; set; }
        public string StageName { get; set; } = string.Empty;
        public int RequestStateId { get; set; }
        public string ApprovalMode { get; set; } = "All";
        public int? MinimumApprovals { get; set; }
        public bool IsActive { get; set; } = true;
        public WfWorkflowDefinition WorkflowDefinition { get; set; } = null!;
        public ICollection<WfStageApprover> Approvers { get; set; } = new List<WfStageApprover>();
    }
}
