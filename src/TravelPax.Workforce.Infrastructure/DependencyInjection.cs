using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelPax.Workforce.Application.Abstractions.Authentication;
using TravelPax.Workforce.Application.Abstractions.Attendance;
using TravelPax.Workforce.Application.Abstractions.Audit;
using TravelPax.Workforce.Application.Abstractions.CurrentUser;
using TravelPax.Workforce.Application.Abstractions.Networking;
using TravelPax.Workforce.Application.Abstractions.Roles;
using TravelPax.Workforce.Application.Abstractions.Settings;
using TravelPax.Workforce.Application.Abstractions.Users;
using TravelPax.Workforce.Application.Abstractions.Reports;
using TravelPax.Workforce.Infrastructure.Attendance;
using TravelPax.Workforce.Infrastructure.Audit;
using TravelPax.Workforce.Domain.Entities;
using TravelPax.Workforce.Infrastructure.Authentication;
using TravelPax.Workforce.Infrastructure.Networking;
using TravelPax.Workforce.Infrastructure.Persistence;
using TravelPax.Workforce.Infrastructure.Persistence.Seed;
using TravelPax.Workforce.Infrastructure.Roles;
using TravelPax.Workforce.Infrastructure.Settings;
using TravelPax.Workforce.Infrastructure.Users;
using TravelPax.Workforce.Domain.Constants;
using TravelPax.Workforce.Infrastructure.Reports;

namespace TravelPax.Workforce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INetworkValidationService, NetworkValidationService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IdentitySeeder>();

        services.AddDbContext<TravelPaxDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<AppRole>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddEntityFrameworkStores<TravelPaxDbContext>()
            .AddDefaultTokenProviders();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });

        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionCodes.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.RequireClaim("permission", permission));
            }
        });
        return services;
    }
}
