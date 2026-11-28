using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Intranet.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntranetOutboxMessages",
                columns: table => new
                {
                    OutboxMessageID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetOutboxMessages", x => x.OutboxMessageID);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostCategories",
                columns: table => new
                {
                    IntranetPostCategoryID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationBusinessEntityID = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostCategories", x => x.IntranetPostCategoryID);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPosts",
                columns: table => new
                {
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationBusinessEntityID = table.Column<long>(type: "bigint", nullable: false),
                    PostType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImageID = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    AllowComments = table.Column<bool>(type: "bit", nullable: false),
                    AllowReactions = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAcknowledgement = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPosts", x => x.IntranetPostID);
                });

            migrationBuilder.CreateTable(
                name: "IntranetNewJoinerProfiles",
                columns: table => new
                {
                    IntranetNewJoinerProfileID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    IndividualID = table.Column<long>(type: "bigint", nullable: false),
                    JobID = table.Column<long>(type: "bigint", nullable: true),
                    JoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartmentBusinessEntityID = table.Column<long>(type: "bigint", nullable: true),
                    WelcomeMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Introduction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowProfilePhoto = table.Column<bool>(type: "bit", nullable: false),
                    ProfilePhotoReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetNewJoinerProfiles", x => x.IntranetNewJoinerProfileID);
                    table.ForeignKey(
                        name: "FK_IntranetNewJoinerProfiles_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostAcknowledgements",
                columns: table => new
                {
                    IntranetPostAcknowledgementID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    IndividualID = table.Column<long>(type: "bigint", nullable: false),
                    AcknowledgedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostAcknowledgements", x => x.IntranetPostAcknowledgementID);
                    table.ForeignKey(
                        name: "FK_IntranetPostAcknowledgements_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostAttachments",
                columns: table => new
                {
                    IntranetPostAttachmentID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    FileReferenceID = table.Column<long>(type: "bigint", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    UploadedBy = table.Column<long>(type: "bigint", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostAttachments", x => x.IntranetPostAttachmentID);
                    table.ForeignKey(
                        name: "FK_IntranetPostAttachments_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostAudiences",
                columns: table => new
                {
                    IntranetPostAudienceID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    AudienceType = table.Column<int>(type: "int", nullable: false),
                    TargetEntityID = table.Column<long>(type: "bigint", nullable: true),
                    IsIncluded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostAudiences", x => x.IntranetPostAudienceID);
                    table.ForeignKey(
                        name: "FK_IntranetPostAudiences_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostCategoryAssignments",
                columns: table => new
                {
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    IntranetPostCategoryID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostCategoryAssignments", x => new { x.IntranetPostID, x.IntranetPostCategoryID });
                    table.ForeignKey(
                        name: "FK_IntranetPostCategoryAssignments_IntranetPostCategories_IntranetPostCategoryID",
                        column: x => x.IntranetPostCategoryID,
                        principalTable: "IntranetPostCategories",
                        principalColumn: "IntranetPostCategoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntranetPostCategoryAssignments_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostComments",
                columns: table => new
                {
                    IntranetPostCommentID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    ParentCommentID = table.Column<long>(type: "bigint", nullable: true),
                    AuthorIndividualID = table.Column<long>(type: "bigint", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostComments", x => x.IntranetPostCommentID);
                    table.ForeignKey(
                        name: "FK_IntranetPostComments_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostReactions",
                columns: table => new
                {
                    IntranetPostReactionID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    IndividualID = table.Column<long>(type: "bigint", nullable: false),
                    ReactionType = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostReactions", x => x.IntranetPostReactionID);
                    table.ForeignKey(
                        name: "FK_IntranetPostReactions_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPostReadStates",
                columns: table => new
                {
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    IndividualID = table.Column<long>(type: "bigint", nullable: false),
                    FirstViewedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastViewedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPostReadStates", x => new { x.IntranetPostID, x.IndividualID });
                    table.ForeignKey(
                        name: "FK_IntranetPostReadStates_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntranetPublicationHistory",
                columns: table => new
                {
                    IntranetPublicationHistoryID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntranetPostID = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorID = table.Column<long>(type: "bigint", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntranetPublicationHistory", x => x.IntranetPublicationHistoryID);
                    table.ForeignKey(
                        name: "FK_IntranetPublicationHistory_IntranetPosts_IntranetPostID",
                        column: x => x.IntranetPostID,
                        principalTable: "IntranetPosts",
                        principalColumn: "IntranetPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntranetNewJoinerProfiles_IntranetPostID",
                table: "IntranetNewJoinerProfiles",
                column: "IntranetPostID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntranetOutboxMessages_Status",
                table: "IntranetOutboxMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPostAcknowledgements_IntranetPostID",
                table: "IntranetPostAcknowledgements",
                column: "IntranetPostID");

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPostAttachments_IntranetPostID",
                table: "IntranetPostAttachments",
                column: "IntranetPostID");

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPostAudiences_IntranetPostID_AudienceType",
                table: "IntranetPostAudiences",
                columns: new[] { "IntranetPostID", "AudienceType" });

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPostCategoryAssignments_IntranetPostCategoryID",
                table: "IntranetPostCategoryAssignments",
                column: "IntranetPostCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPostComments_IntranetPostID",
                table: "IntranetPostComments",
                column: "IntranetPostID");

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPostReactions_IntranetPostID",
                table: "IntranetPostReactions",
                column: "IntranetPostID");

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPosts_OrganisationBusinessEntityID_Status",
                table: "IntranetPosts",
                columns: new[] { "OrganisationBusinessEntityID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPosts_PublishedAt",
                table: "IntranetPosts",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntranetPublicationHistory_IntranetPostID",
                table: "IntranetPublicationHistory",
                column: "IntranetPostID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntranetNewJoinerProfiles");

            migrationBuilder.DropTable(
                name: "IntranetOutboxMessages");

            migrationBuilder.DropTable(
                name: "IntranetPostAcknowledgements");

            migrationBuilder.DropTable(
                name: "IntranetPostAttachments");

            migrationBuilder.DropTable(
                name: "IntranetPostAudiences");

            migrationBuilder.DropTable(
                name: "IntranetPostCategoryAssignments");

            migrationBuilder.DropTable(
                name: "IntranetPostComments");

            migrationBuilder.DropTable(
                name: "IntranetPostReactions");

            migrationBuilder.DropTable(
                name: "IntranetPostReadStates");

            migrationBuilder.DropTable(
                name: "IntranetPublicationHistory");

            migrationBuilder.DropTable(
                name: "IntranetPostCategories");

            migrationBuilder.DropTable(
                name: "IntranetPosts");
        }
    }
}
