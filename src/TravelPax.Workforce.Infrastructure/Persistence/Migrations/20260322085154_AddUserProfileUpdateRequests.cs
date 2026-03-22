using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileUpdateRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "users",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_profile_update_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentDisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CurrentMobileNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CurrentEmergencyContactName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CurrentEmergencyContactPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RequestedDisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RequestedMobileNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RequestedEmergencyContactName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RequestedEmergencyContactPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewerComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profile_update_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_profile_update_requests_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_profile_update_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_update_requests_ReviewedByUserId",
                table: "user_profile_update_requests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_update_requests_Status_CreatedAt",
                table: "user_profile_update_requests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_update_requests_UserId_Status_CreatedAt",
                table: "user_profile_update_requests",
                columns: new[] { "UserId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profile_update_requests");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "users");
        }
    }
}
