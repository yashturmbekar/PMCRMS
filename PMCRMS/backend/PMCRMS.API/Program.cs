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
// Database Configuration (Railway compatible)
string connectionString;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    try
    {
        // Parse DATABASE_URL format: postgresql://user:password@host:port/database
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true;Timeout=30;Command Timeout=30;Pooling=true;MinPoolSize=1;MaxPoolSize=20";
        
        Log.Information("Using database connection from environment variable. Host: {Host}, Database: {Database}", uri.Host, uri.AbsolutePath.Trim('/'));
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to parse database URL from environment variable: {DatabaseUrl}", databaseUrl);
        throw new InvalidOperationException("Invalid database URL format", ex);
    }
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("No database connection string configured");
    Log.Information("Using database connection from configuration file");
}

builder.Services.AddDbContext<PMCRMSDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
    
    // Only enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

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
    
    // Also add HTTPS version if HTTP provided, and vice versa
    if (frontendUrl.StartsWith("https://"))
    {
        var httpVersion = frontendUrl.Replace("https://", "http://");
        if (!origins.Contains(httpVersion))
        {
            origins.Add(httpVersion);
        }
    }
    else if (frontendUrl.StartsWith("http://"))
    {
        var httpsVersion = frontendUrl.Replace("http://", "https://");
        if (!origins.Contains(httpsVersion))
        {
            origins.Add(httpsVersion);
        }
    }
}

// Also read from CorsSettings:AllowedOrigins in config
var configOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
if (configOrigins != null && configOrigins.Length > 0)
{
    foreach (var origin in configOrigins)
    {
        if (!string.IsNullOrEmpty(origin) && !origins.Contains(origin))
        {
            origins.Add(origin);
        }
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
builder.Services.AddHttpClient(); // For Brevo API
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IDataSeeder, DataSeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<PdfService>(); // PDF Generation Service
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

// Serve static files from wwwroot (for generated PDFs)
app.UseStaticFiles();

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
