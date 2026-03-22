using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceRuleProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_rule_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LateGraceMinutes = table.Column<int>(type: "integer", nullable: true),
                    HalfDayThresholdMinutes = table.Column<int>(type: "integer", nullable: true),
                    MinPresentMinutes = table.Column<int>(type: "integer", nullable: true),
                    OvertimeThresholdMinutes = table.Column<int>(type: "integer", nullable: true),
                    EarlyOutGraceMinutes = table.Column<int>(type: "integer", nullable: true),
                    ShortLeaveDeductionMinutes = table.Column<int>(type: "integer", nullable: true),
                    EnableMissedPunchDetection = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_rule_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_rule_profiles_office_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "office_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_attendance_rule_profiles_shift_definitions_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shift_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_rule_profiles_BranchId",
                table: "attendance_rule_profiles",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_rule_profiles_ScopeType_BranchId_ShiftId_Priority~",
                table: "attendance_rule_profiles",
                columns: new[] { "ScopeType", "BranchId", "ShiftId", "Priority", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_rule_profiles_ShiftId",
                table: "attendance_rule_profiles",
                column: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_rule_profiles");
        }
    }
}
