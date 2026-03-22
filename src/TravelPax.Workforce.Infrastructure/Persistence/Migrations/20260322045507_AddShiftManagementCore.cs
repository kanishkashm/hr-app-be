using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftManagementCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShiftType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    FlexMinutes = table.Column<int>(type: "integer", nullable: false),
                    GraceMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinHalfDayMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinPresentMinutes = table.Column<int>(type: "integer", nullable: false),
                    OvertimeAfterMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shift_definitions_office_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "office_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "employee_shift_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_shift_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_shift_assignments_shift_definitions_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shift_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_shift_assignments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_assignment_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Team = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_assignment_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shift_assignment_rules_office_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "office_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_shift_assignment_rules_shift_definitions_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shift_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shift_overrides_shift_definitions_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shift_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_overrides_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_shift_assignments_ShiftId",
                table: "employee_shift_assignments",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_shift_assignments_UserId_EffectiveFrom_EffectiveTo~",
                table: "employee_shift_assignments",
                columns: new[] { "UserId", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignment_rules_BranchId_Department_Team_EffectiveFr~",
                table: "shift_assignment_rules",
                columns: new[] { "BranchId", "Department", "Team", "EffectiveFrom", "EffectiveTo", "Priority", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_shift_assignment_rules_ShiftId",
                table: "shift_assignment_rules",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_definitions_BranchId",
                table: "shift_definitions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_definitions_Code_BranchId",
                table: "shift_definitions",
                columns: new[] { "Code", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shift_overrides_ShiftId",
                table: "shift_overrides",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_overrides_UserId_Date",
                table: "shift_overrides",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_shift_assignments");

            migrationBuilder.DropTable(
                name: "shift_assignment_rules");

            migrationBuilder.DropTable(
                name: "shift_overrides");

            migrationBuilder.DropTable(
                name: "shift_definitions");
        }
    }
}
