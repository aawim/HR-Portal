-- Restore the intended terminal HR approval transition for Early Return Request.
-- This changes configuration only; it does not approve or update any request.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypes WHERE RequestTypeID = 105 AND TypeName = 'Early Return Request')
    THROW 50000, 'Unexpected Early Return Request configuration.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestStates WHERE RequestStateID = 14 AND StateName = 'Pending HR Approval' AND IsFinalState = 0)
    THROW 50000, 'Unexpected HR approval state configuration.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestStates WHERE RequestStateID = 4 AND RTRIM(StateName) = 'Approved' AND IsFinalState = 1)
    THROW 50000, 'Unexpected approved state configuration.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions WITH (UPDLOCK, HOLDLOCK)
    WHERE RequestTypeID = 105 AND FromRequestStateID = 14 AND ToRequestStateID = 4)
    INSERT dbo.RequestTypesAllowedRequestStateTransitions (RequestTypeID, FromRequestStateID, ToRequestStateID)
    VALUES (105, 14, 4);
COMMIT;
