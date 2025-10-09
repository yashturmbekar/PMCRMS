# PMCRMS Copilot Instructions

## Project Overview

PMCRMS (Permit Management and Certificate Recommendation Management System) is a full-stack application for Pune Municipal Corporation built with .NET 9 Web API backend and React/TypeScript frontend. It manages building permits through a complex 23-step approval workflow with role-based access control.

## Architecture & Technology Stack

### Backend (.NET 9 Web API)
- **Location**: `PMCRMS/backend/PMCRMS.API/`  
- **Database**: PostgreSQL with Entity Framework Core 9.0
- **Authentication**: JWT tokens with OTP-based passwordless login
- **Logging**: Serilog with structured logging to files and console
- **API Documentation**: Swagger/OpenAPI with JWT authentication support
- **File Handling**: Local storage with configurable cloud options

### Frontend (React + TypeScript + Vite)
- **Location**: `PMCRMS/frontend/`
- **Stack**: React 19, TypeScript 5.9, Tailwind CSS 4.1, Vite 7.1
- **State Management**: Context API for auth, TanStack Query for server state
- **UI Components**: Custom PMC-themed components with professional styling
- **Form Handling**: Formik with Yup validation

## File Structure & Organization

### Backend Structure
```
PMCRMS.API/
├── Controllers/          # API endpoints with [ApiController] and [Route] attributes
├── Models/              # Database entities inheriting BaseEntity
├── DTOs/                # Request/Response objects, separate files per controller
├── Data/                # DbContext and migrations
├── Services/            # Business logic (planned - use DI container)
├── Repositories/        # Data access layer (planned - implement Repository pattern)
├── Migrations/          # EF Core database migrations
├── logs/                # Serilog output files (pmcrms-{date}.log)
├── uploads/             # File storage directory
└── Properties/          # launchSettings.json for dev environment
```

### Frontend Structure  
```
src/
├── components/          # Reusable UI components
├── pages/              # Route-specific page components
├── contexts/           # React Context providers (AuthContext)
├── hooks/              # Custom React hooks (useAuth)
├── services/           # API communication (apiService.ts)
├── types/              # TypeScript type definitions
├── assets/             # Static assets
└── index.css           # Global styles with PMC design system
```

## Coding Standards & Best Practices

### Backend C# Standards

#### Entity Models
- **MUST** inherit from `BaseEntity` for audit trails
- Use proper data annotations: `[Required]`, `[MaxLength]`, `[EmailAddress]`
- Implement navigation properties for EF relationships
- Use enum types for controlled values (UserRole, ApplicationStatus)

```csharp
public class User : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    public UserRole Role { get; set; } = UserRole.Applicant;
    
    // Navigation properties
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
```

#### Controller Patterns
- Use `[ApiController]` and `[Route("api/[controller]")]` attributes
- Return standardized `ApiResponse<T>` wrapper
- Implement proper error handling with try-catch blocks
- Use dependency injection for services
- Add role-based authorization with `[Authorize(Roles = "Admin")]`

```csharp
[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly PMCRMSDbContext _context;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(PMCRMSDbContext context, ILogger<ApplicationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<ApplicationDto>>>> GetApplications()
    {
        try
        {
            _logger.LogInformation("Fetching applications for user {UserId}", User.FindFirst("user_id")?.Value);
            // Implementation
            return Ok(new ApiResponse<List<ApplicationDto>>
            {
                Success = true,
                Data = applications,
                Message = "Applications retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching applications");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Internal server error",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
```

#### DTOs and API Responses
- Create separate Request/Response DTOs for each endpoint
- Use `ApiResponse<T>` wrapper with `Success`, `Message`, `Data`, `Errors` properties
- Implement validation attributes on request DTOs

```csharp
public class CreateApplicationRequest
{
    [Required]
    [MaxLength(100)]
    public string ProjectTitle { get; set; } = string.Empty;
    
    [Required]
    public string ApplicationType { get; set; } = string.Empty;
    
    [Range(0.1, double.MaxValue)]
    public decimal PlotArea { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
```

### Frontend TypeScript Standards

#### Component Structure
- Use functional components with TypeScript interfaces
- Implement proper prop typing and default values
- Use React.FC for component typing
- Follow PMC naming conventions for CSS classes

```typescript
interface ApplicationCardProps {
  application: Application;
  onStatusUpdate?: (id: number, status: string) => void;
  className?: string;
}

const ApplicationCard: React.FC<ApplicationCardProps> = ({ 
  application, 
  onStatusUpdate,
  className = "" 
}) => {
  return (
    <div className={`pmc-card p-6 ${className}`}>
      <h3 className="text-lg font-semibold text-pmc-gray-800">
        {application.projectTitle}
      </h3>
      {/* Implementation */}
    </div>
  );
};
```

#### API Service Patterns
- Centralize all API calls in `apiService.ts`
- Use axios interceptors for authentication and error handling
- Implement proper TypeScript interfaces for all API responses
- Store JWT tokens in localStorage with consistent naming (`pmcrms_token`)

```typescript
class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: "http://localhost:5086/api",
      timeout: 30000,
    });

    // Auth interceptor
    this.api.interceptors.request.use((config) => {
      const token = localStorage.getItem("pmcrms_token");
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    // Error handling interceptor  
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          this.logout();
          window.location.href = "/login";
        }
        return Promise.reject(error);
      }
    );
  }
}
```

## Logging & Monitoring Standards

### Backend Logging with Serilog
- **Configuration**: Set up in `Program.cs` with file and console sinks
- **File Location**: `logs/pmcrms-{Date}.log` with daily rolling
- **Log Levels**: Use appropriate levels (Information, Warning, Error, Debug)
- **Structured Logging**: Include contextual information (UserId, ActionName)

```csharp
// Program.cs Serilog setup
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/pmcrms-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Controller logging examples
_logger.LogInformation("User {UserId} attempting login with identifier {Identifier}", 
    user.Id, request.Identifier);

_logger.LogWarning("Invalid OTP attempt for {Identifier}. Attempt count: {AttemptCount}", 
    otpVerification.Identifier, otpVerification.AttemptCount);

_logger.LogError(ex, "Database error while creating application for user {UserId}", 
    User.FindFirst("user_id")?.Value);
```

### Frontend Error Handling
- Use axios interceptors for global error handling
- Implement user-friendly error messages
- Log errors to console in development mode

```typescript
// Error handling in components
try {
  const response = await apiService.createApplication(formData);
  setSuccess("Application created successfully");
} catch (error) {
  console.error("Error creating application:", error);
  setError(error.response?.data?.message || "Failed to create application");
}
```

## Database Patterns & Conventions

### Entity Framework Conventions
- **Connection String**: PostgreSQL in `appsettings.json`
- **Migrations**: Use descriptive names (`dotnet ef migrations add AddUserRoleToApplications`)
- **Seed Data**: Implement in `PMCRMSDbContext.SeedData()` method
- **Relationships**: Configure in `OnModelCreating()` with proper delete behavior

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure relationships
    modelBuilder.Entity<Application>(entity =>
    {
        entity.HasIndex(e => e.ApplicationNumber).IsUnique();
        entity.Property(e => e.Type).HasConversion<int>();
        
        entity.HasOne(e => e.Applicant)
            .WithMany(e => e.Applications)
            .HasForeignKey(e => e.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);
    });
}

// Audit field updates
private void UpdateAuditFields()
{
    var entries = ChangeTracker.Entries<BaseEntity>();
    foreach (var entry in entries)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedDate = DateTime.UtcNow;
                break;
            case EntityState.Modified:
                entry.Entity.UpdatedDate = DateTime.UtcNow;
                break;
        }
    }
}
```

## Authentication & Security Patterns

### OTP-Based Authentication Flow
1. **Send OTP**: Generate 6-digit code, save to `OtpVerifications` table with 10-minute expiry
2. **Verify OTP**: Validate code, create/find user, generate JWT token
3. **JWT Configuration**: Use HS256 algorithm, 24-hour expiry, include user claims
4. **Token Storage**: Store in localStorage as `pmcrms_token` and `pmcrms_user`

### Security Best Practices
- **JWT Claims**: Include `user_id`, `role`, `email`, `phone` claims
- **CORS**: Configure specific origins in `appsettings.json`
- **Authorization**: Use role-based attributes on controllers
- **Input Validation**: Implement data annotations and model validation
- **SQL Injection**: Use EF Core parameterized queries (never raw SQL)

## Development Workflows

### Backend Development Commands
```bash
# Project setup
cd "PMCRMS/backend/PMCRMS.API"
dotnet restore
dotnet build

# Database operations  
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database drop --force  # Reset database

# Development server
dotnet run  # Starts on http://localhost:5086
dotnet watch run  # Hot reload enabled

# Testing
dotnet test
dotnet test --logger "console;verbosity=detailed"
```

### Frontend Development Commands
```bash
# Project setup
cd "PMCRMS/frontend"
npm install
npm audit fix

# Development server
npm run dev  # Starts on http://localhost:5173
npm run build
npm run preview  # Preview production build

# Code quality
npm run lint
npm run lint:fix
```

## PMC Design System & Styling

### CSS Custom Properties
- Use PMC color variables: `--pmc-primary`, `--pmc-gray-800`, etc.
- Implement professional component classes: `.pmc-button`, `.pmc-card`, `.pmc-table`
- Follow consistent spacing and typography scales

### Component Styling Patterns
```css
/* Use PMC design system classes */
.pmc-button-primary {
  background-color: var(--pmc-primary);
  color: #ffffff;
  padding: 12px 24px;
  border-radius: 8px;
  font-weight: 600;
  transition: all 0.2s ease-in-out;
}

.pmc-form-container {
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
  border: 1px solid var(--pmc-gray-200);
}
```

## Common Development Tasks

### Adding New API Endpoint
1. Create DTO classes in `DTOs/` folder
2. Add method to appropriate controller with proper attributes
3. Implement error handling and logging
4. Update Swagger documentation
5. Add corresponding frontend service method

### Adding New Database Entity
1. Create model class inheriting `BaseEntity`
2. Add DbSet to `PMCRMSDbContext`
3. Configure relationships in `OnModelCreating()`
4. Generate migration: `dotnet ef migrations add AddNewEntity`
5. Update seed data if needed

### Adding New Frontend Page
1. Create component in `src/pages/`
2. Add route in `App.tsx`
3. Implement proper TypeScript interfaces
4. Add navigation link if needed
5. Use PMC design system classes

### Role-Based Feature Implementation
1. Update `UserRole` enum in backend
2. Modify seed data in `PMCRMSDbContext`
3. Add authorization attributes to controllers
4. Update frontend role checking logic
5. Test with different user roles

## Configuration Management

### Backend Configuration (`appsettings.json`)
- **JWT Settings**: SecretKey (32+ chars), Issuer, Audience, ExpiryHours
- **Database**: PostgreSQL connection string
- **Email**: SMTP configuration for OTP delivery
- **File Upload**: Size limits, allowed extensions, storage path
- **Payment**: BillDesk integration settings
- **CORS**: Allowed origins for frontend access

### Frontend Configuration
- **Environment Variables**: Use `.env` files for different environments
- **API Base URL**: Configure in `apiService.ts`
- **Build Configuration**: Vite config for development and production
- **TypeScript**: Strict mode enabled, proper path resolution

## Performance & Best Practices

### Backend Optimization
- Use async/await for all database operations
- Implement proper pagination for large datasets
- Use projection (Select) to limit returned data
- Add database indexes for frequently queried columns
- Implement caching for static data (roles, statuses)

### Frontend Optimization  
- Use React.memo for expensive components
- Implement code splitting with lazy loading
- Optimize bundle size with proper imports
- Use TanStack Query for server state caching
- Implement proper loading states and error boundaries

### Database Performance
- Use EF Core Include() for related data
- Implement proper indexing strategy
- Use database constraints for data integrity
- Regular backup and maintenance procedures
- Monitor query performance in development

## Integration Points

### Authentication Flow
1. Frontend calls `/api/auth/send-otp` with email/phone
2. Backend generates 6-digit OTP, saves to `OtpVerifications` table
3. Frontend calls `/api/auth/verify-otp` with OTP code
4. Backend validates OTP, creates/finds user, returns JWT token
5. Frontend stores token and user data in `AuthContext`

### API Communication
- **Base URL**: Backend runs on `https://localhost:7001` or `http://localhost:5086`
- **CORS**: Configured for `http://localhost:5173` (Vite dev server)
- **Headers**: JWT token in `Authorization: Bearer {token}` header
- **Error Handling**: Axios interceptors handle token expiration

## Current Development Status (65% Complete)

### ✅ Working Components
- JWT authentication with OTP verification
- Basic CRUD for applications
- Database models and migrations
- Frontend auth flow and routing
- Swagger API documentation

### 🚧 In Progress / TODO
- File upload system (`DocumentController`)
- Payment integration (`PaymentController`) 
- Email/SMS OTP delivery services
- Repository pattern implementation
- Comprehensive error handling

## Key Files for Understanding

### Backend Architecture
- `Program.cs` - DI container, JWT config, CORS, database setup
- `Data/PMCRMSDbContext.cs` - EF Core context with all model configurations
- `Controllers/AuthController.cs` - OTP-based authentication implementation
- `Models/User.cs` - User roles and entity structure

### Frontend Architecture  
- `src/App.tsx` - Routing and protected route setup
- `src/contexts/AuthContext.tsx` - Authentication state management
- `src/services/apiService.ts` - Centralized API communication

### Configuration
- `appsettings.json` - JWT, CORS, and database connection settings
- `package.json` - Frontend dependencies and build scripts