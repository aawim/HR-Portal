using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261108210000_StaffGoodsLoanManagement")]
    public sealed class StaffGoodsLoanManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. GL_Stores
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_Stores')
                BEGIN
                    CREATE TABLE dbo.GL_Stores (
                        StoreID INT IDENTITY(1,1) NOT NULL,
                        StoreCode NVARCHAR(50) NOT NULL,
                        StoreName NVARCHAR(200) NOT NULL,
                        RegistrationNumber NVARCHAR(100) NOT NULL,
                        ContactPerson NVARCHAR(150) NOT NULL,
                        PhoneNumber NVARCHAR(50) NOT NULL,
                        Email NVARCHAR(150) NOT NULL,
                        Address NVARCHAR(500) NOT NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_GL_Stores_IsActive DEFAULT (1),
                        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_Stores_CreatedDate DEFAULT (SYSUTCDATETIME()),
                        CreatedByUserID INT NOT NULL,
                        CONSTRAINT PK_GL_Stores PRIMARY KEY CLUSTERED (StoreID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_GL_Stores_StoreCode ON dbo.GL_Stores (StoreCode);
                    CREATE NONCLUSTERED INDEX IX_GL_Stores_IsActive ON dbo.GL_Stores (IsActive);
                END;

                -- 2. GL_StoreAgreements
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_StoreAgreements')
                BEGIN
                    CREATE TABLE dbo.GL_StoreAgreements (
                        AgreementID INT IDENTITY(1,1) NOT NULL,
                        StoreID INT NOT NULL,
                        OrganisationBusinessEntityID INT NOT NULL,
                        AgreementNumber NVARCHAR(100) NOT NULL,
                        EffectiveFrom DATE NOT NULL,
                        EffectiveTo DATE NULL,
                        MinimumMonths INT NOT NULL CONSTRAINT DF_GL_StoreAgreements_MinimumMonths DEFAULT (1),
                        MaximumMonths INT NOT NULL CONSTRAINT DF_GL_StoreAgreements_MaximumMonths DEFAULT (36),
                        MaximumLoanAmount DECIMAL(18,2) NOT NULL,
                        InterestRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_GL_StoreAgreements_InterestRate DEFAULT (0.0),
                        SettlementMode NVARCHAR(50) NOT NULL CONSTRAINT DF_GL_StoreAgreements_SettlementMode DEFAULT ('MONTHLY'),
                        IsActive BIT NOT NULL CONSTRAINT DF_GL_StoreAgreements_IsActive DEFAULT (1),
                        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_StoreAgreements_CreatedDate DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_GL_StoreAgreements PRIMARY KEY CLUSTERED (AgreementID),
                        CONSTRAINT FK_GL_StoreAgreements_Stores FOREIGN KEY (StoreID) REFERENCES dbo.GL_Stores (StoreID),
                        CONSTRAINT FK_GL_StoreAgreements_Organisations FOREIGN KEY (OrganisationBusinessEntityID) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_GL_StoreAgreements_Org_AgreementNumber ON dbo.GL_StoreAgreements (OrganisationBusinessEntityID, AgreementNumber);
                    CREATE NONCLUSTERED INDEX IX_GL_StoreAgreements_Org_Store_Active ON dbo.GL_StoreAgreements (OrganisationBusinessEntityID, StoreID, IsActive);
                END;

                -- 3. GL_DeductionPolicies
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_DeductionPolicies')
                BEGIN
                    CREATE TABLE dbo.GL_DeductionPolicies (
                        PolicyID INT IDENTITY(1,1) NOT NULL,
                        OrganisationBusinessEntityID INT NOT NULL,
                        PolicyName NVARCHAR(150) NOT NULL,
                        MaximumSalaryPercentage DECIMAL(5,2) NOT NULL CONSTRAINT DF_GL_DeductionPolicies_MaxSalaryPercent DEFAULT (20.0),
                        MinimumNetSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_GL_DeductionPolicies_MinNetSalary DEFAULT (0.0),
                        MaximumActiveLoans INT NULL,
                        MaximumOutstandingAmount DECIMAL(18,2) NULL,
                        SalaryBasis NVARCHAR(50) NOT NULL CONSTRAINT DF_GL_DeductionPolicies_SalaryBasis DEFAULT ('BasicSalary'),
                        EffectiveFrom DATE NOT NULL,
                        EffectiveTo DATE NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_GL_DeductionPolicies_IsActive DEFAULT (1),
                        CONSTRAINT PK_GL_DeductionPolicies PRIMARY KEY CLUSTERED (PolicyID),
                        CONSTRAINT FK_GL_DeductionPolicies_Organisations FOREIGN KEY (OrganisationBusinessEntityID) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE NONCLUSTERED INDEX IX_GL_DeductionPolicies_Org_Active ON dbo.GL_DeductionPolicies (OrganisationBusinessEntityID, IsActive);
                END;

                -- 4. GL_Loans
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_Loans')
                BEGIN
                    CREATE TABLE dbo.GL_Loans (
                        LoanID INT IDENTITY(1,1) NOT NULL,
                        LoanNumber NVARCHAR(100) NOT NULL,
                        OrganisationBusinessEntityID INT NOT NULL,
                        IndividualID INT NOT NULL,
                        JobID INT NOT NULL,
                        StoreID INT NOT NULL,
                        AgreementID INT NOT NULL,
                        PolicyID INT NOT NULL,
                        RequestID INT NULL,
                        WorkflowRequestInstanceID INT NULL,
                        PurchaseDate DATE NOT NULL,
                        InvoiceNumber NVARCHAR(100) NOT NULL,
                        PrincipalAmount DECIMAL(18,2) NOT NULL,
                        TotalRepayableAmount DECIMAL(18,2) NOT NULL,
                        RepaymentMonths INT NOT NULL,
                        FirstDeductionMonth DATE NOT NULL,
                        SalaryAtApproval DECIMAL(18,2) NOT NULL CONSTRAINT DF_GL_Loans_SalaryAtApproval DEFAULT (0.0),
                        EligibleSalaryAtApproval DECIMAL(18,2) NOT NULL CONSTRAINT DF_GL_Loans_EligibleSalary DEFAULT (0.0),
                        DeductionPercentageAtApproval DECIMAL(5,2) NOT NULL CONSTRAINT DF_GL_Loans_DeductionPercent DEFAULT (0.0),
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_GL_Loans_Status DEFAULT ('DRAFT'),
                        ApprovedByUserID INT NULL,
                        ApprovedDate DATETIME2(0) NULL,
                        ClosedDate DATETIME2(0) NULL,
                        Remarks NVARCHAR(1000) NULL,
                        CreatedByUserID INT NOT NULL,
                        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_Loans_CreatedDate DEFAULT (SYSUTCDATETIME()),
                        ModifiedDate DATETIME2(0) NULL,
                        RowVersion ROWVERSION NOT NULL,
                        CONSTRAINT PK_GL_Loans PRIMARY KEY CLUSTERED (LoanID),
                        CONSTRAINT FK_GL_Loans_Organisations FOREIGN KEY (OrganisationBusinessEntityID) REFERENCES dbo.Organisations (BusinessEntityID),
                        CONSTRAINT FK_GL_Loans_Individuals FOREIGN KEY (IndividualID) REFERENCES dbo.BE_Individuals (BusinessEntityID),
                        CONSTRAINT FK_GL_Loans_Jobs FOREIGN KEY (JobID) REFERENCES dbo.Jobs (JobID),
                        CONSTRAINT FK_GL_Loans_Stores FOREIGN KEY (StoreID) REFERENCES dbo.GL_Stores (StoreID),
                        CONSTRAINT FK_GL_Loans_StoreAgreements FOREIGN KEY (AgreementID) REFERENCES dbo.GL_StoreAgreements (AgreementID),
                        CONSTRAINT FK_GL_Loans_DeductionPolicies FOREIGN KEY (PolicyID) REFERENCES dbo.GL_DeductionPolicies (PolicyID),
                        CONSTRAINT FK_GL_Loans_Requests FOREIGN KEY (RequestID) REFERENCES dbo.Requests (RequestID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_GL_Loans_Org_LoanNumber ON dbo.GL_Loans (OrganisationBusinessEntityID, LoanNumber);
                    CREATE NONCLUSTERED INDEX IX_GL_Loans_Org_Individual_Status ON dbo.GL_Loans (OrganisationBusinessEntityID, IndividualID, Status);
                    CREATE NONCLUSTERED INDEX IX_GL_Loans_Job_Status ON dbo.GL_Loans (JobID, Status);
                    CREATE NONCLUSTERED INDEX IX_GL_Loans_StoreID ON dbo.GL_Loans (StoreID);
                    CREATE NONCLUSTERED INDEX IX_GL_Loans_RequestID ON dbo.GL_Loans (RequestID);
                END;

                -- 5. GL_LoanItems
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_LoanItems')
                BEGIN
                    CREATE TABLE dbo.GL_LoanItems (
                        LoanItemID INT IDENTITY(1,1) NOT NULL,
                        LoanID INT NOT NULL,
                        ItemDescription NVARCHAR(300) NOT NULL,
                        Quantity INT NOT NULL CONSTRAINT DF_GL_LoanItems_Quantity DEFAULT (1),
                        UnitPrice DECIMAL(18,2) NOT NULL,
                        TotalAmount DECIMAL(18,2) NOT NULL,
                        CONSTRAINT PK_GL_LoanItems PRIMARY KEY CLUSTERED (LoanItemID),
                        CONSTRAINT FK_GL_LoanItems_Loans FOREIGN KEY (LoanID) REFERENCES dbo.GL_Loans (LoanID) ON DELETE CASCADE
                    );

                    CREATE NONCLUSTERED INDEX IX_GL_LoanItems_LoanID ON dbo.GL_LoanItems (LoanID);
                END;

                -- 6. GL_LoanInstallments
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_LoanInstallments')
                BEGIN
                    CREATE TABLE dbo.GL_LoanInstallments (
                        InstallmentID INT IDENTITY(1,1) NOT NULL,
                        LoanID INT NOT NULL,
                        InstallmentNumber INT NOT NULL,
                        DueMonth DATE NOT NULL,
                        ScheduledAmount DECIMAL(18,2) NOT NULL,
                        PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_GL_LoanInstallments_PaidAmount DEFAULT (0.0),
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_GL_LoanInstallments_Status DEFAULT ('PENDING'),
                        LastPaymentDate DATETIME2(0) NULL,
                        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_LoanInstallments_CreatedDate DEFAULT (SYSUTCDATETIME()),
                        RowVersion ROWVERSION NOT NULL,
                        CONSTRAINT PK_GL_LoanInstallments PRIMARY KEY CLUSTERED (InstallmentID),
                        CONSTRAINT FK_GL_LoanInstallments_Loans FOREIGN KEY (LoanID) REFERENCES dbo.GL_Loans (LoanID) ON DELETE CASCADE
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_GL_LoanInstallments_Loan_Number ON dbo.GL_LoanInstallments (LoanID, InstallmentNumber);
                    CREATE NONCLUSTERED INDEX IX_GL_LoanInstallments_DueMonth_Status ON dbo.GL_LoanInstallments (DueMonth, Status);
                END;

                -- 7. GL_PayrollDeductions
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_PayrollDeductions')
                BEGIN
                    CREATE TABLE dbo.GL_PayrollDeductions (
                        DeductionID INT IDENTITY(1,1) NOT NULL,
                        InstallmentID INT NOT NULL,
                        PayrollRunID INT NOT NULL,
                        PayrollItemID INT NULL,
                        TransactionType NVARCHAR(50) NOT NULL CONSTRAINT DF_GL_PayrollDeductions_TransactionType DEFAULT ('DEDUCTION'),
                        Amount DECIMAL(18,2) NOT NULL,
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_GL_PayrollDeductions_Status DEFAULT ('PENDING'),
                        DeductionDate DATE NOT NULL,
                        ReversesDeductionID INT NULL,
                        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_PayrollDeductions_CreatedDate DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_GL_PayrollDeductions PRIMARY KEY CLUSTERED (DeductionID),
                        CONSTRAINT FK_GL_PayrollDeductions_Installments FOREIGN KEY (InstallmentID) REFERENCES dbo.GL_LoanInstallments (InstallmentID)
                    );

                    CREATE NONCLUSTERED INDEX IX_GL_PayrollDeductions_Inst_Run_Type ON dbo.GL_PayrollDeductions (InstallmentID, PayrollRunID, TransactionType);
                    CREATE NONCLUSTERED INDEX IX_GL_PayrollDeductions_PayrollRunID ON dbo.GL_PayrollDeductions (PayrollRunID);
                END;

                -- 8. GL_StoreSettlements
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_StoreSettlements')
                BEGIN
                    CREATE TABLE dbo.GL_StoreSettlements (
                        SettlementID INT IDENTITY(1,1) NOT NULL,
                        OrganisationBusinessEntityID INT NOT NULL,
                        StoreID INT NOT NULL,
                        SettlementNumber NVARCHAR(100) NOT NULL,
                        PaymentDate DATE NOT NULL,
                        PaymentReference NVARCHAR(150) NOT NULL,
                        TotalAmount DECIMAL(18,2) NOT NULL,
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_GL_StoreSettlements_Status DEFAULT ('DRAFT'),
                        CreatedByUserID INT NOT NULL,
                        CreatedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_StoreSettlements_CreatedDate DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_GL_StoreSettlements PRIMARY KEY CLUSTERED (SettlementID),
                        CONSTRAINT FK_GL_StoreSettlements_Stores FOREIGN KEY (StoreID) REFERENCES dbo.GL_Stores (StoreID),
                        CONSTRAINT FK_GL_StoreSettlements_Organisations FOREIGN KEY (OrganisationBusinessEntityID) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_GL_StoreSettlements_Org_SettlementNumber ON dbo.GL_StoreSettlements (OrganisationBusinessEntityID, SettlementNumber);
                    CREATE NONCLUSTERED INDEX IX_GL_StoreSettlements_Org_Store_Status ON dbo.GL_StoreSettlements (OrganisationBusinessEntityID, StoreID, Status);
                END;

                -- 9. GL_StoreSettlementAllocations
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_StoreSettlementAllocations')
                BEGIN
                    CREATE TABLE dbo.GL_StoreSettlementAllocations (
                        AllocationID INT IDENTITY(1,1) NOT NULL,
                        SettlementID INT NOT NULL,
                        LoanID INT NOT NULL,
                        AllocatedAmount DECIMAL(18,2) NOT NULL,
                        CONSTRAINT PK_GL_StoreSettlementAllocations PRIMARY KEY CLUSTERED (AllocationID),
                        CONSTRAINT FK_GL_StoreSettlementAllocations_Settlements FOREIGN KEY (SettlementID) REFERENCES dbo.GL_StoreSettlements (SettlementID) ON DELETE CASCADE,
                        CONSTRAINT FK_GL_StoreSettlementAllocations_Loans FOREIGN KEY (LoanID) REFERENCES dbo.GL_Loans (LoanID)
                    );

                    CREATE NONCLUSTERED INDEX IX_GL_StoreSettlementAllocations_Settlement_Loan ON dbo.GL_StoreSettlementAllocations (SettlementID, LoanID);
                END;

                -- 10. GL_LoanStatusHistory
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_LoanStatusHistory')
                BEGIN
                    CREATE TABLE dbo.GL_LoanStatusHistory (
                        HistoryID INT IDENTITY(1,1) NOT NULL,
                        LoanID INT NOT NULL,
                        PreviousStatus NVARCHAR(50) NOT NULL,
                        NewStatus NVARCHAR(50) NOT NULL,
                        Reason NVARCHAR(500) NULL,
                        ChangedByUserID INT NOT NULL,
                        ChangedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_LoanStatusHistory_ChangedDate DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_GL_LoanStatusHistory PRIMARY KEY CLUSTERED (HistoryID),
                        CONSTRAINT FK_GL_LoanStatusHistory_Loans FOREIGN KEY (LoanID) REFERENCES dbo.GL_Loans (LoanID) ON DELETE CASCADE
                    );

                    CREATE NONCLUSTERED INDEX IX_GL_LoanStatusHistory_LoanID ON dbo.GL_LoanStatusHistory (LoanID);
                END;

                -- 11. GL_LoanDocuments
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GL_LoanDocuments')
                BEGIN
                    CREATE TABLE dbo.GL_LoanDocuments (
                        DocumentID INT IDENTITY(1,1) NOT NULL,
                        LoanID INT NOT NULL,
                        FileName NVARCHAR(250) NOT NULL,
                        ContentType NVARCHAR(100) NOT NULL,
                        FileSize BIGINT NOT NULL CONSTRAINT DF_GL_LoanDocuments_FileSize DEFAULT (0),
                        FileBytes VARBINARY(MAX) NOT NULL,
                        UploadedByUserID INT NOT NULL,
                        UploadedDate DATETIME2(0) NOT NULL CONSTRAINT DF_GL_LoanDocuments_UploadedDate DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_GL_LoanDocuments PRIMARY KEY CLUSTERED (DocumentID),
                        CONSTRAINT FK_GL_LoanDocuments_Loans FOREIGN KEY (LoanID) REFERENCES dbo.GL_Loans (LoanID) ON DELETE CASCADE
                    );

                    CREATE NONCLUSTERED INDEX IX_GL_LoanDocuments_LoanID ON dbo.GL_LoanDocuments (LoanID);
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS dbo.GL_LoanDocuments;
                DROP TABLE IF EXISTS dbo.GL_LoanStatusHistory;
                DROP TABLE IF EXISTS dbo.GL_StoreSettlementAllocations;
                DROP TABLE IF EXISTS dbo.GL_StoreSettlements;
                DROP TABLE IF EXISTS dbo.GL_PayrollDeductions;
                DROP TABLE IF EXISTS dbo.GL_LoanInstallments;
                DROP TABLE IF EXISTS dbo.GL_LoanItems;
                DROP TABLE IF EXISTS dbo.GL_Loans;
                DROP TABLE IF EXISTS dbo.GL_DeductionPolicies;
                DROP TABLE IF EXISTS dbo.GL_StoreAgreements;
                DROP TABLE IF EXISTS dbo.GL_Stores;
            ");
        }
    }
}
