using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261109160000_StaffBankAndEmergencyContacts")]
    public sealed class StaffBankAndEmergencyContacts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. BK_StaffBankAccounts
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BK_StaffBankAccounts')
                BEGIN
                    CREATE TABLE dbo.BK_StaffBankAccounts (
                        Id INT IDENTITY(1,1) NOT NULL,
                        IndividualId INT NOT NULL,
                        BankName NVARCHAR(150) NOT NULL,
                        BankCode NVARCHAR(50) NULL,
                        BranchName NVARCHAR(150) NULL,
                        AccountHolderName NVARCHAR(200) NOT NULL,
                        AccountNumber NVARCHAR(100) NOT NULL,
                        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_BK_StaffBankAccounts_Currency DEFAULT ('MVR'),
                        IsPrimary BIT NOT NULL CONSTRAINT DF_BK_StaffBankAccounts_IsPrimary DEFAULT (0),
                        Remarks NVARCHAR(500) NULL,
                        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_BK_StaffBankAccounts_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2(0) NULL,
                        CONSTRAINT PK_BK_StaffBankAccounts PRIMARY KEY CLUSTERED (Id),
                        CONSTRAINT FK_BK_StaffBankAccounts_Individual FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID) ON DELETE CASCADE
                    );

                    CREATE NONCLUSTERED INDEX IX_BK_StaffBankAccounts_IndividualId ON dbo.BK_StaffBankAccounts (IndividualId);
                    CREATE NONCLUSTERED INDEX IX_BK_StaffBankAccounts_IsPrimary ON dbo.BK_StaffBankAccounts (IndividualId, IsPrimary);
                END;

                -- 2. EC_StaffEmergencyContacts
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EC_StaffEmergencyContacts')
                BEGIN
                    CREATE TABLE dbo.EC_StaffEmergencyContacts (
                        Id INT IDENTITY(1,1) NOT NULL,
                        IndividualId INT NOT NULL,
                        ContactName NVARCHAR(200) NOT NULL,
                        Relationship NVARCHAR(100) NOT NULL,
                        PrimaryPhone NVARCHAR(50) NOT NULL,
                        SecondaryPhone NVARCHAR(50) NULL,
                        Email NVARCHAR(150) NULL,
                        Address NVARCHAR(500) NULL,
                        IsPrimary BIT NOT NULL CONSTRAINT DF_EC_StaffEmergencyContacts_IsPrimary DEFAULT (0),
                        Notes NVARCHAR(500) NULL,
                        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_EC_StaffEmergencyContacts_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2(0) NULL,
                        CONSTRAINT PK_EC_StaffEmergencyContacts PRIMARY KEY CLUSTERED (Id),
                        CONSTRAINT FK_EC_StaffEmergencyContacts_Individual FOREIGN KEY (IndividualId) REFERENCES dbo.BE_Individuals (BusinessEntityID) ON DELETE CASCADE
                    );

                    CREATE NONCLUSTERED INDEX IX_EC_StaffEmergencyContacts_IndividualId ON dbo.EC_StaffEmergencyContacts (IndividualId);
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS dbo.EC_StaffEmergencyContacts;
                DROP TABLE IF EXISTS dbo.BK_StaffBankAccounts;
            ");
        }
    }
}
