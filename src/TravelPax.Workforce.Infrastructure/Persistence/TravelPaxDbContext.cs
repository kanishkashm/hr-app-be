using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Domain.Entities;

namespace TravelPax.Workforce.Infrastructure.Persistence;

public sealed class TravelPaxDbContext(DbContextOptions<TravelPaxDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<OfficeBranch> OfficeBranches => Set<OfficeBranch>();
    public DbSet<AllowedNetwork> AllowedNetworks => Set<AllowedNetwork>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();
    public DbSet<WorkCalendarEntry> WorkCalendarEntries => Set<WorkCalendarEntry>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceRuleProfile> AttendanceRuleProfiles => Set<AttendanceRuleProfile>();
    public DbSet<AttendancePeriodLock> AttendancePeriodLocks => Set<AttendancePeriodLock>();
    public DbSet<ShiftDefinition> ShiftDefinitions => Set<ShiftDefinition>();
    public DbSet<EmployeeShiftAssignment> EmployeeShiftAssignments => Set<EmployeeShiftAssignment>();
    public DbSet<ShiftAssignmentRule> ShiftAssignmentRules => Set<ShiftAssignmentRule>();
    public DbSet<ShiftOverride> ShiftOverrides => Set<ShiftOverride>();
    public DbSet<PayrollPeriodFinalization> PayrollPeriodFinalizations => Set<PayrollPeriodFinalization>();
    public DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequests => Set<AttendanceCorrectionRequest>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<UserProfileUpdateRequest> UserProfileUpdateRequests => Set<UserProfileUpdateRequest>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<LoginAuditLog> LoginAuditLogs => Set<LoginAuditLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x => x.EmployeeId).IsUnique();
            entity.Property(x => x.EmployeeId).HasMaxLength(50);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.DisplayName).HasMaxLength(150);
            entity.Property(x => x.Department).HasMaxLength(120);
            entity.Property(x => x.Designation).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.EmergencyContactName).HasMaxLength(160);
            entity.Property(x => x.EmergencyContactPhone).HasMaxLength(40);
            entity.HasOne(x => x.ReportingManager)
                .WithMany()
                .HasForeignKey(x => x.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AppRole>(entity =>
        {
            entity.ToTable("roles");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(100);
        });

        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Module).HasMaxLength(100);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
        });

        builder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("user_permissions");
            entity.HasKey(x => new { x.UserId, x.PermissionId });
        });

        builder.Entity<OfficeBranch>(entity =>
        {
            entity.ToTable("office_branches");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50);
        });

        builder.Entity<AllowedNetwork>(entity => entity.ToTable("allowed_networks"));
        builder.Entity<CompanySetting>().ToTable("company_settings");
        builder.Entity<WorkCalendarEntry>(entity =>
        {
            entity.ToTable("work_calendar_entries");
            entity.Property(x => x.DayType).HasMaxLength(40);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.BranchId, x.CalendarDate, x.IsActive });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("attendance_records");
            entity.HasIndex(x => new { x.UserId, x.AttendanceDate }).IsUnique();
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ClockInNetworkRule).WithMany().HasForeignKey(x => x.ClockInNetworkRuleId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ClockOutNetworkRule).WithMany().HasForeignKey(x => x.ClockOutNetworkRuleId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<AttendanceRuleProfile>(entity =>
        {
            entity.ToTable("attendance_rule_profiles");
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.ScopeType).HasMaxLength(30);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.ScopeType, x.BranchId, x.ShiftId, x.Priority, x.IsActive });
        });
        builder.Entity<ShiftDefinition>(entity =>
        {
            entity.ToTable("shift_definitions");
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.ShiftType).HasMaxLength(30);
            entity.HasIndex(x => new { x.Code, x.BranchId }).IsUnique();
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<EmployeeShiftAssignment>(entity =>
        {
            entity.ToTable("employee_shift_assignments");
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.EffectiveFrom, x.EffectiveTo, x.IsActive });
        });
        builder.Entity<ShiftAssignmentRule>(entity =>
        {
            entity.ToTable("shift_assignment_rules");
            entity.Property(x => x.Department).HasMaxLength(120);
            entity.Property(x => x.Team).HasMaxLength(120);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.BranchId, x.Department, x.Team, x.EffectiveFrom, x.EffectiveTo, x.Priority, x.IsActive });
        });
        builder.Entity<ShiftOverride>(entity =>
        {
            entity.ToTable("shift_overrides");
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
        });
        builder.Entity<AttendancePeriodLock>(entity =>
        {
            entity.ToTable("attendance_period_locks");
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.Year, x.Month, x.BranchId }).IsUnique();
            entity.HasIndex(x => new { x.Year, x.Month, x.IsLocked });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.LockedByUser).WithMany().HasForeignKey(x => x.LockedByUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.UnlockedByUser).WithMany().HasForeignKey(x => x.UnlockedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<PayrollPeriodFinalization>(entity =>
        {
            entity.ToTable("payroll_period_finalizations");
            entity.Property(x => x.SnapshotJson).HasColumnType("jsonb");
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => new { x.Year, x.Month, x.BranchId, x.IsFinalized });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.FinalizedByUser).WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ReopenedByUser).WithMany().HasForeignKey(x => x.ReopenedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<AttendanceCorrectionRequest>(entity =>
        {
            entity.ToTable("attendance_correction_requests");
            entity.Property(x => x.RequestType).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.ReviewerNote).HasMaxLength(1000);
            entity.Property(x => x.ReviewIpAddress).HasMaxLength(80);
            entity.HasOne(x => x.AttendanceRecord)
                .WithMany()
                .HasForeignKey(x => x.AttendanceRecordId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.RequestedByUser)
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.RequestType, x.Status, x.CreatedAt });
        });
        builder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("leave_requests");
            entity.Property(x => x.LeaveType).HasMaxLength(50);
            entity.Property(x => x.DayPortion).HasMaxLength(30);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.ReviewerNote).HasMaxLength(1000);
            entity.Property(x => x.HrReviewerNote).HasMaxLength(1000);
            entity.Property(x => x.DirectorReviewerNote).HasMaxLength(1000);
            entity.Property(x => x.TotalDays).HasPrecision(5, 1);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.HrReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.HrReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.DirectorReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.DirectorReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.UserId, x.StartDate, x.EndDate });
        });
        builder.Entity<LeavePolicy>(entity =>
        {
            entity.ToTable("leave_policies");
            entity.Property(x => x.LeaveType).HasMaxLength(50);
            entity.Property(x => x.EmploymentType).HasMaxLength(50);
            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.LeaveType, x.EmploymentType, x.BranchId }).IsUnique();
        });
        builder.Entity<LeaveBalance>(entity =>
        {
            entity.ToTable("leave_balances");
            entity.Property(x => x.LeaveType).HasMaxLength(50);
            entity.Property(x => x.AllocatedDays).HasPrecision(6, 1);
            entity.Property(x => x.CarryForwardDays).HasPrecision(6, 1);
            entity.Property(x => x.ManualAdjustmentDays).HasPrecision(6, 1);
            entity.Property(x => x.UsedDays).HasPrecision(6, 1);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.Year, x.LeaveType }).IsUnique();
        });
        builder.Entity<UserProfileUpdateRequest>(entity =>
        {
            entity.ToTable("user_profile_update_requests");
            entity.Property(x => x.CurrentDisplayName).HasMaxLength(150);
            entity.Property(x => x.CurrentMobileNumber).HasMaxLength(40);
            entity.Property(x => x.CurrentEmergencyContactName).HasMaxLength(160);
            entity.Property(x => x.CurrentEmergencyContactPhone).HasMaxLength(40);
            entity.Property(x => x.RequestedDisplayName).HasMaxLength(150);
            entity.Property(x => x.RequestedMobileNumber).HasMaxLength(40);
            entity.Property(x => x.RequestedEmergencyContactName).HasMaxLength(160);
            entity.Property(x => x.RequestedEmergencyContactPhone).HasMaxLength(40);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.ReviewerComment).HasMaxLength(1000);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });
        builder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("user_notifications");
            entity.Property(x => x.Type).HasMaxLength(50);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.EntityName).HasMaxLength(100);
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });
        builder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.ToTable("email_outbox_messages");
            entity.Property(x => x.ToEmail).HasMaxLength(320);
            entity.Property(x => x.Subject).HasMaxLength(400);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt, x.CreatedAt });
        });
        builder.Entity<LoginAuditLog>().ToTable("login_audit_logs");
        builder.Entity<AuditLog>().ToTable("audit_logs");
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasIndex(x => x.Token).IsUnique();
        });
    }
}
