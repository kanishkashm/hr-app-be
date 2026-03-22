using Microsoft.OpenApi.Models;
using Serilog;
using TravelPax.Workforce.Application;
using TravelPax.Workforce.Infrastructure;
using TravelPax.Workforce.Infrastructure.Persistence;
using TravelPax.Workforce.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TravelPax Workforce API",
        Version = "v1",
        Description = "Attendance-first HR and workforce management API for TravelPax."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
var skipStartupInitialization = builder.Configuration.GetValue<bool>("SkipStartupInitialization");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!skipStartupInitialization)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TravelPaxDbContext>();
    var connectionString = dbContext.Database.GetConnectionString() ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        app.Logger.LogInformation(
            "Applying migrations on PostgreSQL database '{Database}' at host '{Host}' (environment: {Environment})",
            connectionBuilder.Database,
            connectionBuilder.Host,
            app.Environment.EnvironmentName);
    }

    await dbContext.Database.MigrateAsync();
    await dbContext.Database.ExecuteSqlRawAsync("""
        ALTER TABLE attendance_correction_requests
        ADD COLUMN IF NOT EXISTS "RequestType" character varying(40) NOT NULL DEFAULT 'Correction';
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_attendance_correction_requests_RequestType_Status_CreatedAt"
        ON attendance_correction_requests ("RequestType", "Status", "CreatedAt");
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS work_calendar_entries (
            "Id" uuid NOT NULL,
            "BranchId" uuid NULL,
            "CalendarDate" date NOT NULL,
            "DayType" character varying(40) NOT NULL,
            "Name" character varying(200) NOT NULL,
            "IsRecurringAnnual" boolean NOT NULL,
            "IsActive" boolean NOT NULL,
            "Notes" character varying(1000) NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "CreatedBy" uuid NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "UpdatedBy" uuid NULL,
            CONSTRAINT "PK_work_calendar_entries" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_work_calendar_entries_office_branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES office_branches ("Id") ON DELETE SET NULL
        );
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_work_calendar_entries_BranchId_CalendarDate_IsActive"
        ON work_calendar_entries ("BranchId", "CalendarDate", "IsActive");
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        ALTER TABLE users
        ADD COLUMN IF NOT EXISTS "EmergencyContactName" character varying(160) NULL;
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        ALTER TABLE users
        ADD COLUMN IF NOT EXISTS "EmergencyContactPhone" character varying(40) NULL;
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS user_profile_update_requests (
            "Id" uuid NOT NULL,
            "UserId" uuid NOT NULL,
            "CurrentDisplayName" character varying(150) NOT NULL,
            "CurrentMobileNumber" character varying(40) NULL,
            "CurrentEmergencyContactName" character varying(160) NULL,
            "CurrentEmergencyContactPhone" character varying(40) NULL,
            "RequestedDisplayName" character varying(150) NOT NULL,
            "RequestedMobileNumber" character varying(40) NULL,
            "RequestedEmergencyContactName" character varying(160) NULL,
            "RequestedEmergencyContactPhone" character varying(40) NULL,
            "Reason" character varying(1000) NOT NULL,
            "Status" character varying(30) NOT NULL,
            "ReviewedByUserId" uuid NULL,
            "ReviewedAt" timestamp with time zone NULL,
            "ReviewerComment" character varying(1000) NULL,
            "AppliedAt" timestamp with time zone NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "CreatedBy" uuid NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "UpdatedBy" uuid NULL,
            CONSTRAINT "PK_user_profile_update_requests" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_user_profile_update_requests_users_ReviewedByUserId" FOREIGN KEY ("ReviewedByUserId") REFERENCES users ("Id") ON DELETE SET NULL,
            CONSTRAINT "FK_user_profile_update_requests_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
        );
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_user_profile_update_requests_UserId_Status_CreatedAt"
        ON user_profile_update_requests ("UserId", "Status", "CreatedAt");
        """);
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS "IX_user_profile_update_requests_Status_CreatedAt"
        ON user_profile_update_requests ("Status", "CreatedAt");
        """);
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();
}

app.UseSerilogRequestLogging();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();

public partial class Program;
