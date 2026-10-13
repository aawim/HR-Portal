using Microsoft.EntityFrameworkCore;

namespace HRM.Models.Workflow
{
   public static class WorkflowModelBuilderExtensions
    {
        public static ModelBuilder ConfigureWorkflow(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WfWorkflowDefinition>(e =>
            {
                e.ToTable("WF_WorkflowDefinitions", "dbo");
                e.HasKey(x => x.WorkflowDefinitionId);
                e.Property(x => x.WorkflowDefinitionId).HasColumnName("WorkflowDefinitionID");
                e.Property(x => x.RequestTypeId).HasColumnName("RequestTypeID");
                e.Property(x => x.OrganisationBusinessEntityId).HasColumnName("OrganisationBusinessEntityID");
                e.Property(x => x.Name).HasMaxLength(250).IsRequired();
                e.Property(x => x.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
                e.Property(x => x.VersionNumber).HasDefaultValue(1);
                e.Property(x => x.IsActive).HasDefaultValue(false);
                e.HasIndex(x => new { x.RequestTypeId, x.OrganisationBusinessEntityId, x.VersionNumber }).IsUnique().HasDatabaseName("UX_WF_Def_OrgVersion");
                e.HasIndex(x => new { x.RequestTypeId, x.OrganisationBusinessEntityId, x.IsActive, x.EffectiveFrom, x.EffectiveTo }).HasDatabaseName("IX_WF_Def_Lookup");
            });
            modelBuilder.Entity<WfWorkflowStage>(e =>
            {
                e.ToTable("WF_WorkflowStages", "dbo");
                e.HasKey(x => x.WorkflowStageId);
                e.Property(x => x.WorkflowStageId).HasColumnName("WorkflowStageID");
                e.Property(x => x.WorkflowDefinitionId).HasColumnName("WorkflowDefinitionID");
                e.Property(x => x.RequestStateId).HasColumnName("RequestStateID");
                e.Property(x => x.StageName).HasMaxLength(200).IsRequired();
                e.Property(x => x.ApprovalMode).HasColumnType("varchar(20)").IsRequired();
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => new { x.WorkflowDefinitionId, x.StageOrder }).IsUnique().HasDatabaseName("UQ_WF_Stages_Order");
                e.HasOne(x => x.WorkflowDefinition).WithMany(x => x.Stages).HasForeignKey(x => x.WorkflowDefinitionId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<WfStageApprover>(e =>
            {
                e.ToTable("WF_StageApprovers", "dbo");
                e.HasKey(x => x.StageApproverId);
                e.Property(x => x.StageApproverId).HasColumnName("StageApproverID");
                e.Property(x => x.WorkflowStageId).HasColumnName("WorkflowStageID");
                e.Property(x => x.ApproverReferenceId).HasColumnName("ApproverReferenceID");
                e.Property(x => x.ApproverType).HasColumnType("varchar(30)").IsRequired();
                e.Property(x => x.SequenceNumber).HasDefaultValue(1);
                e.Property(x => x.IsRequired).HasDefaultValue(true);
                e.HasOne(x => x.WorkflowStage).WithMany(x => x.Approvers).HasForeignKey(x => x.WorkflowStageId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<WfRequestInstance>(e =>
            {
                e.ToTable("WF_RequestInstances", "dbo");
                e.HasKey(x => x.WorkflowRequestInstanceId);
                e.Property(x => x.WorkflowRequestInstanceId).HasColumnName("WorkflowRequestInstanceID");
                e.Property(x => x.RequestId).HasColumnName("RequestID");
                e.Property(x => x.WorkflowDefinitionId).HasColumnName("WorkflowDefinitionID");
                e.Property(x => x.CurrentRequestStageInstanceId).HasColumnName("CurrentRequestStageInstanceID");
                e.Property(x => x.WorkflowStatus).HasColumnType("varchar(25)").HasDefaultValue("InProgress");
                e.Property(x => x.ExecutionStatus).HasColumnType("varchar(25)").HasDefaultValue("NotStarted");
                e.Property(x => x.StartedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
                e.Property(x => x.CompletedAt).HasPrecision(3);
                e.Property(x => x.RowVersion).IsRowVersion();
                e.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("UQ_WF_Instance_Request");
                e.HasOne(x => x.WorkflowDefinition).WithMany().HasForeignKey(x => x.WorkflowDefinitionId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.CurrentStage).WithMany().HasForeignKey(x => x.CurrentRequestStageInstanceId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<WfRequestStageInstance>(e =>
            {
                e.ToTable("WF_RequestStageInstances", "dbo");
                e.HasKey(x => x.RequestStageInstanceId);
                e.Property(x => x.RequestStageInstanceId).HasColumnName("RequestStageInstanceID");
                e.Property(x => x.WorkflowRequestInstanceId).HasColumnName("WorkflowRequestInstanceID");
                e.Property(x => x.WorkflowStageId).HasColumnName("WorkflowStageID");
                e.Property(x => x.StageStatus).HasColumnType("varchar(20)").HasDefaultValue("Pending");
                e.Property(x => x.AttemptNumber).HasDefaultValue(1);
                e.Property(x => x.StartedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
                e.Property(x => x.CompletedAt).HasPrecision(3);
                e.HasIndex(x => new { x.WorkflowRequestInstanceId, x.WorkflowStageId, x.AttemptNumber }).IsUnique().HasDatabaseName("UQ_WF_StageInstance_Attempt");
                e.HasOne(x => x.WorkflowRequestInstance).WithMany(x => x.StageInstances).HasForeignKey(x => x.WorkflowRequestInstanceId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.WorkflowStage).WithMany().HasForeignKey(x => x.WorkflowStageId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<WfRequestApproval>(e =>
            {
                e.ToTable("WF_RequestApprovals", "dbo");
                e.HasKey(x => x.RequestApprovalId);
                e.Property(x => x.RequestApprovalId).HasColumnName("RequestApprovalID");
                e.Property(x => x.RequestStageInstanceId).HasColumnName("RequestStageInstanceID");
                e.Property(x => x.ApproverUserId).HasColumnName("ApproverUserID");
                e.Property(x => x.StageApproverId).HasColumnName("StageApproverID");
                e.Property(x => x.ApprovalSequence).HasDefaultValue(1);
                e.Property(x => x.Decision).HasColumnType("varchar(20)").HasDefaultValue("Pending");
                e.Property(x => x.Comments).HasMaxLength(2000);
                e.Property(x => x.DecisionAt).HasPrecision(3);
                e.Property(x => x.AssignedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasIndex(x => new { x.RequestStageInstanceId, x.ApproverUserId }).IsUnique().HasDatabaseName("UQ_WF_Approval_Assignee");
                e.HasIndex(x => new { x.ApproverUserId, x.Decision, x.AssignedAt }).HasDatabaseName("IX_WF_Approval_Inbox");
                e.HasOne(x => x.RequestStageInstance).WithMany(x => x.Approvals).HasForeignKey(x => x.RequestStageInstanceId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.StageApprover).WithMany().HasForeignKey(x => x.StageApproverId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<WfRequestAction>(e =>
            {
                e.ToTable("WF_RequestActions", "dbo");
                e.HasKey(x => x.RequestActionId);
                e.Property(x => x.RequestActionId).HasColumnName("RequestActionID");
                e.Property(x => x.WorkflowRequestInstanceId).HasColumnName("WorkflowRequestInstanceID");
                e.Property(x => x.RequestStageInstanceId).HasColumnName("RequestStageInstanceID");
                e.Property(x => x.PerformedByUserId).HasColumnName("PerformedByUserID");
                e.Property(x => x.FromRequestStateId).HasColumnName("FromRequestStateID");
                e.Property(x => x.ToRequestStateId).HasColumnName("ToRequestStateID");
                e.Property(x => x.ActionCode).HasColumnType("varchar(30)").IsRequired();
                e.Property(x => x.Comments).HasMaxLength(2000);
                e.Property(x => x.PerformedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
                e.HasIndex(x => new { x.WorkflowRequestInstanceId, x.PerformedAt }).HasDatabaseName("IX_WF_Action_History");
                e.HasOne(x => x.WorkflowRequestInstance).WithMany(x => x.Actions).HasForeignKey(x => x.WorkflowRequestInstanceId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(x => x.RequestStageInstance).WithMany().HasForeignKey(x => x.RequestStageInstanceId).OnDelete(DeleteBehavior.NoAction);
            });
            return modelBuilder;
        }
    }
}
