-- Ensure complete allowed request state transitions for:
--   Supervisor Assignment Request (TypeName = 'Supervisor Assignment Request')
--   Supervisor Removal Request    (TypeName = 'Supervisor Removal Request')
-- Covers submission, multi-stage approvals, direct approval, rejection, and cancellation.

SET NOCOUNT ON;

DECLARE @SupervisorTypes TABLE (RequestTypeID INT PRIMARY KEY);
INSERT INTO @SupervisorTypes (RequestTypeID)
SELECT RequestTypeID
FROM dbo.RequestTypes
WHERE TypeName IN ('Supervisor Assignment Request', 'Supervisor Removal Request');

DECLARE @PendingStates TABLE (StateID INT PRIMARY KEY);
INSERT INTO @PendingStates (StateID)
SELECT RequestStateID
FROM dbo.RequestStates
WHERE RequestStateID IN (2, 3, 9, 11, 12, 13, 14, 15);

-- 1. From Incomplete (1) -> any Pending state, Approved (4), or Cancelled (5)
INSERT INTO dbo.RequestTypesAllowedRequestStateTransitions (RequestTypeID, FromRequestStateID, ToRequestStateID)
SELECT st.RequestTypeID, 1, target.StateID
FROM @SupervisorTypes st
CROSS JOIN (
    SELECT StateID FROM @PendingStates
    UNION SELECT RequestStateID FROM dbo.RequestStates WHERE RequestStateID IN (4, 5)
) target
WHERE EXISTS (SELECT 1 FROM dbo.RequestStates WHERE RequestStateID = 1)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions existing
      WHERE existing.RequestTypeID = st.RequestTypeID
        AND existing.FromRequestStateID = 1
        AND existing.ToRequestStateID = target.StateID
  );

-- 2. From any Pending state -> any other Pending state, Approved (4), Cancelled (5), or Rejected (6)
INSERT INTO dbo.RequestTypesAllowedRequestStateTransitions (RequestTypeID, FromRequestStateID, ToRequestStateID)
SELECT st.RequestTypeID, src.StateID, dst.StateID
FROM @SupervisorTypes st
CROSS JOIN @PendingStates src
CROSS JOIN (
    SELECT StateID FROM @PendingStates
    UNION SELECT RequestStateID FROM dbo.RequestStates WHERE RequestStateID IN (4, 5, 6)
) dst
WHERE src.StateID <> dst.StateID
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions existing
      WHERE existing.RequestTypeID = st.RequestTypeID
        AND existing.FromRequestStateID = src.StateID
        AND existing.ToRequestStateID = dst.StateID
  );

SELECT rt.RequestTypeID, rt.TypeName, COUNT(*) AS AllowedTransitions
FROM dbo.RequestTypesAllowedRequestStateTransitions t
JOIN dbo.RequestTypes rt ON rt.RequestTypeID = t.RequestTypeID
WHERE rt.RequestTypeID IN (SELECT RequestTypeID FROM @SupervisorTypes)
GROUP BY rt.RequestTypeID, rt.TypeName
ORDER BY rt.RequestTypeID;
GO
