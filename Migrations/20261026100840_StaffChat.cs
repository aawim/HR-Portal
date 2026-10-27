using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class StaffChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CH_Conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    FirstUserId = table.Column<int>(type: "int", nullable: false),
                    SecondUserId = table.Column<int>(type: "int", nullable: false),
                    FirstReadMessageId = table.Column<long>(type: "bigint", nullable: false),
                    SecondReadMessageId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CH_Conversations", x => x.Id);
                    table.CheckConstraint("CK_CH_Conversations_Participants", "[FirstUserId] < [SecondUserId]");
                    table.ForeignKey(
                        name: "FK_CH_Conversations_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Conversations_Users_FirstUserId",
                        column: x => x.FirstUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Conversations_Users_SecondUserId",
                        column: x => x.SecondUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CH_Messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    ClientMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CH_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CH_Messages_CH_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "CH_Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Messages_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CH_Messages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CH_Conversations_FirstUserId",
                table: "CH_Conversations",
                column: "FirstUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Conversations_OrganisationId_FirstUserId_SecondUserId",
                table: "CH_Conversations",
                columns: new[] { "OrganisationId", "FirstUserId", "SecondUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CH_Conversations_OrganisationId_UpdatedAtUtc",
                table: "CH_Conversations",
                columns: new[] { "OrganisationId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CH_Conversations_SecondUserId",
                table: "CH_Conversations",
                column: "SecondUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Messages_ConversationId_Id",
                table: "CH_Messages",
                columns: new[] { "ConversationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CH_Messages_RequestId",
                table: "CH_Messages",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CH_Messages_SenderUserId_ClientMessageId",
                table: "CH_Messages",
                columns: new[] { "SenderUserId", "ClientMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CH_Messages_SenderUserId_SentAtUtc",
                table: "CH_Messages",
                columns: new[] { "SenderUserId", "SentAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Chat history must be preserved. Restore the reviewed pre-upgrade backup to roll back.");
        }
    }
}
