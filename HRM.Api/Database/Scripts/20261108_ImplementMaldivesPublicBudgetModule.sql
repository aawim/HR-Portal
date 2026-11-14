-- 20261108_ImplementMaldivesPublicBudgetModule.sql
-- Feature Prefix: BG_ (Maldives Public Finance Budget Management & Payroll Coupling)
-- Compliant with Maldives Public Finance Act (Law No. 3/2006) & Public Finance Regulations

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Upgrade BG_BudgetItems with Maldives MoF COA fields if not present
IF OBJECT_ID(N'dbo.BG_BudgetItems', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BG_BudgetItems') AND name = N'EconomicCode')
    BEGIN
        ALTER TABLE dbo.BG_BudgetItems ADD EconomicCode NVARCHAR(20) NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BG_BudgetItems') AND name = N'MoFClassificationCode')
    BEGIN
        ALTER TABLE dbo.BG_BudgetItems ADD MoFClassificationCode NVARCHAR(50) NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BG_BudgetItems') AND name = N'ChapterCode')
    BEGIN
        ALTER TABLE dbo.BG_BudgetItems ADD ChapterCode NVARCHAR(10) NOT NULL CONSTRAINT DF_BG_BudgetItems_ChapterCode DEFAULT ('21');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BG_BudgetItems') AND name = N'AccountType')
    BEGIN
        ALTER TABLE dbo.BG_BudgetItems ADD AccountType NVARCHAR(20) NOT NULL CONSTRAINT DF_BG_BudgetItems_AccountType DEFAULT ('Expenditure');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BG_BudgetItems') AND name = N'IsSalaryComponentLinked')
    BEGIN
        ALTER TABLE dbo.BG_BudgetItems ADD IsSalaryComponentLinked BIT NOT NULL CONSTRAINT DF_BG_BudgetItems_IsSalaryComponentLinked DEFAULT (1);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.BG_BudgetItems') AND name = N'IsActive')
    BEGIN
        ALTER TABLE dbo.BG_BudgetItems ADD IsActive BIT NOT NULL CONSTRAINT DF_BG_BudgetItems_IsActive DEFAULT (1);
    END

    -- Populate EconomicCode from legacy Code if null
    EXEC(N'UPDATE dbo.BG_BudgetItems SET EconomicCode = CAST(Code AS NVARCHAR(20)) WHERE EconomicCode IS NULL AND Code IS NOT NULL;');
END

-- 2. BG_FiscalYears Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_FiscalYears')
BEGIN
    CREATE TABLE dbo.BG_FiscalYears (
        FiscalYearId INT IDENTITY(1,1) NOT NULL,
        YearNumber INT NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        TotalApprovedBudget DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_FiscalYears_TotalApproved DEFAULT (0),
        TotalEmolumentsBudget DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_FiscalYears_TotalEmoluments DEFAULT (0),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_BG_FiscalYears_Status DEFAULT ('Active'),
        IsActive BIT NOT NULL CONSTRAINT DF_BG_FiscalYears_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_BG_FiscalYears_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BG_FiscalYears PRIMARY KEY CLUSTERED (FiscalYearId),
        CONSTRAINT UQ_BG_FiscalYears_YearNumber UNIQUE (YearNumber)
    );
END

-- 3. BG_BudgetCategories Table (MoF COA Chapter Hierarchy)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_BudgetCategories')
BEGIN
    CREATE TABLE dbo.BG_BudgetCategories (
        CategoryId INT IDENTITY(1,1) NOT NULL,
        ParentCategoryId INT NULL,
        CategoryCode NVARCHAR(20) NOT NULL,
        CategoryNameEn NVARCHAR(200) NOT NULL,
        CategoryNameDv NVARCHAR(200) NOT NULL,
        ChapterNumber INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_BG_BudgetCategories_IsActive DEFAULT (1),
        CONSTRAINT PK_BG_BudgetCategories PRIMARY KEY CLUSTERED (CategoryId),
        CONSTRAINT FK_BG_BudgetCategories_Parent FOREIGN KEY (ParentCategoryId) REFERENCES dbo.BG_BudgetCategories(CategoryId)
    );
END

-- 4. BG_BudgetTransactionTypes Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_BudgetTransactionTypes')
BEGIN
    CREATE TABLE dbo.BG_BudgetTransactionTypes (
        Id INT NOT NULL,
        TypeName NVARCHAR(50) NOT NULL,
        DhivehiTypeName NVARCHAR(100) NULL,
        CONSTRAINT PK_BG_BudgetTransactionTypes PRIMARY KEY CLUSTERED (Id)
    );
END

-- 5. BG_BudgetAllocations Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_BudgetAllocations')
BEGIN
    CREATE TABLE dbo.BG_BudgetAllocations (
        AllocationId INT IDENTITY(1,1) NOT NULL,
        FiscalYearId INT NOT NULL,
        OrganisationId INT NOT NULL,
        BudgetItemId INT NOT NULL,
        ApprovedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_BudgetAllocations_Approved DEFAULT (0),
        RevisedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_BudgetAllocations_Revised DEFAULT (0),
        VirementInAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_BudgetAllocations_VirementIn DEFAULT (0),
        VirementOutAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_BudgetAllocations_VirementOut DEFAULT (0),
        CommittedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_BudgetAllocations_Committed DEFAULT (0),
        ExpendedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BG_BudgetAllocations_Expended DEFAULT (0),
        MoFApprovalReference NVARCHAR(100) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_BG_BudgetAllocations_Status DEFAULT ('Approved'),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_BG_BudgetAllocations_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_BG_BudgetAllocations_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BG_BudgetAllocations PRIMARY KEY CLUSTERED (AllocationId),
        CONSTRAINT FK_BG_BudgetAllocations_FiscalYear FOREIGN KEY (FiscalYearId) REFERENCES dbo.BG_FiscalYears(FiscalYearId),
        CONSTRAINT FK_BG_BudgetAllocations_Organisation FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_BG_BudgetAllocations_BudgetItem FOREIGN KEY (BudgetItemId) REFERENCES dbo.BG_BudgetItems(BudgetItemID),
        CONSTRAINT UQ_BG_BudgetAllocations_Year_Org_Item UNIQUE (FiscalYearId, OrganisationId, BudgetItemId)
    );

    CREATE NONCLUSTERED INDEX IX_BG_BudgetAllocations_Org_Year ON dbo.BG_BudgetAllocations (OrganisationId, FiscalYearId);
END

-- 6. BG_BudgetVirements Table (Section 5 Public Finance Act transfers)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_BudgetVirements')
BEGIN
    CREATE TABLE dbo.BG_BudgetVirements (
        VirementId INT IDENTITY(1,1) NOT NULL,
        FiscalYearId INT NOT NULL,
        VirementNumber NVARCHAR(50) NOT NULL,
        FromBudgetItemId INT NOT NULL,
        ToBudgetItemId INT NOT NULL,
        OrganisationId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Justification NVARCHAR(MAX) NOT NULL,
        LegalBasis NVARCHAR(500) NOT NULL,
        MoFReferenceNumber NVARCHAR(100) NULL,
        MoFApprovedDate DATE NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_BG_BudgetVirements_Status DEFAULT ('Approved'),
        CreatedByUserId INT NULL,
        ApprovedByUserId INT NULL,
        ApprovedAt DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_BG_BudgetVirements_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BG_BudgetVirements PRIMARY KEY CLUSTERED (VirementId),
        CONSTRAINT FK_BG_BudgetVirements_FiscalYear FOREIGN KEY (FiscalYearId) REFERENCES dbo.BG_FiscalYears(FiscalYearId),
        CONSTRAINT FK_BG_BudgetVirements_FromItem FOREIGN KEY (FromBudgetItemId) REFERENCES dbo.BG_BudgetItems(BudgetItemID),
        CONSTRAINT FK_BG_BudgetVirements_ToItem FOREIGN KEY (ToBudgetItemId) REFERENCES dbo.BG_BudgetItems(BudgetItemID),
        CONSTRAINT FK_BG_BudgetVirements_Org FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT UQ_BG_BudgetVirements_Number UNIQUE (VirementNumber)
    );
END

-- 7. BG_BudgetCommitments Table (Pre-Payroll and Procurement Encumbrance)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_BudgetCommitments')
BEGIN
    CREATE TABLE dbo.BG_BudgetCommitments (
        CommitmentId INT IDENTITY(1,1) NOT NULL,
        FiscalYearId INT NOT NULL,
        OrganisationId INT NOT NULL,
        BudgetItemId INT NOT NULL,
        CommitmentNumber NVARCHAR(50) NOT NULL,
        CommitmentType NVARCHAR(50) NOT NULL, -- 'PayrollBatch', 'PositionRequisition', 'Procurement'
        SourceReferenceId INT NULL,          -- e.g. PayrollRunId
        Amount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_BG_BudgetCommitments_Status DEFAULT ('Reserved'),
        ExpiresAt DATE NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_BG_BudgetCommitments_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BG_BudgetCommitments PRIMARY KEY CLUSTERED (CommitmentId),
        CONSTRAINT FK_BG_BudgetCommitments_FiscalYear FOREIGN KEY (FiscalYearId) REFERENCES dbo.BG_FiscalYears(FiscalYearId),
        CONSTRAINT FK_BG_BudgetCommitments_Org FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_BG_BudgetCommitments_BudgetItem FOREIGN KEY (BudgetItemId) REFERENCES dbo.BG_BudgetItems(BudgetItemID),
        CONSTRAINT UQ_BG_BudgetCommitments_Number UNIQUE (CommitmentNumber)
    );

    CREATE NONCLUSTERED INDEX IX_BG_BudgetCommitments_Org_Item_Status ON dbo.BG_BudgetCommitments (OrganisationId, BudgetItemId, Status);
END

-- 8. BG_BudgetTransactions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_BudgetTransactions')
BEGIN
    CREATE TABLE dbo.BG_BudgetTransactions (
        BudgetTransactionID INT IDENTITY(1,1) NOT NULL,
        BudgetItemID INT NOT NULL,
        FiscalYearID INT NULL,
        OrganisationID INT NOT NULL,
        TransactionTypeID INT NOT NULL,
        Date DATE NOT NULL,
        ReferenceNumber NVARCHAR(100) NULL,
        LetterNumber NVARCHAR(100) NULL,
        BandeyriReferenceNumber NVARCHAR(100) NULL,
        Details NVARCHAR(MAX) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        SourceDocumentType NVARCHAR(50) NULL,
        SourceDocumentID INT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_BG_BudgetTransactions_Status DEFAULT ('Posted'),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_BG_BudgetTransactions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BG_BudgetTransactions PRIMARY KEY CLUSTERED (BudgetTransactionID),
        CONSTRAINT FK_BG_BudgetTransactions_BudgetItem FOREIGN KEY (BudgetItemID) REFERENCES dbo.BG_BudgetItems(BudgetItemID),
        CONSTRAINT FK_BG_BudgetTransactions_Organisation FOREIGN KEY (OrganisationID) REFERENCES dbo.Organisations(BusinessEntityID),
        CONSTRAINT FK_BG_BudgetTransactions_Type FOREIGN KEY (TransactionTypeID) REFERENCES dbo.BG_BudgetTransactionTypes(Id)
    );

    CREATE NONCLUSTERED INDEX IX_BG_BudgetTransactions_Item_Org_Date ON dbo.BG_BudgetTransactions (BudgetItemID, OrganisationID, Date);
END

-- 9. BG_PayrollBudgetMappings Table (Direct matrix linking Payroll & Allowances to MoF COA)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_PayrollBudgetMappings')
BEGIN
    CREATE TABLE dbo.BG_PayrollBudgetMappings (
        MappingId INT IDENTITY(1,1) NOT NULL,
        SalaryComponentId INT NULL,
        AllowanceTypeId INT NULL,
        BudgetItemId INT NOT NULL,
        CostCenterOrganisationId INT NULL,
        ComponentName NVARCHAR(150) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_BG_PayrollBudgetMappings_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_BG_PayrollBudgetMappings_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BG_PayrollBudgetMappings PRIMARY KEY CLUSTERED (MappingId),
        CONSTRAINT FK_BG_PayrollBudgetMappings_BudgetItem FOREIGN KEY (BudgetItemId) REFERENCES dbo.BG_BudgetItems(BudgetItemID),
        CONSTRAINT FK_BG_PayrollBudgetMappings_Org FOREIGN KEY (CostCenterOrganisationId) REFERENCES dbo.Organisations(BusinessEntityID)
    );
END

-- 10. BG_BudgetAuditEvents Table (Immutable financial audit trail)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BG_BudgetAuditEvents')
BEGIN
    CREATE TABLE dbo.BG_BudgetAuditEvents (
        AuditEventId BIGINT IDENTITY(1,1) NOT NULL,
        FiscalYearId INT NULL,
        BudgetItemId INT NULL,
        ActionType NVARCHAR(50) NOT NULL,
        EntityName NVARCHAR(100) NOT NULL,
        EntityId INT NOT NULL,
        PreviousValuesJson NVARCHAR(MAX) NULL,
        NewValuesJson NVARCHAR(MAX) NULL,
        PerformedByUserId INT NULL,
        PerformedAt DATETIME2 NOT NULL CONSTRAINT DF_BG_BudgetAuditEvents_PerformedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BG_BudgetAuditEvents PRIMARY KEY CLUSTERED (AuditEventId)
    );
END

-- ==========================================
-- SEED DATA: MALDIVES PUBLIC FINANCE STANDARD
-- ==========================================

-- Seed BG_BudgetTransactionTypes
IF NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetTransactionTypes WHERE Id = 1)
BEGIN
    INSERT INTO dbo.BG_BudgetTransactionTypes (Id, TypeName, DhivehiTypeName) VALUES
    (1, N'InitialAllocation', N'އަހަރީ ބަޖެޓް ކަނޑައެޅުން'),
    (2, N'VirementIn', N'ބަޖެޓް ބަދަލުކުރުން - އިތުރު'),
    (3, N'VirementOut', N'ބަޖެޓް ބަދަލުކުރުން - އުނި'),
    (4, N'SupplementaryAllocation', N'އިތުރު ބަޖެޓް (ސަޕްލިމެންޓަރީ)'),
    (5, N'CommitmentReservation', N'ފައިސާ ރިޒާވްކުރުން (ކޮމިޓްމަންޓް)'),
    (6, N'CommitmentRelease', N'ކޮމިޓްމަންޓް ދޫކޮށްލުން'),
    (7, N'ActualExpenditure', N'ޚަރަދު ހިނގުން (މުސާރަ ޚަރަދު)'),
    (8, N'Reversal', N'އަނބުރާ ގެނައުން');
END

-- Seed BG_FiscalYears (2026 Active Fiscal Year)
IF NOT EXISTS (SELECT 1 FROM dbo.BG_FiscalYears WHERE YearNumber = 2026)
BEGIN
    INSERT INTO dbo.BG_FiscalYears (YearNumber, StartDate, EndDate, TotalApprovedBudget, TotalEmolumentsBudget, Status, IsActive)
    VALUES (2026, '2026-01-01', '2026-12-31', 450000000.00, 185000000.00, 'Active', 1);
END

-- Seed BG_BudgetCategories (MoF Standard Chapters)
IF NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetCategories WHERE CategoryCode = '21')
BEGIN
    INSERT INTO dbo.BG_BudgetCategories (CategoryCode, CategoryNameEn, CategoryNameDv, ChapterNumber, IsActive) VALUES
    ('21', N'Compensation of Employees (Personal Emoluments)', N'މުވައްޒަފުންނަށް ހިނގާ ޚަރަދު', 21, 1),
    ('22', N'Use of Goods and Services', N'އޮފީސް ހިންގުމާއި ޚިދުމަތުގެ ޚަރަދު', 22, 1),
    ('28', N'Capital Expenditure & Assets', N'ކެޕިޓަލް ޚަރަދު', 28, 1);
END

-- Seed / Link Core 2026 Personal Emolument Allocations for NCIT (BusinessEntityID = 3)
DECLARE @Fiscal2026Id INT = (SELECT FiscalYearId FROM dbo.BG_FiscalYears WHERE YearNumber = 2026);
DECLARE @OrgNcitId INT = 3; -- National Centre for Information Technology

IF @Fiscal2026Id IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Organisations WHERE BusinessEntityID = @OrgNcitId)
BEGIN
    -- Basic Salary (211001) - BudgetItemID 24
    IF EXISTS (SELECT 1 FROM dbo.BG_BudgetItems WHERE BudgetItemID = 24)
       AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetAllocations WHERE FiscalYearId = @Fiscal2026Id AND OrganisationId = @OrgNcitId AND BudgetItemId = 24)
    BEGIN
        INSERT INTO dbo.BG_BudgetAllocations (FiscalYearId, OrganisationId, BudgetItemId, ApprovedAmount, RevisedAmount, VirementInAmount, VirementOutAmount, CommittedAmount, ExpendedAmount, MoFApprovalReference, Status)
        VALUES (@Fiscal2026Id, @OrgNcitId, 24, 25000000.00, 25000000.00, 0, 0, 1850000.00, 18200000.00, N'MOF/2026/BUD-0042', 'Approved');
    END

    -- Overtime Pay (211002) - BudgetItemID 25
    IF EXISTS (SELECT 1 FROM dbo.BG_BudgetItems WHERE BudgetItemID = 25)
       AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetAllocations WHERE FiscalYearId = @Fiscal2026Id AND OrganisationId = @OrgNcitId AND BudgetItemId = 25)
    BEGIN
        INSERT INTO dbo.BG_BudgetAllocations (FiscalYearId, OrganisationId, BudgetItemId, ApprovedAmount, RevisedAmount, VirementInAmount, VirementOutAmount, CommittedAmount, ExpendedAmount, MoFApprovalReference, Status)
        VALUES (@Fiscal2026Id, @OrgNcitId, 25, 4500000.00, 5000000.00, 500000.00, 0, 320000.00, 3650000.00, N'MOF/2026/BUD-0042', 'Approved');
    END

    -- Technical / Professional Allowance (212003) - BudgetItemID 28
    IF EXISTS (SELECT 1 FROM dbo.BG_BudgetItems WHERE BudgetItemID = 28)
       AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetAllocations WHERE FiscalYearId = @Fiscal2026Id AND OrganisationId = @OrgNcitId AND BudgetItemId = 28)
    BEGIN
        INSERT INTO dbo.BG_BudgetAllocations (FiscalYearId, OrganisationId, BudgetItemId, ApprovedAmount, RevisedAmount, VirementInAmount, VirementOutAmount, CommittedAmount, ExpendedAmount, MoFApprovalReference, Status)
        VALUES (@Fiscal2026Id, @OrgNcitId, 28, 8500000.00, 8500000.00, 0, 0, 680000.00, 6100000.00, N'MOF/2026/BUD-0042', 'Approved');
    END

    -- Service Allowance (212027) - BudgetItemID 52
    IF EXISTS (SELECT 1 FROM dbo.BG_BudgetItems WHERE BudgetItemID = 52)
       AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetAllocations WHERE FiscalYearId = @Fiscal2026Id AND OrganisationId = @OrgNcitId AND BudgetItemId = 52)
    BEGIN
        INSERT INTO dbo.BG_BudgetAllocations (FiscalYearId, OrganisationId, BudgetItemId, ApprovedAmount, RevisedAmount, VirementInAmount, VirementOutAmount, CommittedAmount, ExpendedAmount, MoFApprovalReference, Status)
        VALUES (@Fiscal2026Id, @OrgNcitId, 52, 6000000.00, 5500000.00, 0, 500000.00, 420000.00, 4250000.00, N'MOF/2026/BUD-0042', 'Approved');
    END

    -- Responsibility Allowance (212028) - BudgetItemID 53
    IF EXISTS (SELECT 1 FROM dbo.BG_BudgetItems WHERE BudgetItemID = 53)
       AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetAllocations WHERE FiscalYearId = @Fiscal2026Id AND OrganisationId = @OrgNcitId AND BudgetItemId = 53)
    BEGIN
        INSERT INTO dbo.BG_BudgetAllocations (FiscalYearId, OrganisationId, BudgetItemId, ApprovedAmount, RevisedAmount, VirementInAmount, VirementOutAmount, CommittedAmount, ExpendedAmount, MoFApprovalReference, Status)
        VALUES (@Fiscal2026Id, @OrgNcitId, 53, 3200000.00, 3200000.00, 0, 0, 240000.00, 2400000.00, N'MOF/2026/BUD-0042', 'Approved');
    END

    -- MRPS Pension 7% (213006) - BudgetItemID 59
    IF EXISTS (SELECT 1 FROM dbo.BG_BudgetItems WHERE BudgetItemID = 59)
       AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetAllocations WHERE FiscalYearId = @Fiscal2026Id AND OrganisationId = @OrgNcitId AND BudgetItemId = 59)
    BEGIN
        INSERT INTO dbo.BG_BudgetAllocations (FiscalYearId, OrganisationId, BudgetItemId, ApprovedAmount, RevisedAmount, VirementInAmount, VirementOutAmount, CommittedAmount, ExpendedAmount, MoFApprovalReference, Status)
        VALUES (@Fiscal2026Id, @OrgNcitId, 59, 1750000.00, 1750000.00, 0, 0, 129500.00, 1274000.00, N'MOF/2026/BUD-0042', 'Approved');
    END
END

-- Seed sample Virement: Service Allowance (52) -> Overtime (25) of MVR 500,000 under Section 5
IF @Fiscal2026Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetVirements WHERE VirementNumber = 'VIR-2026-0001')
BEGIN
    INSERT INTO dbo.BG_BudgetVirements (FiscalYearId, VirementNumber, FromBudgetItemId, ToBudgetItemId, OrganisationId, Amount, Justification, LegalBasis, MoFReferenceNumber, MoFApprovedDate, Status, ApprovedAt)
    VALUES (@Fiscal2026Id, 'VIR-2026-0001', 52, 25, @OrgNcitId, 500000.00, 
            N'Internal virement within Chapter 21 to meet critical 24/7 National Data Center shift duty and infrastructure monitoring requirements.',
            N'Public Finance Act (Law 3/2006) Section 5 & Ministry of Finance Circular 13-P/CIR/2026/04',
            N'MOF/VIR/2026/089', '2026-08-15', 'Approved', SYSUTCDATETIME());
END

-- Seed sample Payroll Commitment
IF @Fiscal2026Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetCommitments WHERE CommitmentNumber = 'COM-2026-11-PAY')
BEGIN
    INSERT INTO dbo.BG_BudgetCommitments (FiscalYearId, OrganisationId, BudgetItemId, CommitmentNumber, CommitmentType, SourceReferenceId, Amount, Status, ExpiresAt)
    VALUES (@Fiscal2026Id, @OrgNcitId, 24, 'COM-2026-11-PAY', 'PayrollBatch', 101, 1850000.00, 'Reserved', '2026-11-30');
END

-- Seed Payroll to Budget Mappings
IF NOT EXISTS (SELECT 1 FROM dbo.BG_PayrollBudgetMappings WHERE ComponentName = 'Basic Salary')
BEGIN
    INSERT INTO dbo.BG_PayrollBudgetMappings (SalaryComponentId, AllowanceTypeId, BudgetItemId, CostCenterOrganisationId, ComponentName, IsActive) VALUES
    (NULL, NULL, 24, @OrgNcitId, N'Basic Salary', 1),
    (NULL, NULL, 25, @OrgNcitId, N'Overtime Pay', 1),
    (NULL, NULL, 28, @OrgNcitId, N'Technical / Professional Allowance', 1),
    (NULL, 1, 49, @OrgNcitId, N'Phone Allowance', 1),
    (NULL, 2, 39, @OrgNcitId, N'Accommodation / Living Allowance', 1),
    (NULL, NULL, 52, @OrgNcitId, N'Service Allowance', 1),
    (NULL, NULL, 53, @OrgNcitId, N'Responsibility Allowance', 1),
    (NULL, NULL, 59, @OrgNcitId, N'MRPS Employer Pension 7%', 1);
END

-- Seed sample Transactions (Initial allocation and virement)
IF NOT EXISTS (SELECT 1 FROM dbo.BG_BudgetTransactions WHERE ReferenceNumber = 'TXN-2026-001')
BEGIN
    INSERT INTO dbo.BG_BudgetTransactions (BudgetItemID, FiscalYearID, OrganisationID, TransactionTypeID, Date, ReferenceNumber, LetterNumber, BandeyriReferenceNumber, Details, Amount, SourceDocumentType, Status) VALUES
    (24, @Fiscal2026Id, @OrgNcitId, 1, '2026-01-01', 'TXN-2026-001', 'MOF/2026/BUD-0042', 'BAND-2026-00918', N'Approved State Budget Appropriation for 2026 - Basic Salary', 25000000.00, 'InitialAllocation', 'Posted'),
    (25, @Fiscal2026Id, @OrgNcitId, 1, '2026-01-01', 'TXN-2026-002', 'MOF/2026/BUD-0042', 'BAND-2026-00918', N'Approved State Budget Appropriation for 2026 - Overtime', 4500000.00, 'InitialAllocation', 'Posted'),
    (25, @Fiscal2026Id, @OrgNcitId, 2, '2026-08-15', 'TXN-2026-003', 'VIR-2026-0001', 'BAND-VIR-2026-044', N'Virement from Service Allowance to Overtime Pay', 500000.00, 'Virement', 'Posted'),
    (52, @Fiscal2026Id, @OrgNcitId, 3, '2026-08-15', 'TXN-2026-004', 'VIR-2026-0001', 'BAND-VIR-2026-044', N'Virement deduction to Overtime Pay', 500000.00, 'Virement', 'Posted');
END

COMMIT TRANSACTION;
GO
