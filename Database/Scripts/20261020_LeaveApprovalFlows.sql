-- Additive, safe to rerun. Existing definitions and workflow instances are preserved.
IF COL_LENGTH('dbo.WF_WorkflowDefinitions', 'LeaveTypeId') IS NULL
    ALTER TABLE dbo.WF_WorkflowDefinitions ADD LeaveTypeId int NULL;
IF COL_LENGTH('dbo.WF_WorkflowDefinitions', 'LeaveRequestKind') IS NULL
    ALTER TABLE dbo.WF_WorkflowDefinitions ADD LeaveRequestKind int NULL;
