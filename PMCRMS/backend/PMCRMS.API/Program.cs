using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using PMCRMS.API.Data;
using PMCRMS.API.Services;
using QuestPDF.Infrastructure;

// Configure QuestPDF License (Community License for non-commercial/government projects under $1M revenue)
QuestPDF.Settings.License = LicenseType.Community;

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

// Configure HSM Configuration
builder.Services.Configure<PMCRMS.API.Configuration.HsmConfiguration>(
    builder.Configuration.GetSection(PMCRMS.API.Configuration.HsmConfiguration.SectionName));

// Configure HSM HTTP clients for digital signature
builder.Services.AddHttpClient("HsmClient", (serviceProvider, client) =>
{
    var hsmConfig = builder.Configuration.GetSection(PMCRMS.API.Configuration.HsmConfiguration.SectionName)
        .Get<PMCRMS.API.Configuration.HsmConfiguration>();
    
    var otpUrl = hsmConfig?.OtpServiceUrl ?? builder.Configuration["HSM:OtpBaseUrl"] ?? "http://210.212.188.44:8001/jrequest/";
    client.BaseAddress = new Uri(otpUrl);
    
    var timeout = hsmConfig?.TimeoutSeconds ?? 60;
    client.Timeout = TimeSpan.FromSeconds(timeout);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("SignerClient", (serviceProvider, client) =>
{
    var hsmConfig = builder.Configuration.GetSection(PMCRMS.API.Configuration.HsmConfiguration.SectionName)
        .Get<PMCRMS.API.Configuration.HsmConfiguration>();
    
    var signUrl = hsmConfig?.SignerServiceUrl ?? builder.Configuration["HSM:SignBaseUrl"] ?? "http://210.212.188.35:8080/emSigner/";
    client.BaseAddress = new Uri(signUrl);
    
    var timeout = hsmConfig?.TimeoutSeconds ?? 60;
    client.Timeout = TimeSpan.FromSeconds(timeout);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
});

// Legacy HSM clients (for backward compatibility)
builder.Services.AddHttpClient("HSM_OTP", client =>
{
    var hsmOtpUrl = builder.Configuration["HSM:OtpBaseUrl"] ?? "http://210.212.188.44:8001/jrequest/";
    client.BaseAddress = new Uri(hsmOtpUrl);
    client.Timeout = TimeSpan.Parse(builder.Configuration["HSM:Timeout"] ?? "00:00:30");
});

builder.Services.AddHttpClient("HSM_SIGN", client =>
{
    var hsmSignUrl = builder.Configuration["HSM:SignBaseUrl"] ?? "http://210.212.188.35:8080/emSigner/";
    client.BaseAddress = new Uri(hsmSignUrl);
    client.Timeout = TimeSpan.Parse(builder.Configuration["HSM:Timeout"] ?? "00:00:30");
});

// Configure BillDesk Payment HTTP client
builder.Services.AddHttpClient("BillDesk_Payment", client =>
{
    var paymentUrl = builder.Configuration["BillDesk:PaymentGatewayUrl"] ?? "https://pay.billdesk.com/web/v1_2/embeddedsdk";
    client.BaseAddress = new Uri(paymentUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IDataSeeder, DataSeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWorkflowNotificationService, WorkflowNotificationService>(); // Workflow email notifications to applicants
builder.Services.AddScoped<PdfService>(); // PDF Generation Service
builder.Services.AddScoped<IHsmService, HsmService>(); // HSM service for digital signatures
builder.Services.AddScoped<IAutoAssignmentService, AutoAssignmentService>(); // Auto-assignment for Junior Engineer workflow
builder.Services.AddScoped<IWorkflowProgressionService, WorkflowProgressionService>(); // Complete workflow progression with auto-assignment
builder.Services.AddScoped<IAppointmentService, AppointmentService>(); // Appointment scheduling for Junior Engineer workflow
builder.Services.AddScoped<IDocumentVerificationService, DocumentVerificationService>(); // Document verification for Junior Engineer workflow
builder.Services.AddScoped<IDigitalSignatureService, DigitalSignatureService>(); // Digital signature with HSM integration for Junior Engineer workflow
builder.Services.AddScoped<IJEWorkflowService, JEWorkflowService>(); // Workflow orchestration - Complete JE workflow coordination
builder.Services.AddScoped<IAEWorkflowService, AEWorkflowService>(); // Assistant Engineer workflow service
builder.Services.AddScoped<IEEWorkflowService, EEWorkflowService>(); // Executive Engineer workflow service
builder.Services.AddScoped<ICEWorkflowService, CEWorkflowService>(); // City Engineer workflow service (Final Approval)
builder.Services.AddScoped<ClerkWorkflowService>(); // Clerk workflow service (Post-Payment Processing)
builder.Services.AddScoped<EEStage2WorkflowService>(); // EE Stage 2 workflow service (Certificate Digital Signature)
builder.Services.AddScoped<CEStage2WorkflowService>(); // CE Stage 2 workflow service (Final Certificate Signature)
builder.Services.AddScoped<DocumentDownloadService>(); // Document download service with OTP authentication (Public Access)
builder.Services.AddScoped<IChallanService, ChallanService>(); // Challan generation service with bilingual PDF support
builder.Services.AddScoped<ISignatureWorkflowService, SignatureWorkflowService>(); // Sequential digital signature workflow (JE → AE → EE → CE)

// BillDesk Payment Gateway Services
builder.Services.AddSingleton<IBillDeskConfigService, BillDeskConfigService>(); // BillDesk configuration (singleton)
builder.Services.AddScoped<IPluginContextService, PluginContextService>(); // Plugin context for BillDesk operations
builder.Services.AddScoped<IBillDeskPaymentService, BillDeskPaymentService>(); // BillDesk payment service
builder.Services.AddScoped<PaymentService>(); // Main payment orchestration service

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
        
        // Ensure System Admin exists
        Log.Information("Ensuring System Admin exists...");
        await dataSeeder.EnsureSystemAdminExistsAsync();
        Log.Information("System Admin check completed.");
        
        // Update officer credentials
        Log.Information("Updating officer credentials...");
        await dataSeeder.UpdateOfficerCredentialsAsync();
        Log.Information("Officer credentials update completed.");
        
        // Seed auto-assignment rules
        Log.Information("Seeding auto-assignment rules...");
        await dataSeeder.SeedAutoAssignmentRulesAsync();
        Log.Information("Auto-assignment rules seeding completed.");
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
