using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations;

[Migration("20261113093000_RemoveLegacyShift")]
public sealed class RemoveLegacyShift : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DECLARE @workPlanningProviderId int =
    (SELECT TOP (1) PlanningProviderID FROM dbo.PlanningProviders
     WHERE Code = N'WORK_PLANNING' AND IsActive = 1);

IF @workPlanningProviderId IS NULL
    THROW 51000, 'An active WORK_PLANNING provider is required before retiring LEGACY_SHIFT.', 1;

UPDATE wp
SET PlanningProviderID = @workPlanningProviderId
FROM dbo.WorkPlans wp
INNER JOIN dbo.PlanningProviders p ON p.PlanningProviderID = wp.PlanningProviderID
WHERE p.Code = N'LEGACY_SHIFT';

UPDATE s
SET PlanningProviderID = @workPlanningProviderId
FROM dbo.OrganisationWorkPlanningSettings s
INNER JOIN dbo.PlanningProviders p ON p.PlanningProviderID = s.PlanningProviderID
WHERE p.Code = N'LEGACY_SHIFT';

IF OBJECT_ID(N'dbo.ST_StaffTransferWorkDetails', N'U') IS NOT NULL
BEGIN
    UPDATE d
    SET CurrentPlanningProviderId = @workPlanningProviderId
    FROM dbo.ST_StaffTransferWorkDetails d
    INNER JOIN dbo.PlanningProviders p ON p.PlanningProviderID = d.CurrentPlanningProviderId
    WHERE p.Code = N'LEGACY_SHIFT';

    UPDATE d
    SET ProposedPlanningProviderId = @workPlanningProviderId
    FROM dbo.ST_StaffTransferWorkDetails d
    INNER JOIN dbo.PlanningProviders p ON p.PlanningProviderID = d.ProposedPlanningProviderId
    WHERE p.Code = N'LEGACY_SHIFT';
END;

DELETE FROM dbo.PlanningProviders WHERE Code = N'LEGACY_SHIFT';
");

        migrationBuilder.DropTable(name: "ShiftScheduleDays");
        migrationBuilder.DropTable(name: "ShiftSchedules");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ShiftSchedules",
            columns: table => new
            {
                ScheduleID = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShiftSchedules", x => x.ScheduleID);
                table.ForeignKey(
                    name: "FK_ShiftSchedules_Schedules",
                    column: x => x.ScheduleID,
                    principalTable: "Schedules",
                    principalColumn: "ScheduleID");
            });

        migrationBuilder.CreateTable(
            name: "ShiftScheduleDays",
            columns: table => new
            {
                ShiftScheduleDayID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ScheduleID = table.Column<int>(type: "int", nullable: false),
                DayNumber = table.Column<int>(type: "int", nullable: true),
                WorkShiftID = table.Column<int>(type: "int", nullable: true),
                SpecialDayTypeID = table.Column<int>(type: "int", nullable: false),
                IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                OperationLogID = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateTime>(type: "datetime", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShiftScheduleDays", x => x.ShiftScheduleDayID);
                table.ForeignKey(name: "FK_ShiftScheduleDays_OperationLogs", column: x => x.OperationLogID, principalTable: "OperationLogs", principalColumn: "OperationLogID");
                table.ForeignKey(name: "FK_ShiftScheduleDays_ShiftSchedules", column: x => x.ScheduleID, principalTable: "ShiftSchedules", principalColumn: "ScheduleID");
                table.ForeignKey(name: "FK_ShiftScheduleDays_SpecialDayTypes", column: x => x.SpecialDayTypeID, principalTable: "SpecialDayTypes", principalColumn: "SpecialDayTypeID");
                table.ForeignKey(name: "FK_ShiftScheduleDays_WorkShifts", column: x => x.WorkShiftID, principalTable: "WorkShifts", principalColumn: "WorkShiftID");
            });
    }
}
