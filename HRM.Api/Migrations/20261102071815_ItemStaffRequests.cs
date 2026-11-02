using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class ItemStaffRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IM_ItemStaffRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemUnitId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IM_ItemStaffRequests", x => x.RequestId);
                    table.CheckConstraint("CK_IM_ItemStaffRequests_Values", "[Quantity] > 0 AND [Purpose] IN ('Job related','Common') AND [Kind] IN ('Issue','Requisition')");
                    table.ForeignKey(
                        name: "FK_IM_ItemStaffRequests_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemStaffRequests_IM_ItemUnits_ItemUnitId",
                        column: x => x.ItemUnitId,
                        principalTable: "IM_ItemUnits",
                        principalColumn: "ItemUnitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemStaffRequests_IM_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "IM_Items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemStaffRequests_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemStaffRequests_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemStaffRequests_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemStaffRequests_IndividualId",
                table: "IM_ItemStaffRequests",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemStaffRequests_ItemId",
                table: "IM_ItemStaffRequests",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemStaffRequests_ItemUnitId",
                table: "IM_ItemStaffRequests",
                column: "ItemUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemStaffRequests_JobId",
                table: "IM_ItemStaffRequests",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemStaffRequests_OrganisationId_IndividualId_CreatedAtUtc",
                table: "IM_ItemStaffRequests",
                columns: new[] { "OrganisationId", "IndividualId", "CreatedAtUtc" });
            migrationBuilder.Sql("""
                IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=N'Item Requisition Request')
                BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                    VALUES(N'Item Requisitions',N'IMR',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    DECLARE @RequisitionSequence int=CONVERT(int,SCOPE_IDENTITY());
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                    VALUES(N'Item Requisition Request',@RequisitionSequence,N'/online/item-requests',1);
                END;
                IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=N'Staff Item Issue Request')
                BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                    VALUES(N'Staff Item Issues',N'IMI',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    DECLARE @IssueSequence int=CONVERT(int,SCOPE_IDENTITY());
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                    VALUES(N'Staff Item Issue Request',@IssueSequence,N'/online/item-requests',1);
                END;
                INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
                SELECT t.RequestTypeID, f.RequestStateID, s.RequestStateID
                FROM dbo.RequestTypes t CROSS JOIN dbo.RequestStates f CROSS JOIN dbo.RequestStates s
                WHERE t.TypeName IN (N'Item Requisition Request',N'Staff Item Issue Request')
                  AND (f.RequestStateID=1 OR f.IsProcessingState=1 AND f.IsFinalState=0)
                  AND (s.IsProcessingState=1 AND s.IsFinalState=0 OR s.RequestStateID IN (4,5,6))
                  AND f.RequestStateID<>s.RequestStateID
                  AND NOT EXISTS(SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x
                      WHERE x.RequestTypeID=t.RequestTypeID AND x.FromRequestStateID=f.RequestStateID AND x.ToRequestStateID=s.RequestStateID);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Item issue and requisition history must be retained.");
        }
    }
}
