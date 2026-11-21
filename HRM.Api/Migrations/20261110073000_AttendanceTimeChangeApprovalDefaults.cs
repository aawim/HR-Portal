using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261110073000_AttendanceTimeChangeApprovalDefaults")]
public sealed class AttendanceTimeChangeApprovalDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            SET XACT_ABORT ON;

            -- Time changes are submitted from state 1, approved from state 3,
            -- and may be rejected or cancelled while pending.
            INSERT dbo.RequestTypesAllowedRequestStateTransitions
                (RequestTypeID, FromRequestStateID, ToRequestStateID)
            SELECT requestType.RequestTypeID, transition.FromState, transition.ToState
            FROM dbo.RequestTypes requestType
            CROSS JOIN (VALUES (1,3),(1,6),(3,4),(3,5),(3,6)) transition(FromState,ToState)
            WHERE requestType.TypeName IN (N'Late Check In Request', N'Early Check Out Request')
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.RequestTypesAllowedRequestStateTransitions existing
                  WHERE existing.RequestTypeID = requestType.RequestTypeID
                    AND existing.FromRequestStateID = transition.FromState
                    AND existing.ToRequestStateID = transition.ToState
              );

            -- A global supervisor route is the safe fallback. Tenant/job-type routes
            -- remain more specific and therefore continue to win during resolution.
            DECLARE @RequestTypeID int, @DefinitionID int, @StageID int, @Version int;
            DECLARE time_change_types CURSOR LOCAL FAST_FORWARD FOR
                SELECT RequestTypeID
                FROM dbo.RequestTypes
                WHERE TypeName IN (N'Late Check In Request', N'Early Check Out Request');

            OPEN time_change_types;
            FETCH NEXT FROM time_change_types INTO @RequestTypeID;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.WF_WorkflowDefinitions definition
                    WHERE definition.RequestTypeID = @RequestTypeID
                      AND definition.OrganisationBusinessEntityID IS NULL
                      AND definition.LeaveTypeId IS NULL
                      AND definition.LeaveRequestKind IS NULL
                      AND definition.IsActive = 1
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM dbo.WF_WorkflowJobTypes mapping
                          WHERE mapping.WorkflowDefinitionID = definition.WorkflowDefinitionID
                      )
                )
                BEGIN
                    SELECT @Version = ISNULL(MAX(VersionNumber), 0) + 1
                    FROM dbo.WF_WorkflowDefinitions
                    WHERE RequestTypeID = @RequestTypeID
                      AND OrganisationBusinessEntityID IS NULL;

                    INSERT dbo.WF_WorkflowDefinitions
                        (RequestTypeID, OrganisationBusinessEntityID, VersionNumber, Name,
                         IsActive, CreatedAt, LeaveTypeId, LeaveRequestKind)
                    VALUES
                        (@RequestTypeID, NULL, @Version, N'Time change: supervisor approval',
                         1, SYSUTCDATETIME(), NULL, NULL);
                    SET @DefinitionID = SCOPE_IDENTITY();

                    INSERT dbo.WF_WorkflowStages
                        (WorkflowDefinitionID, StageOrder, StageName, RequestStateID,
                         ApprovalMode, MinimumApprovals, IsActive)
                    VALUES
                        (@DefinitionID, 1, N'Supervisor approval', 3, N'Any', NULL, 1);
                    SET @StageID = SCOPE_IDENTITY();

                    -- Reference 0 means any eligible supervisor resolved from the
                    -- applicant's team/department hierarchy; the applicant is excluded.
                    INSERT dbo.WF_StageApprovers
                        (WorkflowStageID, ApproverType, ApproverReferenceID,
                         SequenceNumber, IsRequired)
                    VALUES (@StageID, N'Supervisor', 0, 1, 1);
                END;

                FETCH NEXT FROM time_change_types INTO @RequestTypeID;
            END;
            CLOSE time_change_types;
            DEALLOCATE time_change_types;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Never remove a definition that has already routed a real request.
            DELETE approver
            FROM dbo.WF_StageApprovers approver
            JOIN dbo.WF_WorkflowStages stage ON stage.WorkflowStageID = approver.WorkflowStageID
            JOIN dbo.WF_WorkflowDefinitions definition ON definition.WorkflowDefinitionID = stage.WorkflowDefinitionID
            WHERE definition.Name = N'Time change: supervisor approval'
              AND definition.OrganisationBusinessEntityID IS NULL
              AND NOT EXISTS
                  (SELECT 1 FROM dbo.WF_RequestInstances instance WHERE instance.WorkflowDefinitionID = definition.WorkflowDefinitionID);

            DELETE stage
            FROM dbo.WF_WorkflowStages stage
            JOIN dbo.WF_WorkflowDefinitions definition ON definition.WorkflowDefinitionID = stage.WorkflowDefinitionID
            WHERE definition.Name = N'Time change: supervisor approval'
              AND definition.OrganisationBusinessEntityID IS NULL
              AND NOT EXISTS
                  (SELECT 1 FROM dbo.WF_RequestInstances instance WHERE instance.WorkflowDefinitionID = definition.WorkflowDefinitionID);

            DELETE definition
            FROM dbo.WF_WorkflowDefinitions definition
            WHERE definition.Name = N'Time change: supervisor approval'
              AND definition.OrganisationBusinessEntityID IS NULL
              AND NOT EXISTS
                  (SELECT 1 FROM dbo.WF_RequestInstances instance WHERE instance.WorkflowDefinitionID = definition.WorkflowDefinitionID);
            """);
    }
}
