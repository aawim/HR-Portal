-- Independent request types; existing Requests and workflow instances are not rewritten.
IF EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE RequestTypeID = 105 AND TypeName <> 'Early Return Request')
    THROW 51000, 'Request Type 105 is already used. Resolve the ID collision before deployment.', 1;
IF EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE RequestTypeID = 106 AND TypeName <> 'Leave Extension Request')
    THROW 51000, 'Request Type 106 is already used. Resolve the ID collision before deployment.', 1;

DECLARE @EarlySequence int, @ExtensionSequence int;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE RequestTypeID = 105)
BEGIN
    INSERT dbo.SequenceNumberTypes (TypeName, Prefix, FormatString, IsResetAnnually, StartingNumber, NextNumber)
    VALUES ('Early Return Requests', 'ER', '{PREFIX}-{YEAR}-{NUMBER}', 0, 1, 1);
    SET @EarlySequence = SCOPE_IDENTITY();
    SET IDENTITY_INSERT dbo.RequestTypes ON;
    INSERT dbo.RequestTypes (RequestTypeID, TypeName, SequenceNumberTypeID, ViewURL, IsSystemType)
    VALUES (105, 'Early Return Request', @EarlySequence, '/online/leaves/leave-change-request', 1);
    SET IDENTITY_INSERT dbo.RequestTypes OFF;
END;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE RequestTypeID = 106)
BEGIN
    INSERT dbo.SequenceNumberTypes (TypeName, Prefix, FormatString, IsResetAnnually, StartingNumber, NextNumber)
    VALUES ('Leave Extension Requests', 'LEX', '{PREFIX}-{YEAR}-{NUMBER}', 0, 1, 1);
    SET @ExtensionSequence = SCOPE_IDENTITY();
    SET IDENTITY_INSERT dbo.RequestTypes ON;
    INSERT dbo.RequestTypes (RequestTypeID, TypeName, SequenceNumberTypeID, ViewURL, IsSystemType)
    VALUES (106, 'Leave Extension Request', @ExtensionSequence, '/online/leaves/leave-change-request', 1);
    SET IDENTITY_INSERT dbo.RequestTypes OFF;
END;
