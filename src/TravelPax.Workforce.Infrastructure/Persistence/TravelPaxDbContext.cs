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
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequests => Set<AttendanceCorrectionRequest>();
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
        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("attendance_records");
            entity.HasIndex(x => new { x.UserId, x.AttendanceDate }).IsUnique();
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ClockInNetworkRule).WithMany().HasForeignKey(x => x.ClockInNetworkRuleId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ClockOutNetworkRule).WithMany().HasForeignKey(x => x.ClockOutNetworkRuleId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<AttendanceCorrectionRequest>(entity =>
        {
            entity.ToTable("attendance_correction_requests");
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
