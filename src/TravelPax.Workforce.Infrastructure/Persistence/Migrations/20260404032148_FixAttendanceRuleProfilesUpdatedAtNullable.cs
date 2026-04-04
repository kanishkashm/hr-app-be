using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAttendanceRuleProfilesUpdatedAtNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DirectorReviewedAt",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DirectorReviewedByUserId",
                table: "leave_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorReviewerNote",
                table: "leave_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HrReviewedAt",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HrReviewedByUserId",
                table: "leave_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HrReviewerNote",
                table: "leave_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_DirectorReviewedByUserId",
                table: "leave_requests",
                column: "DirectorReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_HrReviewedByUserId",
                table: "leave_requests",
                column: "HrReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_users_DirectorReviewedByUserId",
                table: "leave_requests",
                column: "DirectorReviewedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_leave_requests_users_HrReviewedByUserId",
                table: "leave_requests",
                column: "HrReviewedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_users_DirectorReviewedByUserId",
                table: "leave_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_leave_requests_users_HrReviewedByUserId",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "IX_leave_requests_DirectorReviewedByUserId",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "IX_leave_requests_HrReviewedByUserId",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "DirectorReviewedAt",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "DirectorReviewedByUserId",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "DirectorReviewerNote",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "HrReviewedAt",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "HrReviewedByUserId",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "HrReviewerNote",
                table: "leave_requests");
        }
    }
}
