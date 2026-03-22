using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveHalfDayAndDecimalDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDays",
                table: "leave_requests",
                type: "numeric(5,1)",
                precision: 5,
                scale: 1,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "DayPortion",
                table: "leave_requests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "UsedDays",
                table: "leave_balances",
                type: "numeric(6,1)",
                precision: 6,
                scale: 1,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "ManualAdjustmentDays",
                table: "leave_balances",
                type: "numeric(6,1)",
                precision: 6,
                scale: 1,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "CarryForwardDays",
                table: "leave_balances",
                type: "numeric(6,1)",
                precision: 6,
                scale: 1,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "AllocatedDays",
                table: "leave_balances",
                type: "numeric(6,1)",
                precision: 6,
                scale: 1,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayPortion",
                table: "leave_requests");

            migrationBuilder.AlterColumn<int>(
                name: "TotalDays",
                table: "leave_requests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,1)",
                oldPrecision: 5,
                oldScale: 1);

            migrationBuilder.AlterColumn<int>(
                name: "UsedDays",
                table: "leave_balances",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,1)",
                oldPrecision: 6,
                oldScale: 1);

            migrationBuilder.AlterColumn<int>(
                name: "ManualAdjustmentDays",
                table: "leave_balances",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,1)",
                oldPrecision: 6,
                oldScale: 1);

            migrationBuilder.AlterColumn<int>(
                name: "CarryForwardDays",
                table: "leave_balances",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,1)",
                oldPrecision: 6,
                oldScale: 1);

            migrationBuilder.AlterColumn<int>(
                name: "AllocatedDays",
                table: "leave_balances",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,1)",
                oldPrecision: 6,
                oldScale: 1);
        }
    }
}
