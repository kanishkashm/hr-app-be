using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendancePeriodLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_period_locks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnlockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_period_locks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_period_locks_office_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "office_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_attendance_period_locks_users_LockedByUserId",
                        column: x => x.LockedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_attendance_period_locks_users_UnlockedByUserId",
                        column: x => x.UnlockedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_period_locks_BranchId",
                table: "attendance_period_locks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_period_locks_LockedByUserId",
                table: "attendance_period_locks",
                column: "LockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_period_locks_UnlockedByUserId",
                table: "attendance_period_locks",
                column: "UnlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_period_locks_Year_Month_BranchId",
                table: "attendance_period_locks",
                columns: new[] { "Year", "Month", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_period_locks_Year_Month_IsLocked",
                table: "attendance_period_locks",
                columns: new[] { "Year", "Month", "IsLocked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_period_locks");
        }
    }
}
