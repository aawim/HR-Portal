-- Import existing type-specific policy requirements into the scoped handover engine.
-- Legacy columns remain historical metadata; the dedicated rule editor is the only writer going forward.
IF EXISTS(SELECT 1 FROM dbo.LT_LeavePolicies WHERE IsActive=1 AND RequireWorkHandover=1 AND (WorkHandoverAfterDays<0 OR WorkHandoverAfterDays>3650))
 THROW 51000,'An existing handover threshold is invalid. Review it before installing scoped rules.',1;
DECLARE @policy int,@org int,@type int,@required bit,@after int,@from date,@to date,@name nvarchar(200),@rule int,@audit int;
DECLARE policy_cursor CURSOR LOCAL FAST_FORWARD FOR
 SELECT p.LeavePolicyID,p.OrganisationID,p.LeaveTypeID,p.RequireWorkHandover,p.WorkHandoverAfterDays,CONVERT(date,p.EffectiveFrom),CONVERT(date,p.EffectiveTo),LEFT(t.Name+N' organisation default',200)
 FROM dbo.LT_LeavePolicies p JOIN dbo.LT_LeaveTypes t ON t.LeaveTypeID=p.LeaveTypeID WHERE p.IsActive=1;
OPEN policy_cursor;FETCH NEXT FROM policy_cursor INTO @policy,@org,@type,@required,@after,@from,@to,@name;
WHILE @@FETCH_STATUS=0
BEGIN
 INSERT dbo.OperationLogs(OperationLogActionID,CreatedByContextID,CreatedDate,UpdatedDate,CreatedByIP,UpdatedByIP,Remarks,UpdatedContextID)
 VALUES(160,1,SYSUTCDATETIME(),SYSUTCDATETIME(),'','',CONCAT('Imported scoped handover rule from leave policy ',@policy,' for organisation ',@org,'. Existing leave and approval records unchanged.'),1);
 SET @audit=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.LH_LeaveHandoverRules(OrganisationId,Name,Scope,OrganisationStructureId,TeamId,JobTypeId,AllLeaveTypes,Mode,AfterDays,Priority,EffectiveFrom,EffectiveTo,IsActive,Version,OperationLogId)
 VALUES(@org,@name,'Organisation',NULL,NULL,NULL,0,CASE WHEN @required=1 THEN 'Required' ELSE 'Exempt' END,CASE WHEN @required=1 THEN @after ELSE 0 END,0,@from,@to,1,1,@audit);
 SET @rule=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.LH_LeaveHandoverRuleTypes(RuleId,LeaveTypeId,IsActive) VALUES(@rule,@type,1);
 -- Reviewed compatibility repair: NCIT's local annual copy uses the same annual request and reason set.
 -- Do not infer relationships for other names or override a separately configured local policy.
 IF @org=3 AND @type=1
 AND EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=1 AND Name=N'Annual Leave' AND IsGlobal=1 AND RequestTypeID=34)
 AND EXISTS(SELECT 1 FROM dbo.LT_LeaveTypes WHERE LeaveTypeID=66 AND OrganisationID=3 AND Name=N'Annual Leave' AND RequestTypeID=34)
 AND NOT EXISTS(SELECT 1 FROM dbo.LT_LeavePolicies WHERE OrganisationID=3 AND LeaveTypeID=66 AND IsActive=1 AND (EffectiveTo IS NULL OR EffectiveTo>=@from) AND (@to IS NULL OR EffectiveFrom<=@to))
  INSERT dbo.LH_LeaveHandoverRuleTypes(RuleId,LeaveTypeId,IsActive) VALUES(@rule,66,1);
 FETCH NEXT FROM policy_cursor INTO @policy,@org,@type,@required,@after,@from,@to,@name;
END;
CLOSE policy_cursor;DEALLOCATE policy_cursor;
