-- Migration script: Workspace Teams, Projects, Project Tasks, and Evaluations
-- Standard feature tables following established naming convention: PR_Projects, PR_ProjectMembers, PR_ProjectTasks, EV_Evaluations, EV_EvaluationScores

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

-- 1. PR_Projects
IF OBJECT_ID(N'dbo.PR_Projects', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PR_Projects(
        ProjectId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PR_Projects PRIMARY KEY,
        OrganisationId int NOT NULL,
        TeamId int NULL,
        Name nvarchar(200) NOT NULL,
        Reference nvarchar(50) NOT NULL,
        Description nvarchar(1000) NULL,
        ProjectManagerStaffId int NULL,
        ProjectManagerName nvarchar(200) NULL,
        StartDate datetime2 NOT NULL,
        PlannedEndDate datetime2 NULL,
        ActualEndDate datetime2 NULL,
        Status nvarchar(50) NOT NULL CONSTRAINT DF_PR_Projects_Status DEFAULT(N'Active'),
        ProgressPercentage int NOT NULL CONSTRAINT DF_PR_Projects_Progress DEFAULT(0),
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_PR_Projects_CreatedAt DEFAULT(SYSUTCDATETIME()),
        OrganisationStructureId int NULL,
        CONSTRAINT FK_PR_Projects_Teams_TeamId FOREIGN KEY (TeamId) REFERENCES dbo.Teams (TeamId) ON DELETE SET NULL
    );
    CREATE INDEX IX_PR_Projects_OrganisationId_Status ON dbo.PR_Projects(OrganisationId, Status);
    CREATE INDEX IX_PR_Projects_TeamId ON dbo.PR_Projects(TeamId);
END;

IF COL_LENGTH(N'dbo.PR_Projects', N'OrganisationStructureId') IS NULL
BEGIN
    ALTER TABLE dbo.PR_Projects ADD OrganisationStructureId int NULL;
END;

-- 2. PR_ProjectMembers
IF OBJECT_ID(N'dbo.PR_ProjectMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PR_ProjectMembers(
        ProjectMemberId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PR_ProjectMembers PRIMARY KEY,
        ProjectId int NOT NULL,
        StaffId int NOT NULL,
        StaffName nvarchar(200) NOT NULL,
        Role nvarchar(100) NOT NULL CONSTRAINT DF_PR_ProjectMembers_Role DEFAULT(N'Member'),
        AssignedDate datetime2 NOT NULL CONSTRAINT DF_PR_ProjectMembers_AssignedDate DEFAULT(SYSUTCDATETIME()),
        IsActive bit NOT NULL CONSTRAINT DF_PR_ProjectMembers_IsActive DEFAULT(1),
        CONSTRAINT FK_PR_ProjectMembers_PR_Projects_ProjectId FOREIGN KEY (ProjectId) REFERENCES dbo.PR_Projects(ProjectId) ON DELETE CASCADE
    );
    CREATE INDEX IX_PR_ProjectMembers_ProjectId_StaffId ON dbo.PR_ProjectMembers(ProjectId, StaffId);
END;

-- 3. PR_ProjectTasks
IF OBJECT_ID(N'dbo.PR_ProjectTasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PR_ProjectTasks(
        TaskId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PR_ProjectTasks PRIMARY KEY,
        ProjectId int NOT NULL,
        TaskReference nvarchar(50) NOT NULL,
        Title nvarchar(200) NOT NULL,
        Description nvarchar(1000) NULL,
        AssignedStaffId int NULL,
        AssignedStaffName nvarchar(200) NULL,
        StartDate datetime2 NULL,
        ExpectedEndDate datetime2 NULL,
        ActualCompletionDate datetime2 NULL,
        Status nvarchar(50) NOT NULL CONSTRAINT DF_PR_ProjectTasks_Status DEFAULT(N'Ongoing'),
        Priority nvarchar(20) NOT NULL CONSTRAINT DF_PR_ProjectTasks_Priority DEFAULT(N'Medium'),
        Progress int NOT NULL CONSTRAINT DF_PR_ProjectTasks_Progress DEFAULT(0),
        Remarks nvarchar(1000) NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_PR_ProjectTasks_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_PR_ProjectTasks_PR_Projects_ProjectId FOREIGN KEY (ProjectId) REFERENCES dbo.PR_Projects(ProjectId) ON DELETE CASCADE
    );
    CREATE INDEX IX_PR_ProjectTasks_ProjectId_Status ON dbo.PR_ProjectTasks(ProjectId, Status);
    CREATE INDEX IX_PR_ProjectTasks_AssignedStaffId ON dbo.PR_ProjectTasks(AssignedStaffId);
END;

-- 4. EV_Evaluations
IF OBJECT_ID(N'dbo.EV_Evaluations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EV_Evaluations(
        EvaluationId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EV_Evaluations PRIMARY KEY,
        OrganisationId int NOT NULL,
        StaffId int NOT NULL,
        StaffName nvarchar(200) NOT NULL,
        Designation nvarchar(150) NULL,
        DepartmentName nvarchar(150) NULL,
        TeamName nvarchar(150) NULL,
        EvaluatorStaffId int NOT NULL,
        EvaluatorName nvarchar(200) NOT NULL,
        EvaluationDate datetime2 NOT NULL CONSTRAINT DF_EV_Evaluations_EvaluationDate DEFAULT(SYSUTCDATETIME()),
        Period nvarchar(100) NOT NULL,
        TemplateName nvarchar(100) NOT NULL CONSTRAINT DF_EV_Evaluations_TemplateName DEFAULT(N'Standard Staff Evaluation'),
        OverallRating decimal(3,1) NOT NULL CONSTRAINT DF_EV_Evaluations_OverallRating DEFAULT(3.0),
        Status nvarchar(50) NOT NULL CONSTRAINT DF_EV_Evaluations_Status DEFAULT(N'Submitted'),
        GeneralComments nvarchar(2000) NULL,
        Achievements nvarchar(1000) NULL,
        Strengths nvarchar(1000) NULL,
        AreasForImprovement nvarchar(1000) NULL,
        TrainingRecommendations nvarchar(1000) NULL,
        DevelopmentObjectives nvarchar(1000) NULL,
        Goals nvarchar(1000) NULL,
        ProjectContributions nvarchar(2000) NULL,
        FinalisedBy nvarchar(200) NULL,
        FinalisedAt datetime2 NULL,
        AcknowledgedBy nvarchar(200) NULL,
        AcknowledgedAt datetime2 NULL,
        AcknowledgementComments nvarchar(1000) NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_EV_Evaluations_CreatedAt DEFAULT(SYSUTCDATETIME()),
        UpdatedAt datetime2 NULL
    );
    CREATE INDEX IX_EV_Evaluations_OrganisationId_StaffId ON dbo.EV_Evaluations(OrganisationId, StaffId);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'Designation')
        ALTER TABLE dbo.EV_Evaluations ADD Designation nvarchar(150) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'DepartmentName')
        ALTER TABLE dbo.EV_Evaluations ADD DepartmentName nvarchar(150) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'TeamName')
        ALTER TABLE dbo.EV_Evaluations ADD TeamName nvarchar(150) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'TemplateName')
        ALTER TABLE dbo.EV_Evaluations ADD TemplateName nvarchar(100) NOT NULL CONSTRAINT DF_EV_Evaluations_TemplateName DEFAULT(N'Standard Staff Evaluation');

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'Achievements')
        ALTER TABLE dbo.EV_Evaluations ADD Achievements nvarchar(1000) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'TrainingRecommendations')
        ALTER TABLE dbo.EV_Evaluations ADD TrainingRecommendations nvarchar(1000) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'DevelopmentObjectives')
        ALTER TABLE dbo.EV_Evaluations ADD DevelopmentObjectives nvarchar(1000) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'ProjectContributions')
        ALTER TABLE dbo.EV_Evaluations ADD ProjectContributions nvarchar(2000) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'FinalisedBy')
        ALTER TABLE dbo.EV_Evaluations ADD FinalisedBy nvarchar(200) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'FinalisedAt')
        ALTER TABLE dbo.EV_Evaluations ADD FinalisedAt datetime2 NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'AcknowledgedBy')
        ALTER TABLE dbo.EV_Evaluations ADD AcknowledgedBy nvarchar(200) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'AcknowledgedAt')
        ALTER TABLE dbo.EV_Evaluations ADD AcknowledgedAt datetime2 NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_Evaluations') AND name = N'AcknowledgementComments')
        ALTER TABLE dbo.EV_Evaluations ADD AcknowledgementComments nvarchar(1000) NULL;
END;

-- 5. EV_EvaluationScores
IF OBJECT_ID(N'dbo.EV_EvaluationScores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EV_EvaluationScores(
        ScoreId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EV_EvaluationScores PRIMARY KEY,
        EvaluationId int NOT NULL,
        Category nvarchar(100) NOT NULL CONSTRAINT DF_EV_EvaluationScores_Category DEFAULT(N'Work Performance'),
        CriteriaName nvarchar(150) NOT NULL,
        Score int NOT NULL CONSTRAINT DF_EV_EvaluationScores_Score DEFAULT(3),
        Weight decimal(4,2) NOT NULL CONSTRAINT DF_EV_EvaluationScores_Weight DEFAULT(1.0),
        Comments nvarchar(500) NULL,
        CONSTRAINT FK_EV_EvaluationScores_EV_Evaluations_EvaluationId FOREIGN KEY (EvaluationId) REFERENCES dbo.EV_Evaluations(EvaluationId) ON DELETE CASCADE
    );
    CREATE INDEX IX_EV_EvaluationScores_EvaluationId ON dbo.EV_EvaluationScores(EvaluationId);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_EvaluationScores') AND name = N'Category')
        ALTER TABLE dbo.EV_EvaluationScores ADD Category nvarchar(100) NOT NULL CONSTRAINT DF_EV_EvaluationScores_Category DEFAULT(N'Work Performance');

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.EV_EvaluationScores') AND name = N'Weight')
        ALTER TABLE dbo.EV_EvaluationScores ADD Weight decimal(4,2) NOT NULL CONSTRAINT DF_EV_EvaluationScores_Weight DEFAULT(1.0);
END;

-- 6. Seed Help Desk & Service Desk Team and dummy projects/evaluations if not already present
DECLARE @OrgId int = (SELECT TOP 1 BusinessEntityID FROM dbo.Organisations ORDER BY BusinessEntityID);
IF @OrgId IS NOT NULL
BEGIN
    -- Ensure Help Desk & Service Desk Team exists
    IF NOT EXISTS (SELECT 1 FROM dbo.Teams WHERE Name LIKE N'%Help Desk%' OR Name LIKE N'%Service Desk%')
    BEGIN
        INSERT INTO dbo.Teams (Name, OrganisationId, StartDate, IsValid, NameDhivehi, IsSuperVisor)
        VALUES (N'Help Desk & Service Desk Team', @OrgId, '2026-01-01', 1, N'ހެލްޕް ޑެސްކް ޓީމް', 0);
        
        DECLARE @HelpDeskTeamId int = SCOPE_IDENTITY();
        -- Assign first 2 staff if available
        INSERT INTO dbo.TeamStaffs (TeamId, StaffId, IsSuperVisor, StartDate, IsValid)
        SELECT TOP 2 @HelpDeskTeamId, s.IndividualID, 0, '2026-01-01', 1
        FROM dbo.Staffs s
        WHERE NOT EXISTS (SELECT 1 FROM dbo.TeamStaffs ts WHERE ts.TeamId = @HelpDeskTeamId AND ts.StaffId = s.IndividualID);
    END;

    -- Ensure a sample project exists
    IF NOT EXISTS (SELECT 1 FROM dbo.PR_Projects)
    BEGIN
        DECLARE @TeamId int = (SELECT TOP 1 TeamId FROM dbo.Teams WHERE OrganisationId = @OrgId);
        DECLARE @MgrStaffId int = (SELECT TOP 1 IndividualID FROM dbo.Staffs);

        INSERT INTO dbo.PR_Projects (OrganisationId, TeamId, Name, Reference, Description, ProjectManagerStaffId, ProjectManagerName, StartDate, PlannedEndDate, Status, ProgressPercentage)
        VALUES 
        (@OrgId, @TeamId, N'HRM Workspace Hub Modernization', N'PRJ-2026-001', N'Modern responsive redesign of the supervisor workspace with 3-column dashboard, team 360, and project delivery tracking.', @MgrStaffId, N'Ahmed Naseer', '2026-10-01', '2026-12-15', N'Active', 75),
        (@OrgId, @TeamId, N'Self-Service Attendance & Leave Portal', N'PRJ-2026-002', N'Employee mobile & web self-service interface for biometric punch review, roster swaps, and leave approvals.', @MgrStaffId, N'Mariyam Zoya', '2026-09-15', '2026-11-30', N'Ongoing', 60),
        (@OrgId, @TeamId, N'Biometric Device Infrastructure Upgrade', N'PRJ-2026-003', N'Upgrade older network fingerprint terminals to real-time TCP/IP push protocol.', @MgrStaffId, N'Ali Rasheed', '2026-08-01', '2026-10-25', N'Completed', 100),
        (@OrgId, @TeamId, N'Legacy Pay Matrix Migration Phase 2', N'PRJ-2026-004', N'Consolidation of legacy compensation step hierarchies into current unified pay matrix.', @MgrStaffId, N'Hassan Shuaib', '2026-11-01', '2027-02-28', N'Parked', 20);

        DECLARE @P1 int = (SELECT ProjectId FROM dbo.PR_Projects WHERE Reference = N'PRJ-2026-001');
        DECLARE @P2 int = (SELECT ProjectId FROM dbo.PR_Projects WHERE Reference = N'PRJ-2026-002');
        DECLARE @P3 int = (SELECT ProjectId FROM dbo.PR_Projects WHERE Reference = N'PRJ-2026-003');

        IF @P1 IS NOT NULL
        BEGIN
            INSERT INTO dbo.PR_ProjectTasks (ProjectId, TaskReference, Title, Description, AssignedStaffId, AssignedStaffName, StartDate, ExpectedEndDate, ActualCompletionDate, Status, Priority, Progress, Remarks)
            VALUES
            (@P1, N'TSK-101', N'Design 3-Column Home Layout', N'Implement responsive 23%/54%/23% CSS grid using Tailwind CSS.', @MgrStaffId, N'Ahmed Naseer', '2026-10-05', '2026-10-15', '2026-10-14', N'Completed', N'High', 100, N'Design matches UI standards.'),
            (@P1, N'TSK-102', N'Develop Teams Master-Detail Grid', N'Build responsive master-detail view with persistent 23% team list on desktop.', @MgrStaffId, N'Ahmed Naseer', '2026-10-16', '2026-11-10', NULL, N'Ongoing', N'Critical', 80, N'In progress and responsive.'),
            (@P1, N'TSK-103', N'View 360 Contextual Flyout', N'Create right-hand contextual panel showing employee work history and live tasks.', @MgrStaffId, N'Ahmed Naseer', '2026-10-20', '2026-11-12', NULL, N'Ongoing', N'High', 70, N'Contextual drawer active.'),
            (@P1, N'TSK-104', N'Legacy IE11 Compatibility Fallback', N'Analyze CSS grid fallback for obsolete browsers.', @MgrStaffId, N'Ahmed Naseer', '2026-10-10', '2026-10-30', NULL, N'Parked', N'Low', 15, N'Paused due to modern browser baseline requirement.');
        END;

        IF @P2 IS NOT NULL
        BEGIN
            INSERT INTO dbo.PR_ProjectTasks (ProjectId, TaskReference, Title, Description, AssignedStaffId, AssignedStaffName, StartDate, ExpectedEndDate, ActualCompletionDate, Status, Priority, Progress, Remarks)
            VALUES
            (@P2, N'TSK-201', N'API Endpoint Token Refresh', N'Implement refresh token flow for mobile PWA.', @MgrStaffId, N'Mariyam Zoya', '2026-09-20', '2026-10-05', '2026-10-04', N'Completed', N'Critical', 100, N'Security verified.'),
            (@P2, N'TSK-202', N'Live Attendance Geofence Verification', N'Add GPS radius validation for remote site clocking.', @MgrStaffId, N'Mariyam Zoya', '2026-10-06', '2026-11-20', NULL, N'Ongoing', N'Medium', 50, N'Testing at satellite offices.'),
            (@P2, N'TSK-203', N'Offline Storage Sync Queue', N'Queue clock events locally when device offline.', @MgrStaffId, N'Mariyam Zoya', '2026-10-15', '2026-11-25', NULL, N'Parked', N'Medium', 30, N'Pending security review.');
        END;

        IF @P3 IS NOT NULL
        BEGIN
            INSERT INTO dbo.PR_ProjectTasks (ProjectId, TaskReference, Title, Description, AssignedStaffId, AssignedStaffName, StartDate, ExpectedEndDate, ActualCompletionDate, Status, Priority, Progress, Remarks)
            VALUES
            (@P3, N'TSK-301', N'Firmware Verification', N'Flash device firmware to v4.8 with TLS support.', @MgrStaffId, N'Ali Rasheed', '2026-08-10', '2026-08-25', '2026-08-24', N'Completed', N'High', 100, N'All units updated.');
        END;
    END;

    -- Ensure a sample evaluation exists
    IF NOT EXISTS (SELECT 1 FROM dbo.EV_Evaluations)
    BEGIN
        DECLARE @EvalStaffId int = (SELECT TOP 1 IndividualID FROM dbo.Staffs);
        IF @EvalStaffId IS NOT NULL
        BEGIN
            INSERT INTO dbo.EV_Evaluations (OrganisationId, StaffId, StaffName, EvaluatorStaffId, EvaluatorName, EvaluationDate, Period, OverallRating, Status, GeneralComments, Strengths, AreasForImprovement, Goals)
            VALUES
            (@OrgId, @EvalStaffId, N'Team Member', @EvalStaffId, N'Supervisor Lead', '2026-10-15', N'Q3 2026 Performance Review', 4.4, N'Approved', N'Consistently high level of execution, technical quality, and cooperative communication.', N'Prompt task completion, excellent problem solving skills, great team player.', N'Could take more active initiative in cross-departmental documentation sharing.', N'Complete advanced architecture certification and mentor junior engineers.');
            
            DECLARE @EvalId int = SCOPE_IDENTITY();
            INSERT INTO dbo.EV_EvaluationScores (EvaluationId, CriteriaName, Score, Comments)
            VALUES
            (@EvalId, N'Quality of Work & Accuracy', 5, N'Consistently accurate with zero defects.'),
            (@EvalId, N'Team Collaboration & Communication', 4, N'Proactive in meetings and reviews.'),
            (@EvalId, N'Punctuality & Reliability', 5, N'Always on schedule and meets deadlines.'),
            (@EvalId, N'Technical Competence & Innovation', 4, N'Strong grasp of system components.'),
            (@EvalId, N'Initiative & Problem Solving', 4, N'Handles unexpected hurdles smoothly.');
        END;
    END;
END;
