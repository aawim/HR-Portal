SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.WP_ScopeScheduleAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WP_ScopeScheduleAssignments (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_WP_ScopeScheduleAssignments PRIMARY KEY,
        OrganisationId int NOT NULL,
        OrganisationStructureId int NULL,
        GroupId int NULL,
        WorkTemplateId int NOT NULL,
        SchedulePurpose nvarchar(20) NOT NULL,
        ScheduleMode nvarchar(20) NOT NULL,
        WorkingDaysMask int NOT NULL,
        RotationAnchorDate date NULL,
        RotationTemplateIds nvarchar(2000) NULL,
        EffectiveFrom date NOT NULL,
        EffectiveTo date NULL,
        Priority int NOT NULL,
        IsActive bit NOT NULL,
        Version int NOT NULL,
        OperationLogId int NOT NULL,
        CONSTRAINT CK_WP_ScopeScheduleAssignments_Target CHECK (OrganisationStructureId IS NULL OR GroupId IS NULL),
        CONSTRAINT CK_WP_ScopeScheduleAssignments_Period CHECK (EffectiveTo IS NULL OR EffectiveTo>=EffectiveFrom),
        CONSTRAINT CK_WP_ScopeScheduleAssignments_Purpose CHECK (SchedulePurpose IN ('Official','Duty')),
        CONSTRAINT CK_WP_ScopeScheduleAssignments_Mode CHECK (ScheduleMode IN ('Standard','Weekly','Rotation')),
        CONSTRAINT CK_WP_ScopeScheduleAssignments_Days CHECK (WorkingDaysMask BETWEEN 1 AND 127),
        CONSTRAINT CK_WP_ScopeScheduleAssignments_Priority CHECK (Priority BETWEEN 0 AND 9999),
        CONSTRAINT FK_WP_ScopeScheduleAssignments_Organisations_OrganisationId FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_WP_ScopeScheduleAssignments_OrganisationStructure_OrganisationStructureId FOREIGN KEY (OrganisationStructureId) REFERENCES dbo.OrganisationStructure(OrganisationStructureID),
        CONSTRAINT FK_WP_ScopeScheduleAssignments_Groups_GroupId FOREIGN KEY (GroupId) REFERENCES dbo.Groups(GroupID),
        CONSTRAINT FK_WP_ScopeScheduleAssignments_WorkTemplates_WorkTemplateId FOREIGN KEY (WorkTemplateId) REFERENCES dbo.WorkTemplates(WorkTemplateID),
        CONSTRAINT FK_WP_ScopeScheduleAssignments_OperationLogs_OperationLogId FOREIGN KEY (OperationLogId) REFERENCES dbo.OperationLogs(OperationLogID)
    );
    CREATE INDEX IX_WP_ScopeScheduleAssignments_OrganisationId_EffectiveFrom_EffectiveTo ON dbo.WP_ScopeScheduleAssignments(OrganisationId,EffectiveFrom,EffectiveTo);
    CREATE INDEX IX_WP_ScopeScheduleAssignments_OrganisationStructureId ON dbo.WP_ScopeScheduleAssignments(OrganisationStructureId);
    CREATE INDEX IX_WP_ScopeScheduleAssignments_GroupId ON dbo.WP_ScopeScheduleAssignments(GroupId);
    CREATE INDEX IX_WP_ScopeScheduleAssignments_WorkTemplateId ON dbo.WP_ScopeScheduleAssignments(WorkTemplateId);
    CREATE INDEX IX_WP_ScopeScheduleAssignments_OperationLogId ON dbo.WP_ScopeScheduleAssignments(OperationLogId);
END;
