using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class StaffChatGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CH_Conversations_OrganisationId_FirstUserId_SecondUserId",
                table: "CH_Conversations");

            migrationBuilder.AddColumn<bool>(
                name: "IsGroup",
                table: "CH_Conversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "CH_Conversations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CH_Events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    SubjectUserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CH_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CH_Events_CH_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "CH_Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Events_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Events_Users_SubjectUserId",
                        column: x => x.SubjectUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CH_Memberships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsManager = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true),
                    JoinedAfterMessageId = table.Column<long>(type: "bigint", nullable: false),
                    LastReadMessageId = table.Column<long>(type: "bigint", nullable: false),
                    LeftAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedByUserId = table.Column<int>(type: "int", nullable: true),
                    LeftAfterMessageId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CH_Memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CH_Memberships_CH_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "CH_Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Memberships_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Memberships_Users_RemovedByUserId",
                        column: x => x.RemovedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Memberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CH_Conversations_OrganisationId_FirstUserId_SecondUserId",
                table: "CH_Conversations",
                columns: new[] { "OrganisationId", "FirstUserId", "SecondUserId" },
                unique: true,
                filter: "[IsGroup] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Events_ActorUserId",
                table: "CH_Events",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Events_ConversationId_Id",
                table: "CH_Events",
                columns: new[] { "ConversationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CH_Events_SubjectUserId",
                table: "CH_Events",
                column: "SubjectUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Memberships_AddedByUserId",
                table: "CH_Memberships",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Memberships_ConversationId_UserId",
                table: "CH_Memberships",
                columns: new[] { "ConversationId", "UserId" },
                unique: true,
                filter: "[LeftAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Memberships_RemovedByUserId",
                table: "CH_Memberships",
                column: "RemovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Memberships_UserId_ConversationId_LeftAtUtc",
                table: "CH_Memberships",
                columns: new[] { "UserId", "ConversationId", "LeftAtUtc" });
            // Preserve existing direct-chat access and unread cursors. The original
            // sender who created each legacy conversation is unknown, so AddedBy stays null.
            migrationBuilder.Sql("""
                INSERT dbo.CH_Memberships(ConversationId,UserId,IsManager,JoinedAtUtc,AddedByUserId,JoinedAfterMessageId,LastReadMessageId,LeftAtUtc,RemovedByUserId,LeftAfterMessageId)
                SELECT c.Id,c.FirstUserId,0,c.CreatedAtUtc,NULL,0,c.FirstReadMessageId,NULL,NULL,NULL
                FROM dbo.CH_Conversations c
                WHERE c.IsGroup=0 AND NOT EXISTS(SELECT 1 FROM dbo.CH_Memberships m WHERE m.ConversationId=c.Id AND m.UserId=c.FirstUserId);
                INSERT dbo.CH_Memberships(ConversationId,UserId,IsManager,JoinedAtUtc,AddedByUserId,JoinedAfterMessageId,LastReadMessageId,LeftAtUtc,RemovedByUserId,LeftAfterMessageId)
                SELECT c.Id,c.SecondUserId,0,c.CreatedAtUtc,NULL,0,c.SecondReadMessageId,NULL,NULL,NULL
                FROM dbo.CH_Conversations c
                WHERE c.IsGroup=0 AND NOT EXISTS(SELECT 1 FROM dbo.CH_Memberships m WHERE m.ConversationId=c.Id AND m.UserId=c.SecondUserId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Group messages and membership history must be retained. Restore the reviewed pre-upgrade backup to roll back.");
        }
    }
}
