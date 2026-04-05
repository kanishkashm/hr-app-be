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

app.Logger.LogInformation("Starting TravelPax Workforce API (environment: {Environment})", app.Environment.EnvironmentName);
app.Logger.LogInformation("SkipStartupInitialization is set to {SkipStartupInitialization}", skipStartupInitialization);
if (!skipStartupInitialization)
{
    app.Logger.LogInformation("Applying database migrations and seeding initial data...");
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
