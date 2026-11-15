SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WITH (UPDLOCK,HOLDLOCK) WHERE RoleKey='Leadership.Appointment.Manage')
BEGIN
 INSERT dbo.Roles(RoleKey,Name,Description,ContextID,IsSystemRole)
 VALUES('Leadership.Appointment.Manage','Leadership appointment administration','Manage leadership appointments, scoped authority, delegations and attendance policies. Must be explicitly assigned.',1,1);
END;
COMMIT;
