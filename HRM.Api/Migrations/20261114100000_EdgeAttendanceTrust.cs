using Microsoft.EntityFrameworkCore.Migrations;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
#nullable disable
namespace HRM.Migrations;
[DbContext(typeof(HrmTeContext))]
[Migration("20261114100000_EdgeAttendanceTrust")]
public partial class EdgeAttendanceTrust : Migration
{
 protected override void Up(MigrationBuilder m)=>m.Sql("""
CREATE TABLE dbo.EA_TrustedIssuers (EdgeAttendanceTrustedIssuerId int IDENTITY(1,1) NOT NULL PRIMARY KEY, Subject nvarchar(500) NOT NULL, Thumbprint nvarchar(128) NOT NULL, IsActive bit NOT NULL, CreatedAtUtc datetime2 NOT NULL, CONSTRAINT UX_EA_TrustedIssuers_Thumbprint UNIQUE (Thumbprint));
CREATE TABLE dbo.EA_Agents (EdgeAttendanceAgentId int IDENTITY(1,1) NOT NULL PRIMARY KEY, AgentIdentifier nvarchar(100) NOT NULL, OrganisationId int NOT NULL, CertificateThumbprint nvarchar(128) NOT NULL, CertificateIssuer nvarchar(500) NOT NULL, CertificateNotAfterUtc datetime2 NOT NULL, IsActive bit NOT NULL, RevokedAtUtc datetime2 NULL, CreatedAtUtc datetime2 NOT NULL, LastSeenAtUtc datetime2 NULL, LastHealthStatus nvarchar(40) NULL, CONSTRAINT UX_EA_Agents_AgentIdentifier UNIQUE (AgentIdentifier), CONSTRAINT UX_EA_Agents_CertificateThumbprint UNIQUE (CertificateThumbprint), CONSTRAINT FK_EA_Agents_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations(BusinessEntityID));
CREATE TABLE dbo.EA_AgentDevices (EdgeAttendanceAgentDeviceId int IDENTITY(1,1) NOT NULL PRIMARY KEY, EdgeAttendanceAgentId int NOT NULL, AttendanceDeviceId int NOT NULL, IsActive bit NOT NULL, CreatedAtUtc datetime2 NOT NULL, CONSTRAINT UX_EA_AgentDevices_Agent_Device UNIQUE (EdgeAttendanceAgentId,AttendanceDeviceId), CONSTRAINT FK_EA_AgentDevices_Agents FOREIGN KEY (EdgeAttendanceAgentId) REFERENCES dbo.EA_Agents(EdgeAttendanceAgentId), CONSTRAINT FK_EA_AgentDevices_AttendanceDevices FOREIGN KEY (AttendanceDeviceId) REFERENCES dbo.AttendanceDevices(AttendanceDeviceID));
CREATE TABLE dbo.EA_EventReceipts (EdgeAttendanceEventReceiptId bigint IDENTITY(1,1) NOT NULL PRIMARY KEY, EdgeAttendanceAgentId int NOT NULL, AttendanceDeviceId int NOT NULL, DeviceEventId nvarchar(200) NOT NULL, EventHash nvarchar(128) NOT NULL, LocalUserId nvarchar(100) NOT NULL, DeviceTimestampUtc datetime2 NOT NULL, ReceivedAtUtc datetime2 NOT NULL, ProcessingStatus nvarchar(30) NOT NULL, AttendanceLogId int NULL, CONSTRAINT UX_EA_EventReceipts_Device_Event UNIQUE (AttendanceDeviceId,DeviceEventId), CONSTRAINT UX_EA_EventReceipts_EventHash UNIQUE (EventHash), CONSTRAINT FK_EA_EventReceipts_Agents FOREIGN KEY (EdgeAttendanceAgentId) REFERENCES dbo.EA_Agents(EdgeAttendanceAgentId), CONSTRAINT FK_EA_EventReceipts_AttendanceDevices FOREIGN KEY (AttendanceDeviceId) REFERENCES dbo.AttendanceDevices(AttendanceDeviceID), CONSTRAINT FK_EA_EventReceipts_AttendanceLogs FOREIGN KEY (AttendanceLogId) REFERENCES dbo.AttendanceLogs(AttendanceLogID));
CREATE TABLE dbo.EA_IngestionFailures (EdgeAttendanceIngestionFailureId bigint IDENTITY(1,1) NOT NULL PRIMARY KEY, EdgeAttendanceAgentId int NULL, AttendanceDeviceId int NULL, FailureCode nvarchar(80) NOT NULL, Detail nvarchar(2000) NOT NULL, OccurredAtUtc datetime2 NOT NULL, CONSTRAINT FK_EA_IngestionFailures_Agents FOREIGN KEY (EdgeAttendanceAgentId) REFERENCES dbo.EA_Agents(EdgeAttendanceAgentId), CONSTRAINT FK_EA_IngestionFailures_AttendanceDevices FOREIGN KEY (AttendanceDeviceId) REFERENCES dbo.AttendanceDevices(AttendanceDeviceID));
CREATE INDEX IX_EA_IngestionFailures_Agent_Occurred ON dbo.EA_IngestionFailures(EdgeAttendanceAgentId,OccurredAtUtc);
""");
 protected override void Down(MigrationBuilder m)=>m.Sql("DROP TABLE dbo.EA_IngestionFailures; DROP TABLE dbo.EA_EventReceipts; DROP TABLE dbo.EA_AgentDevices; DROP TABLE dbo.EA_Agents; DROP TABLE dbo.EA_TrustedIssuers;");
}
