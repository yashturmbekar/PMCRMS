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
// Get connection string from environment variable or configuration
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Log the connection string status for debugging (mask sensitive parts)
Log.Information("DATABASE_URL from env: {HasValue}", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL")));
Log.Information("DefaultConnection from config: {HasValue}", !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection")));

if (string.IsNullOrWhiteSpace(connectionString))
{
    Log.Error("DATABASE_URL is empty or null. Cannot start application.");
    throw new InvalidOperationException("Database connection string is not configured. Set DATABASE_URL environment variable or DefaultConnection in appsettings.json");
}

// Convert postgres:// to postgresql:// if needed (Railway compatibility)
if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) && 
    !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    connectionString = connectionString.Replace("postgres://", "postgresql://", StringComparison.OrdinalIgnoreCase);
    Log.Information("Converted connection string from postgres:// to postgresql://");
}

// Log masked connection string for debugging
var maskedConnectionString = connectionString.Length > 20 
    ? $"{connectionString.Substring(0, 20)}...{connectionString.Substring(connectionString.Length - 10)}" 
    : "too short";
Log.Information("Database connection string configured: {MaskedString}", maskedConnectionString);

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
