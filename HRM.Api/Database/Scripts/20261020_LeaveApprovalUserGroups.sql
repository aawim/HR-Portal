-- Preserve legacy approver types and allow explicit User Group references for new stages.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_WF_Approvers_Type' AND parent_object_id = OBJECT_ID('dbo.WF_StageApprovers'))
    ALTER TABLE dbo.WF_StageApprovers DROP CONSTRAINT CK_WF_Approvers_Type;
ALTER TABLE dbo.WF_StageApprovers WITH CHECK ADD CONSTRAINT CK_WF_Approvers_Type
    CHECK (ApproverType IN ('Position','HRHead','HR','HOD','Supervisor','Role','User','UserGroup','SupervisorGroup'));
