using TravelPax.Workforce.Contracts.Settings;
using TravelPax.Workforce.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using TravelPax.Workforce.Domain.Constants;
using TravelPax.Workforce.Infrastructure.Settings;

namespace TravelPax.Workforce.UnitTests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task SetAttendancePeriodLockAsync_UnlockThrows_WhenPayrollFinalized()
    {
        await using var db = TestDbFactory.CreateContext();
        var actorId = Guid.NewGuid();
        var service = new SettingsService(db, new FakeCurrentUserService(actorId), new FakeNetworkValidationService());

        db.PayrollPeriodFinalizations.Add(new PayrollPeriodFinalization
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            Month = 3,
            BranchId = null,
            IsFinalized = true,
            FinalizedAt = DateTimeOffset.UtcNow,
            FinalizedByUserId = actorId,
            SnapshotJson = "{}",
            CreatedBy = actorId,
            UpdatedBy = actorId,
        });
        await db.SaveChangesAsync();

        var request = new SetAttendancePeriodLockRequest(2026, 3, null, false, "unlock for edit");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetAttendancePeriodLockAsync(request));
        Assert.Contains("already finalized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FinalizePayrollPeriodAsync_Throws_WhenAttendancePeriodIsNotLocked()
    {
        await using var db = TestDbFactory.CreateContext();
        var actorId = Guid.NewGuid();
        var service = new SettingsService(db, new FakeCurrentUserService(actorId), new FakeNetworkValidationService());

        var request = new FinalizePayrollPeriodRequest(2026, 3, null, "run payroll");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.FinalizePayrollPeriodAsync(request));
        Assert.Contains("Lock the attendance period before finalizing payroll", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FinalizePayrollPeriodAsync_CreatesFinalizationAndAudit_WhenLocked()
    {
        await using var db = TestDbFactory.CreateContext();
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new SettingsService(db, new FakeCurrentUserService(actorId), new FakeNetworkValidationService());

        db.Users.Add(new AppUser
        {
            Id = userId,
            EmployeeId = "TP-0010",
            FirstName = "Ops",
            LastName = "User",
            DisplayName = "Ops User",
            UserName = "ops.user",
            NormalizedUserName = "OPS.USER",
            Email = "ops.user@travelpax.lk",
            NormalizedEmail = "OPS.USER@TRAVELPAX.LK",
            Status = "Active",
        });

        db.AttendancePeriodLocks.Add(new AttendancePeriodLock
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            Month = 3,
            BranchId = null,
            IsLocked = true,
            LockedAt = DateTimeOffset.UtcNow,
            LockedByUserId = actorId,
            CreatedBy = actorId,
            UpdatedBy = actorId,
        });

        db.AttendanceRecords.Add(new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttendanceDate = new DateOnly(2026, 3, 20),
            ClockInAt = new DateTimeOffset(2026, 3, 20, 3, 30, 0, TimeSpan.Zero),
            ClockOutAt = new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero),
            TotalWorkMinutes = 510,
            Status = "Present",
            CreatedBy = userId,
            UpdatedBy = userId,
        });

        await db.SaveChangesAsync();

        var response = await service.FinalizePayrollPeriodAsync(new FinalizePayrollPeriodRequest(2026, 3, null, "monthly close"));

        Assert.True(response.IsFinalized);
        Assert.Equal(2026, response.Year);
        Assert.Equal(3, response.Month);

        var finalization = db.PayrollPeriodFinalizations.Single();
        Assert.True(finalization.IsFinalized);
        Assert.False(string.IsNullOrWhiteSpace(finalization.SnapshotJson));

        var audit = db.AuditLogs.Single(x => x.Action == "PayrollPeriodFinalized");
        Assert.Equal("Payroll", audit.Module);
    }

    [Fact]
    public async Task ReopenPayrollPeriodAsync_Throws_WhenActorIsNotHrOrSuperAdmin()
    {
        await using var db = TestDbFactory.CreateContext();
        var actorId = Guid.NewGuid();
        var finalizationId = Guid.NewGuid();
        var service = new SettingsService(db, new FakeCurrentUserService(actorId), new FakeNetworkValidationService());

        db.PayrollPeriodFinalizations.Add(new PayrollPeriodFinalization
        {
            Id = finalizationId,
            Year = 2026,
            Month = 3,
            IsFinalized = true,
            FinalizedAt = DateTimeOffset.UtcNow,
            FinalizedByUserId = Guid.NewGuid(),
            SnapshotJson = "{}",
            CreatedBy = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReopenPayrollPeriodAsync(finalizationId, new ReopenPayrollPeriodRequest("Need correction", true)));

        Assert.Contains("Only Super Admin or HR Admin", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReopenPayrollPeriodAsync_ReopensAndUnlocksAttendanceLock_WhenAuthorized()
    {
        await using var db = TestDbFactory.CreateContext();
        var actorId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var finalizationId = Guid.NewGuid();
        var lockId = Guid.NewGuid();
        var service = new SettingsService(db, new FakeCurrentUserService(actorId), new FakeNetworkValidationService());

        db.Roles.Add(new AppRole
        {
            Id = roleId,
            Name = RoleCodes.HrAdmin,
            NormalizedName = RoleCodes.HrAdmin,
            Code = RoleCodes.HrAdmin,
            IsSystem = true,
        });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actorId, RoleId = roleId });

        db.AttendancePeriodLocks.Add(new AttendancePeriodLock
        {
            Id = lockId,
            Year = 2026,
            Month = 3,
            BranchId = null,
            IsLocked = true,
            LockedAt = DateTimeOffset.UtcNow,
            LockedByUserId = actorId,
            CreatedBy = actorId,
            UpdatedBy = actorId,
        });

        db.PayrollPeriodFinalizations.Add(new PayrollPeriodFinalization
        {
            Id = finalizationId,
            Year = 2026,
            Month = 3,
            BranchId = null,
            IsFinalized = true,
            FinalizedAt = DateTimeOffset.UtcNow,
            FinalizedByUserId = actorId,
            SnapshotJson = "{}",
            CreatedBy = actorId,
            UpdatedBy = actorId,
        });
        await db.SaveChangesAsync();

        var response = await service.ReopenPayrollPeriodAsync(
            finalizationId,
            new ReopenPayrollPeriodRequest("Urgent attendance correction", true));

        Assert.False(response.IsFinalized);
        Assert.NotNull(response.ReopenedAt);
        Assert.Equal(actorId, response.ReopenedByUserId);

        var lockEntity = await db.AttendancePeriodLocks.FindAsync(lockId);
        Assert.NotNull(lockEntity);
        Assert.False(lockEntity!.IsLocked);
        Assert.NotNull(lockEntity.UnlockedAt);
        Assert.Equal(actorId, lockEntity.UnlockedByUserId);
    }
}
