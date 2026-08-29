using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planara.Privacy.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GivenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                    table.CheckConstraint("CK_UserConsents_Subject", "\"RegistrationId\" IS NOT NULL OR \"UserId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_UserConsents_ConsentVersions_ConsentVersionId",
                        column: x => x.ConsentVersionId,
                        principalTable: "ConsentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentVersions_Type_Status_EffectiveAt",
                table: "ConsentVersions",
                columns: new[] { "Type", "Status", "EffectiveAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentVersions_Type_Version",
                table: "ConsentVersions",
                columns: new[] { "Type", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CreatedAt",
                table: "OutboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_LockedUntil",
                table: "OutboxMessages",
                column: "LockedUntil");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                table: "OutboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TopicKey",
                table: "OutboxMessages",
                column: "TopicKey");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Type",
                table: "OutboxMessages",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_ConsentVersionId",
                table: "UserConsents",
                column: "ConsentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_ExpiresAt",
                table: "UserConsents",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_GrantRequestId",
                table: "UserConsents",
                column: "GrantRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_RegistrationId_ConsentVersionId",
                table: "UserConsents",
                columns: new[] { "RegistrationId", "ConsentVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_RevokedAt",
                table: "UserConsents",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_UserId_ConsentVersionId",
                table: "UserConsents",
                columns: new[] { "UserId", "ConsentVersionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropTable(
                name: "ConsentVersions");
        }
    }
}
