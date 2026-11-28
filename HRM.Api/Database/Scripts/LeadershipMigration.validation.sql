BEGIN TRANSACTION;
GO

CREATE TABLE [LM_AppointmentTypes] (
    [LeadershipAppointmentTypeId] int NOT NULL IDENTITY,
    [Name] nvarchar(120) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LM_AppointmentTypes] PRIMARY KEY ([LeadershipAppointmentTypeId])
);
GO

CREATE TABLE [LM_Appointments] (
    [LeadershipAppointmentId] int NOT NULL IDENTITY,
    [IndividualId] int NOT NULL,
    [LeadershipAppointmentTypeId] int NOT NULL,
    [PositionTitle] nvarchar(200) NOT NULL,
    [AppointingOrganisationId] int NOT NULL,
    [AdministrativeOrganisationId] int NULL,
    [EffectiveFrom] date NOT NULL,
    [EffectiveTo] date NULL,
    [Status] nvarchar(30) NOT NULL,
    [AppointmentReference] nvarchar(100) NULL,
    [OperationLogId] int NULL,
    CONSTRAINT [PK_LM_Appointments] PRIMARY KEY ([LeadershipAppointmentId]),
    CONSTRAINT [FK_LM_Appointments_BE_Individuals] FOREIGN KEY ([IndividualId]) REFERENCES [BE_Individuals] ([BusinessEntityID]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_Appointments_Types] FOREIGN KEY ([LeadershipAppointmentTypeId]) REFERENCES [LM_AppointmentTypes] ([LeadershipAppointmentTypeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_Appointments_AppointingOrganisation] FOREIGN KEY ([AppointingOrganisationId]) REFERENCES [Organisations] ([BusinessEntityID]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_Appointments_AdministrativeOrganisation] FOREIGN KEY ([AdministrativeOrganisationId]) REFERENCES [Organisations] ([BusinessEntityID]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_Appointments_OperationLogs] FOREIGN KEY ([OperationLogId]) REFERENCES [OperationLogs] ([OperationLogID]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LM_AppointmentOrganisations] (
    [LeadershipAppointmentOrganisationId] int NOT NULL IDENTITY,
    [LeadershipAppointmentId] int NOT NULL,
    [OrganisationId] int NOT NULL,
    [RelationshipType] nvarchar(50) NOT NULL,
    [EffectiveFrom] date NOT NULL,
    [EffectiveTo] date NULL,
    CONSTRAINT [PK_LM_AppointmentOrganisations] PRIMARY KEY ([LeadershipAppointmentOrganisationId]),
    CONSTRAINT [FK_LM_AppointmentOrganisations_Appointments] FOREIGN KEY ([LeadershipAppointmentId]) REFERENCES [LM_Appointments] ([LeadershipAppointmentId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_AppointmentOrganisations_Organisations] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([BusinessEntityID]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LM_AuthorityGrants] (
    [LeadershipAuthorityGrantId] int NOT NULL IDENTITY,
    [LeadershipAppointmentOrganisationId] int NOT NULL,
    [PermissionCode] nvarchar(120) NOT NULL,
    [ScopeType] nvarchar(40) NOT NULL,
    [Conditions] nvarchar(1000) NULL,
    [ValidFrom] date NOT NULL,
    [ValidTo] date NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LM_AuthorityGrants] PRIMARY KEY ([LeadershipAuthorityGrantId]),
    CONSTRAINT [FK_LM_AuthorityGrants_AppointmentOrganisations] FOREIGN KEY ([LeadershipAppointmentOrganisationId]) REFERENCES [LM_AppointmentOrganisations] ([LeadershipAppointmentOrganisationId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LM_AttendancePolicies] (
    [LeadershipAttendancePolicyId] int NOT NULL IDENTITY,
    [LeadershipAppointmentId] int NOT NULL,
    [ManagingOrganisationId] int NOT NULL,
    [AttendanceRequired] bit NOT NULL,
    [VerificationMode] nvarchar(40) NOT NULL,
    [EffectiveFrom] date NOT NULL,
    [EffectiveTo] date NULL,
    CONSTRAINT [PK_LM_AttendancePolicies] PRIMARY KEY ([LeadershipAttendancePolicyId]),
    CONSTRAINT [FK_LM_AttendancePolicies_Appointments] FOREIGN KEY ([LeadershipAppointmentId]) REFERENCES [LM_Appointments] ([LeadershipAppointmentId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_AttendancePolicies_Organisations] FOREIGN KEY ([ManagingOrganisationId]) REFERENCES [Organisations] ([BusinessEntityID]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LM_DeviceAccesses] (
    [LeadershipDeviceAccessId] int NOT NULL IDENTITY,
    [LeadershipAppointmentId] int NOT NULL,
    [AttendanceDeviceId] int NOT NULL,
    [DeviceUserId] nvarchar(100) NOT NULL,
    [ValidFrom] date NOT NULL,
    [ValidTo] date NULL,
    CONSTRAINT [PK_LM_DeviceAccesses] PRIMARY KEY ([LeadershipDeviceAccessId]),
    CONSTRAINT [FK_LM_DeviceAccesses_Appointments] FOREIGN KEY ([LeadershipAppointmentId]) REFERENCES [LM_Appointments] ([LeadershipAppointmentId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_DeviceAccesses_AttendanceDevices] FOREIGN KEY ([AttendanceDeviceId]) REFERENCES [AttendanceDevices] ([AttendanceDeviceID]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LM_Delegations] (
    [LeadershipDelegationId] int NOT NULL IDENTITY,
    [FromLeadershipAppointmentId] int NOT NULL,
    [ToUserId] int NOT NULL,
    [OrganisationId] int NOT NULL,
    [PermissionScope] nvarchar(120) NOT NULL,
    [ValidFrom] date NOT NULL,
    [ValidTo] date NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LM_Delegations] PRIMARY KEY ([LeadershipDelegationId]),
    CONSTRAINT [FK_LM_Delegations_Appointments] FOREIGN KEY ([FromLeadershipAppointmentId]) REFERENCES [LM_Appointments] ([LeadershipAppointmentId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_Delegations_Users] FOREIGN KEY ([ToUserId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_Delegations_Organisations] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([BusinessEntityID]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LM_AuditLogs] (
    [LeadershipAuditLogId] bigint NOT NULL IDENTITY,
    [LeadershipAppointmentId] int NOT NULL,
    [Action] nvarchar(80) NOT NULL,
    [OrganisationId] int NULL,
    [PerformedByUserId] int NOT NULL,
    [TimestampUtc] datetime2 NOT NULL,
    [Detail] nvarchar(2000) NULL,
    CONSTRAINT [PK_LM_AuditLogs] PRIMARY KEY ([LeadershipAuditLogId]),
    CONSTRAINT [FK_LM_AuditLogs_Appointments] FOREIGN KEY ([LeadershipAppointmentId]) REFERENCES [LM_Appointments] ([LeadershipAppointmentId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LM_AuditLogs_Users] FOREIGN KEY ([PerformedByUserId]) REFERENCES [Users] ([UserID]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [UX_LM_AppointmentTypes_Name] ON [LM_AppointmentTypes] ([Name]);
GO

CREATE INDEX [IX_LM_Appointments_IndividualId_Status_EffectiveFrom] ON [LM_Appointments] ([IndividualId], [Status], [EffectiveFrom]);
GO

CREATE UNIQUE INDEX [UX_LM_AppointmentOrganisations_Link] ON [LM_AppointmentOrganisations] ([LeadershipAppointmentId], [OrganisationId], [RelationshipType], [EffectiveFrom]);
GO

CREATE INDEX [IX_LM_AuthorityGrants_Link] ON [LM_AuthorityGrants] ([LeadershipAppointmentOrganisationId], [PermissionCode], [IsActive], [ValidFrom]);
GO

CREATE INDEX [IX_LM_DeviceAccesses_Device] ON [LM_DeviceAccesses] ([AttendanceDeviceId], [DeviceUserId], [ValidFrom]);
GO

CREATE INDEX [IX_LM_Delegations_User] ON [LM_Delegations] ([ToUserId], [OrganisationId], [IsActive], [ValidFrom]);
GO

CREATE INDEX [IX_LM_AuditLogs_Appointment] ON [LM_AuditLogs] ([LeadershipAppointmentId], [TimestampUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261114082343_LeadershipAppointments', N'8.0.27');
GO

COMMIT;
GO

