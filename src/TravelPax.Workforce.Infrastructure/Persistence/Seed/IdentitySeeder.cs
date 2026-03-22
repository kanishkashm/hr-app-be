using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelPax.Workforce.Domain.Constants;
using TravelPax.Workforce.Domain.Entities;

namespace TravelPax.Workforce.Infrastructure.Persistence.Seed;

public sealed class IdentitySeeder(
    TravelPaxDbContext dbContext,
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Code == "CMB-HQ", cancellationToken);
        if (branch is null)
        {
            branch = new OfficeBranch
            {
                Code = "CMB-HQ",
                Name = "Colombo Head Office",
                City = "Colombo"
            };
            dbContext.OfficeBranches.Add(branch);
        }

        if (!await dbContext.CompanySettings.AnyAsync(cancellationToken))
        {
            dbContext.CompanySettings.Add(new CompanySetting());
        }

        if (!await dbContext.AllowedNetworks.AnyAsync(cancellationToken))
        {
            dbContext.AllowedNetworks.Add(new AllowedNetwork
            {
                Branch = branch,
                Name = "Colombo HQ Public IP",
                IpOrCidr = "203.94.76.0/24",
                NetworkType = "Cidr",
                ValidationMode = "Allow",
                Priority = 1
            });
        }

        var attendanceRuleProfilesTableExists = await TableExistsAsync("attendance_rule_profiles", cancellationToken);
        if (attendanceRuleProfilesTableExists && !await dbContext.AttendanceRuleProfiles.AnyAsync(cancellationToken))
        {
            dbContext.AttendanceRuleProfiles.Add(new AttendanceRuleProfile
            {
                Id = Guid.NewGuid(),
                Name = "Default Global Rules",
                ScopeType = "Global",
                Priority = 100,
                IsActive = true,
                LateGraceMinutes = 15,
                HalfDayThresholdMinutes = 240,
                MinPresentMinutes = 480,
                OvertimeThresholdMinutes = 540,
                EarlyOutGraceMinutes = 0,
                ShortLeaveDeductionMinutes = 60,
                EnableMissedPunchDetection = true
            });
        }

        if (!await dbContext.LeavePolicies.AnyAsync(cancellationToken))
        {
            dbContext.LeavePolicies.AddRange(
                new LeavePolicy
                {
                    Id = Guid.NewGuid(),
                    LeaveType = "Annual",
                    EmploymentType = "FullTime",
                    Branch = branch,
                    AnnualAllocationDays = 14,
                    MaxCarryForwardDays = 5
                },
                new LeavePolicy
                {
                    Id = Guid.NewGuid(),
                    LeaveType = "Casual",
                    EmploymentType = "FullTime",
                    Branch = branch,
                    AnnualAllocationDays = 7,
                    MaxCarryForwardDays = 0
                },
                new LeavePolicy
                {
                    Id = Guid.NewGuid(),
                    LeaveType = "Medical",
                    EmploymentType = "FullTime",
                    Branch = branch,
                    AnnualAllocationDays = 10,
                    MaxCarryForwardDays = 0
                });
        }

        foreach (var permissionCode in PermissionCodes.All)
        {
            if (await dbContext.Permissions.AnyAsync(x => x.Name == permissionCode, cancellationToken))
            {
                continue;
            }

            dbContext.Permissions.Add(new Permission
            {
                Name = permissionCode,
                Module = permissionCode.Split('.')[0],
                Description = $"{permissionCode} permission"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var roleDefinitions = new Dictionary<string, string[]>
        {
            [RoleCodes.SuperAdmin] = PermissionCodes.All,
            [RoleCodes.HrAdmin] =
            [
                PermissionCodes.AttendanceView,
                PermissionCodes.AttendanceClock,
                PermissionCodes.AttendanceManage,
                PermissionCodes.LeaveRequest,
                PermissionCodes.LeaveView,
                PermissionCodes.LeaveManage,
                PermissionCodes.ShiftsView,
                PermissionCodes.ShiftsManage,
                PermissionCodes.UsersView,
                PermissionCodes.UsersCreate,
                PermissionCodes.UsersEdit,
                PermissionCodes.RolesView,
                PermissionCodes.AuditView,
                PermissionCodes.SettingsManage
            ],
            [RoleCodes.OperationsManager] = [PermissionCodes.AttendanceView, PermissionCodes.AttendanceManage, PermissionCodes.ReportsView, PermissionCodes.LeaveRequest, PermissionCodes.LeaveView, PermissionCodes.ShiftsView],
            [RoleCodes.TeamLead] = [PermissionCodes.AttendanceView, PermissionCodes.ReportsView, PermissionCodes.LeaveRequest, PermissionCodes.LeaveView, PermissionCodes.ShiftsView],
            [RoleCodes.Employee] = [PermissionCodes.AttendanceClock, PermissionCodes.LeaveRequest]
        };

        foreach (var (roleCode, permissions) in roleDefinitions)
        {
            var role = await roleManager.FindByNameAsync(roleCode);
            if (role is null)
            {
                role = new AppRole
                {
                    Name = roleCode,
                    NormalizedName = roleCode,
                    Code = roleCode,
                    IsSystem = true,
                    Description = roleCode.Replace('_', ' ')
                };

                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
                }
            }

            foreach (var permissionCode in permissions)
            {
                var permission = await dbContext.Permissions.FirstAsync(x => x.Name == permissionCode, cancellationToken);
                var exists = await dbContext.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id, cancellationToken);

                if (!exists)
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureUserAsync("TP-0001", "superadmin@travelpax.lk", "Super", "Admin", "Super Admin", RoleCodes.SuperAdmin, branch.Id, cancellationToken);
        await EnsureUserAsync("TP-0002", "hradmin@travelpax.lk", "HR", "Admin", "HR Admin", RoleCodes.HrAdmin, branch.Id, cancellationToken);
        var employee = await EnsureUserAsync("TP-0003", "employee@travelpax.lk", "Team", "Member", "TravelPax Employee", RoleCodes.Employee, branch.Id, cancellationToken);

        if (!await dbContext.AttendanceRecords.AnyAsync(x => x.UserId == employee.Id, cancellationToken))
        {
            var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
            var todayColombo = TimeZoneInfo.ConvertTime(DateTime.UtcNow, sriLankaTimeZone);
            var sampleLocalDate = todayColombo.Date.AddDays(-1);
            var clockInUtc = TimeZoneInfo.ConvertTimeToUtc(sampleLocalDate.AddHours(9).AddMinutes(6), sriLankaTimeZone);
            var clockOutUtc = TimeZoneInfo.ConvertTimeToUtc(sampleLocalDate.AddHours(18).AddMinutes(2), sriLankaTimeZone);
            dbContext.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                UserId = employee.Id,
                BranchId = branch.Id,
                AttendanceDate = DateOnly.FromDateTime(sampleLocalDate),
                ClockInAt = new DateTimeOffset(clockInUtc, TimeSpan.Zero),
                ClockOutAt = new DateTimeOffset(clockOutUtc, TimeSpan.Zero),
                TotalWorkMinutes = 536,
                Status = "Late",
                IsLate = true,
                LateMinutes = 6,
                ClockInIp = "203.94.76.25",
                ClockOutIp = "203.94.76.25",
                ClockInNetworkValidation = "InsideOfficeNetwork",
                ClockOutNetworkValidation = "InsideOfficeNetwork"
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.LeaveRequests.AnyAsync(x => x.UserId == employee.Id, cancellationToken))
        {
            var sampleStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2));
            dbContext.LeaveRequests.Add(new LeaveRequest
            {
                Id = Guid.NewGuid(),
                UserId = employee.Id,
                LeaveType = "Annual",
                StartDate = sampleStart,
                EndDate = sampleStart.AddDays(1),
                TotalDays = 2,
                Reason = "Sample personal leave request",
                Status = "Pending",
                CreatedBy = employee.Id,
                UpdatedBy = employee.Id
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AppUser> EnsureUserAsync(
        string employeeId,
        string email,
        string firstName,
        string lastName,
        string displayName,
        string roleCode,
        Guid branchId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                UserName = email,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                FirstName = firstName,
                LastName = lastName,
                DisplayName = displayName,
                Department = "People Operations",
                Designation = roleCode.Replace('_', ' '),
                EmploymentType = "FullTime",
                DateJoined = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EmailConfirmed = true,
                BranchId = branchId,
                Status = "Active",
                LockoutEnabled = true
            };

            var result = await userManager.CreateAsync(user, "TravelPax@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleCode))
        {
            await userManager.AddToRoleAsync(user, roleCode);
        }

        return user;
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name = @tableName
            );
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }
}
