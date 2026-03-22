using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveBalancesAndPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leave_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LeaveType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AllocatedDays = table.Column<int>(type: "integer", nullable: false),
                    CarryForwardDays = table.Column<int>(type: "integer", nullable: false),
                    ManualAdjustmentDays = table.Column<int>(type: "integer", nullable: false),
                    UsedDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leave_balances_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmploymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnnualAllocationDays = table.Column<int>(type: "integer", nullable: false),
                    MaxCarryForwardDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leave_policies_office_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "office_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_leave_balances_UserId_Year_LeaveType",
                table: "leave_balances",
                columns: new[] { "UserId", "Year", "LeaveType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_policies_BranchId",
                table: "leave_policies",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_policies_LeaveType_EmploymentType_BranchId",
                table: "leave_policies",
                columns: new[] { "LeaveType", "EmploymentType", "BranchId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leave_balances");

            migrationBuilder.DropTable(
                name: "leave_policies");
        }
    }
}
