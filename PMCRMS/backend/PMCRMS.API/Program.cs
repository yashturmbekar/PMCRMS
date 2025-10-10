using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using PMCRMS.API.Data;
using PMCRMS.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pmcrms-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
// Get connection string - Railway compatibility
var connectionString = BuildConnectionString();

string BuildConnectionString()
{
    // Try Railway-style individual components first (most reliable)
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");

    if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgUser) && !string.IsNullOrEmpty(pgPassword))
    {
        var port = string.IsNullOrEmpty(pgPort) ? "5432" : pgPort;
        var database = string.IsNullOrEmpty(pgDatabase) ? "railway" : pgDatabase;
        var connString = $"Host={pgHost};Port={port};Database={database};Username={pgUser};Password={pgPassword};SSL Mode=Prefer;Trust Server Certificate=true";
        Log.Information("Built connection string from Railway PG components");
        return connString;
    }

    // Try DATABASE_URL
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        try
        {
            // Parse Railway DATABASE_URL format: postgresql://user:password@host:port/database
            var uri = new Uri(databaseUrl.Replace("postgres://", "postgresql://"));
            var host = uri.Host;
            var port = uri.Port;
            var database = uri.AbsolutePath.TrimStart('/');
            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

            var connString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
            Log.Information("Parsed DATABASE_URL successfully");
            return connString;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to parse DATABASE_URL, falling back to config");
        }
    }

    // Fallback to configuration
    var configConnString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(configConnString))
    {
        Log.Information("Using DefaultConnection from configuration");
        return configConnString;
    }

    throw new InvalidOperationException("Database connection string is not configured. Set DATABASE_URL or PG* environment variables");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string could not be built");
}

Log.Information("Database connection configured successfully");

builder.Services.AddDbContext<PMCRMSDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(jwtSecretKey))
{
    throw new InvalidOperationException("JWT Secret Key is not configured. Set JWT_SECRET_KEY environment variable or JwtSettings:SecretKey in appsettings.json");
}

var secretKey = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure CORS
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
var origins = new List<string>
{
    "http://localhost:5173",
    "http://localhost:3000",
    "http://localhost:5000"
};

// Add frontend URL from environment if available
if (!string.IsNullOrEmpty(frontendUrl))
{
    // Remove trailing slashes for consistency
    frontendUrl = frontendUrl.TrimEnd('/');
    if (!origins.Contains(frontendUrl))
    {
        origins.Add(frontendUrl);
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(origins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

Log.Information("CORS configured with origins: {Origins}", string.Join(", ", origins));

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "PMCRMS API", 
        Version = "v1",
        Description = "Permit Management and Certificate Recommendation Management System API"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer your_token')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Register application services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IDataSeeder, DataSeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();
// TODO: Add more service registrations here

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments (can be restricted later if needed)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PMCRMS API V1");
    c.RoutePrefix = string.Empty; // Swagger UI at root
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health");

// API Info endpoint
app.MapGet("/api/info", () => Results.Ok(new 
{ 
    Name = "PMCRMS API",
    Version = "v1.0",
    Status = "Running",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Endpoints = new 
    {
        Health = "/health",
        Swagger = "/swagger",
        Api = "/api/*"
    }
}))
   .WithName("ApiInfo")
   .WithTags("Info")
   .AllowAnonymous();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PMCRMSDbContext>();
    var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    
    try
    {
        Log.Information("Applying database migrations...");
        context.Database.Migrate();
        Log.Information("Database migrations applied successfully.");
        
        // Seed officer passwords
        Log.Information("Seeding officer passwords...");
        await dataSeeder.SeedOfficerPasswordsAsync();
        Log.Information("Officer password seeding completed.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while applying database migrations or seeding data.");
    }
}

Log.Information("PMCRMS API is starting up...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PMCRMS API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
