using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPeriodFinalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_period_finalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinalizedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReopenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReopenedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_period_finalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payroll_period_finalizations_office_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "office_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payroll_period_finalizations_users_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payroll_period_finalizations_users_ReopenedByUserId",
                        column: x => x.ReopenedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_period_finalizations_BranchId",
                table: "payroll_period_finalizations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_period_finalizations_FinalizedByUserId",
                table: "payroll_period_finalizations",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_period_finalizations_ReopenedByUserId",
                table: "payroll_period_finalizations",
                column: "ReopenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_period_finalizations_Year_Month_BranchId_IsFinalized",
                table: "payroll_period_finalizations",
                columns: new[] { "Year", "Month", "BranchId", "IsFinalized" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_period_finalizations");
        }
    }
}
