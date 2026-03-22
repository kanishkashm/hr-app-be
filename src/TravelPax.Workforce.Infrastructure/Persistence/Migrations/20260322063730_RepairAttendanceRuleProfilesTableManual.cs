using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPax.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairAttendanceRuleProfilesTableManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent repair migration: safe whether table exists or not.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS attendance_rule_profiles (
                    "Id" uuid NOT NULL,
                    "Name" character varying(120) NOT NULL,
                    "ScopeType" character varying(30) NOT NULL,
                    "BranchId" uuid NULL,
                    "ShiftId" uuid NULL,
                    "Priority" integer NOT NULL,
                    "IsActive" boolean NOT NULL,
                    "LateGraceMinutes" integer NULL,
                    "HalfDayThresholdMinutes" integer NULL,
                    "MinPresentMinutes" integer NULL,
                    "OvertimeThresholdMinutes" integer NULL,
                    "EarlyOutGraceMinutes" integer NULL,
                    "ShortLeaveDeductionMinutes" integer NULL,
                    "EnableMissedPunchDetection" boolean NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NULL,
                    "UpdatedBy" uuid NULL,
                    CONSTRAINT "PK_attendance_rule_profiles" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_attendance_rule_profiles_office_branches_BranchId"
                        FOREIGN KEY ("BranchId") REFERENCES office_branches ("Id") ON DELETE SET NULL,
                    CONSTRAINT "FK_attendance_rule_profiles_shift_definitions_ShiftId"
                        FOREIGN KEY ("ShiftId") REFERENCES shift_definitions ("Id") ON DELETE SET NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_attendance_rule_profiles_BranchId"
                ON attendance_rule_profiles ("BranchId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_attendance_rule_profiles_ShiftId"
                ON attendance_rule_profiles ("ShiftId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_attendance_rule_profiles_ScopeType_BranchId_ShiftId_Priority_IsActive"
                ON attendance_rule_profiles ("ScopeType", "BranchId", "ShiftId", "Priority", "IsActive");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_attendance_rule_profiles_ScopeType_BranchId_ShiftId_Priority_IsActive";
                DROP INDEX IF EXISTS "IX_attendance_rule_profiles_ShiftId";
                DROP INDEX IF EXISTS "IX_attendance_rule_profiles_BranchId";
                """);
        }
    }
}
