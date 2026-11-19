using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261119090000_EdgeConnectorEnrollmentAndCommands")]
public partial class EdgeConnectorEnrollmentAndCommands : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("""
            IF COL_LENGTH('dbo.EA_Agents', 'SiteName') IS NULL
                ALTER TABLE dbo.EA_Agents ADD SiteName nvarchar(200) NULL;
            IF COL_LENGTH('dbo.EA_Agents', 'ConnectorVersion') IS NULL
                ALTER TABLE dbo.EA_Agents ADD ConnectorVersion nvarchar(64) NULL;
            IF COL_LENGTH('dbo.EA_Agents', 'OperatingSystem') IS NULL
                ALTER TABLE dbo.EA_Agents ADD OperatingSystem nvarchar(128) NULL;
            IF COL_LENGTH('dbo.EA_Agents', 'ConfigurationVersion') IS NULL
                ALTER TABLE dbo.EA_Agents ADD ConfigurationVersion int NOT NULL CONSTRAINT DF_EA_Agents_ConfigurationVersion DEFAULT(1);
            IF COL_LENGTH('dbo.EA_Agents', 'LastSyncAtUtc') IS NULL
                ALTER TABLE dbo.EA_Agents ADD LastSyncAtUtc datetime2 NULL;
            IF COL_LENGTH('dbo.EA_Agents', 'PendingEventCount') IS NULL
                ALTER TABLE dbo.EA_Agents ADD PendingEventCount int NOT NULL CONSTRAINT DF_EA_Agents_PendingEventCount DEFAULT(0);
            IF COL_LENGTH('dbo.EA_Agents', 'PublicKeyPem') IS NULL
                ALTER TABLE dbo.EA_Agents ADD PublicKeyPem nvarchar(4000) NULL;

            IF COL_LENGTH('dbo.EA_AgentDevices', 'PollingIntervalSeconds') IS NULL
                ALTER TABLE dbo.EA_AgentDevices ADD PollingIntervalSeconds int NOT NULL CONSTRAINT DF_EA_AgentDevices_PollingInterval DEFAULT(30);

            IF OBJECT_ID('dbo.EA_ActivationCodes', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.EA_ActivationCodes (
                    EdgeAttendanceActivationCodeId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    CodeHash nvarchar(128) NOT NULL,
                    CodePrefix nvarchar(32) NOT NULL,
                    OrganisationId int NOT NULL,
                    SiteName nvarchar(200) NULL,
                    SuggestedConnectorName nvarchar(100) NULL,
                    DeviceIdsJson nvarchar(2000) NULL,
                    CreatedByIndividualId int NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    ExpiresAtUtc datetime2 NOT NULL,
                    UsedAtUtc datetime2 NULL,
                    UsedByEdgeAttendanceAgentId int NULL,
                    IsRevoked bit NOT NULL CONSTRAINT DF_EA_ActivationCodes_IsRevoked DEFAULT(0),
                    CONSTRAINT UX_EA_ActivationCodes_CodeHash UNIQUE (CodeHash),
                    CONSTRAINT FK_EA_ActivationCodes_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID),
                    CONSTRAINT FK_EA_ActivationCodes_Agents FOREIGN KEY (UsedByEdgeAttendanceAgentId) REFERENCES dbo.EA_Agents(EdgeAttendanceAgentId)
                );
                CREATE INDEX IX_EA_ActivationCodes_Org_Expires ON dbo.EA_ActivationCodes (OrganisationId, ExpiresAtUtc);
            END

            IF OBJECT_ID('dbo.EA_ConnectorCommands', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.EA_ConnectorCommands (
                    EdgeAttendanceConnectorCommandId bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    EdgeAttendanceAgentId int NOT NULL,
                    CommandType nvarchar(80) NOT NULL,
                    PayloadJson nvarchar(4000) NULL,
                    Status nvarchar(30) NOT NULL,
                    CreatedByIndividualId int NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    DispatchedAtUtc datetime2 NULL,
                    CompletedAtUtc datetime2 NULL,
                    ResultJson nvarchar(4000) NULL,
                    ErrorMessage nvarchar(1000) NULL,
                    CONSTRAINT FK_EA_ConnectorCommands_Agents FOREIGN KEY (EdgeAttendanceAgentId) REFERENCES dbo.EA_Agents(EdgeAttendanceAgentId)
                );
                CREATE INDEX IX_EA_ConnectorCommands_Agent_Status_Created ON dbo.EA_ConnectorCommands (EdgeAttendanceAgentId, Status, CreatedAtUtc);
            END
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("""
            IF OBJECT_ID('dbo.EA_ConnectorCommands', 'U') IS NOT NULL
                DROP TABLE dbo.EA_ConnectorCommands;
            IF OBJECT_ID('dbo.EA_ActivationCodes', 'U') IS NOT NULL
                DROP TABLE dbo.EA_ActivationCodes;

            IF COL_LENGTH('dbo.EA_AgentDevices', 'PollingIntervalSeconds') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.EA_AgentDevices DROP CONSTRAINT IF EXISTS DF_EA_AgentDevices_PollingInterval;
                ALTER TABLE dbo.EA_AgentDevices DROP COLUMN PollingIntervalSeconds;
            END

            IF COL_LENGTH('dbo.EA_Agents', 'PublicKeyPem') IS NOT NULL
                ALTER TABLE dbo.EA_Agents DROP COLUMN PublicKeyPem;
            IF COL_LENGTH('dbo.EA_Agents', 'PendingEventCount') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.EA_Agents DROP CONSTRAINT IF EXISTS DF_EA_Agents_PendingEventCount;
                ALTER TABLE dbo.EA_Agents DROP COLUMN PendingEventCount;
            END
            IF COL_LENGTH('dbo.EA_Agents', 'LastSyncAtUtc') IS NOT NULL
                ALTER TABLE dbo.EA_Agents DROP COLUMN LastSyncAtUtc;
            IF COL_LENGTH('dbo.EA_Agents', 'ConfigurationVersion') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.EA_Agents DROP CONSTRAINT IF EXISTS DF_EA_Agents_ConfigurationVersion;
                ALTER TABLE dbo.EA_Agents DROP COLUMN ConfigurationVersion;
            END
            IF COL_LENGTH('dbo.EA_Agents', 'OperatingSystem') IS NOT NULL
                ALTER TABLE dbo.EA_Agents DROP COLUMN OperatingSystem;
            IF COL_LENGTH('dbo.EA_Agents', 'ConnectorVersion') IS NOT NULL
                ALTER TABLE dbo.EA_Agents DROP COLUMN ConnectorVersion;
            IF COL_LENGTH('dbo.EA_Agents', 'SiteName') IS NOT NULL
                ALTER TABLE dbo.EA_Agents DROP COLUMN SiteName;
            """);
    }
}
