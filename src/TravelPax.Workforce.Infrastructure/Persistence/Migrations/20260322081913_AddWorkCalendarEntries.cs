using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkCalendarEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestType",
                table: "attendance_correction_requests",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "work_calendar_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CalendarDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DayType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsRecurringAnnual = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_calendar_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_calendar_entries_office_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "office_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_correction_requests_RequestType_Status_CreatedAt",
                table: "attendance_correction_requests",
                columns: new[] { "RequestType", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_work_calendar_entries_BranchId_CalendarDate_IsActive",
                table: "work_calendar_entries",
                columns: new[] { "BranchId", "CalendarDate", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_calendar_entries");

            migrationBuilder.DropIndex(
                name: "IX_attendance_correction_requests_RequestType_Status_CreatedAt",
                table: "attendance_correction_requests");

            migrationBuilder.DropColumn(
                name: "RequestType",
                table: "attendance_correction_requests");
        }
    }
}
