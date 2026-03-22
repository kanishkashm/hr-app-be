using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TravelPax.Workforce.Domain.Constants;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Authentication;
using TravelPax.Workforce.Infrastructure.Persistence;

namespace TravelPax.Workforce.ApiTests;

public sealed class TravelPaxApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"travelpax-api-tests-{Guid.NewGuid():N}";
    private const string TestIssuer = "TravelPax.Test";
    private const string TestAudience = "TravelPax.Test.Client";
    private const string TestSecret = "super-secure-testing-key-super-secure-testing-key-123456";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["SkipStartupInitialization"] = "true",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=ignored;Username=ignored;Password=ignored",
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
                ["Jwt:SecretKey"] = TestSecret,
                ["Jwt:AccessTokenMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Email:Enabled"] = "false",
            };
            configBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TravelPaxDbContext>>();
            services.RemoveAll<TravelPaxDbContext>();
            services.AddDbContext<TravelPaxDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.PostConfigure<JwtOptions>(options =>
            {
                options.Issuer = TestIssuer;
                options.Audience = TestAudience;
                options.SecretKey = TestSecret;
                options.AccessTokenMinutes = 60;
                options.RefreshTokenDays = 7;
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });
        });
    }

    public async Task SeedDefaultAuthDataAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TravelPaxDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (!await dbContext.CompanySettings.AnyAsync())
        {
            dbContext.CompanySettings.Add(new CompanySetting
            {
                Id = Guid.NewGuid(),
                CompanyName = "TravelPax",
                DefaultTimezone = "Asia/Colombo",
                WorkingDayStartTime = new TimeOnly(9, 0),
                WorkingDayEndTime = new TimeOnly(18, 0),
                LateGraceMinutes = 15,
                WeekendConfig = "{\"days\":[\"Saturday\",\"Sunday\"]}",
            });
        }

        var branch = await dbContext.OfficeBranches.FirstOrDefaultAsync(x => x.Code == "CMB-HQ");
        if (branch is null)
        {
            branch = new OfficeBranch
            {
                Id = Guid.NewGuid(),
                Code = "CMB-HQ",
                Name = "Colombo Head Office",
                City = "Colombo",
                Country = "Sri Lanka",
                Timezone = "Asia/Colombo",
                IsActive = true,
            };
            dbContext.OfficeBranches.Add(branch);
        }

        var requiredPermissions = new[]
        {
            new { Name = PermissionCodes.AttendanceClock, Module = "attendance", Description = "attendance clock permission" },
            new { Name = PermissionCodes.AuditView, Module = "audit", Description = "audit view permission" }
        };

        foreach (var definition in requiredPermissions)
        {
            var existing = await dbContext.Permissions.FirstOrDefaultAsync(x => x.Name == definition.Name);
            if (existing is null)
            {
                dbContext.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = definition.Name,
                    Module = definition.Module,
                    Description = definition.Description,
                });
            }
        }

        await dbContext.SaveChangesAsync();

        var role = await roleManager.FindByNameAsync(RoleCodes.SuperAdmin);
        if (role is null)
        {
            role = new AppRole
            {
                Name = RoleCodes.SuperAdmin,
                NormalizedName = RoleCodes.SuperAdmin,
                Code = RoleCodes.SuperAdmin,
                IsSystem = true,
            };
            var roleResult = await roleManager.CreateAsync(role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException("Could not create test role.");
            }
        }

        var permissionIds = await dbContext.Permissions
            .Where(x => x.Name == PermissionCodes.AttendanceClock || x.Name == PermissionCodes.AuditView)
            .Select(x => x.Id)
            .ToListAsync();

        var existingRolePermissionIds = await dbContext.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionId)
            .ToListAsync();

        var missingRolePermissionIds = permissionIds
            .Except(existingRolePermissionIds)
            .ToList();

        if (missingRolePermissionIds.Count > 0)
        {
            dbContext.RolePermissions.AddRange(missingRolePermissionIds.Select(permissionId => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId,
            }));
            await dbContext.SaveChangesAsync();
        }

        var user = await userManager.FindByEmailAsync("superadmin@travelpax.lk");
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                EmployeeId = "TP-0001",
                UserName = "superadmin@travelpax.lk",
                Email = "superadmin@travelpax.lk",
                NormalizedUserName = "SUPERADMIN@TRAVELPAX.LK",
                NormalizedEmail = "SUPERADMIN@TRAVELPAX.LK",
                FirstName = "Super",
                LastName = "Admin",
                DisplayName = "Super Admin",
                BranchId = branch.Id,
                Department = "People Operations",
                Designation = "SUPER ADMIN",
                Status = "Active",
                EmailConfirmed = true,
                LockoutEnabled = true,
            };

            var createResult = await userManager.CreateAsync(user, "TravelPax@123");
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Could not create test user.");
            }
        }
        else
        {
            if (!await userManager.CheckPasswordAsync(user, "TravelPax@123"))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, token, "TravelPax@123");
                if (!resetResult.Succeeded)
                {
                    throw new InvalidOperationException("Could not reset test user password.");
                }
            }

            if (user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                user.LockoutEnd = null;
            }

            if (user.AccessFailedCount != 0)
            {
                user.AccessFailedCount = 0;
            }

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, RoleCodes.SuperAdmin))
        {
            await userManager.AddToRoleAsync(user, RoleCodes.SuperAdmin);
        }
    }
}
