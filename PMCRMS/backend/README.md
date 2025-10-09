# PMCRMS Backend API

## Overview

The PMCRMS (Permit Management and Certificate Recommendation Management System) Backend API is built using **.NET 8** with **Entity Framework Core** and **PostgreSQL**.

## Features

- **Authentication**: OTP-based login system
- **Authorization**: JWT token-based authentication
- **Database**: PostgreSQL with Entity Framework Core
- **Logging**: Serilog integration
- **API Documentation**: Swagger/OpenAPI
- **CORS**: Configured for frontend integration

## Prerequisites

- .NET 8 SDK
- PostgreSQL database
- IDE (Visual Studio, VS Code, etc.)

## Getting Started

### 1. Database Setup

1. Install PostgreSQL and create a database named `PMCRMS_DB`
2. Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=PMCRMS_DB;Username=postgres;Password=your_password"
   }
   ```

### 2. Environment Configuration

1. Copy `.env.example` to `.env`
2. Update the environment variables as needed

### 3. Database Migration

```bash
# Apply migrations to create database schema
dotnet ef database update
```

### 4. Run the Application

```bash
# Restore packages and run
dotnet restore
dotnet run
```

The API will be available at:

- **HTTPS**: https://localhost:7001
- **HTTP**: http://localhost:5000
- **Swagger UI**: https://localhost:7001 (root path)

## Project Structure

```
PMCRMS.API/
├── Controllers/          # API Controllers
├── Data/                # Database Context
├── DTOs/                # Data Transfer Objects
├── Models/              # Entity Models
├── Services/            # Business Logic Services
├── Repositories/        # Data Access Layer
├── Migrations/          # EF Core Migrations
├── logs/                # Application Logs
└── uploads/             # File Uploads
```

## Database Schema

- **Users**: System users with role-based access
- **Applications**: Permit/certificate applications
- **ApplicationDocuments**: Uploaded documents
- **ApplicationStatus**: Status tracking history
- **ApplicationComments**: Comments and notes
- **Payments**: Payment transactions
- **OtpVerifications**: OTP management

## API Endpoints

### Authentication

- `POST /api/auth/send-otp` - Send OTP for login/registration
- `POST /api/auth/verify-otp` - Verify OTP and authenticate
- `GET /api/auth/test` - Health check endpoint

### Applications (TODO)

- `GET /api/applications` - Get applications list
- `POST /api/applications` - Create new application
- `GET /api/applications/{id}` - Get application details
- `PUT /api/applications/{id}` - Update application
- `PUT /api/applications/{id}/status` - Update application status

### Users (TODO)

- `GET /api/users/profile` - Get user profile
- `PUT /api/users/profile` - Update user profile

### Documents (TODO)

- `POST /api/documents/upload` - Upload document
- `GET /api/documents/{id}` - Download document

### Payments (TODO)

- `POST /api/payments/initiate` - Initiate payment
- `POST /api/payments/verify` - Verify payment status

## Configuration

### JWT Settings

```json
"JwtSettings": {
  "SecretKey": "your-secret-key-here",
  "Issuer": "PMCRMS.API",
  "Audience": "PMCRMS.Client",
  "ExpiryHours": 24
}
```

### CORS Settings

```json
"CorsSettings": {
  "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
}
```

## Development

### Adding New Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Logging

- Logs are written to console and files (`logs/pmcrms-{Date}.log`)
- Log level can be configured in `appsettings.json`

## Error Handling

- Global exception handling middleware
- Structured error responses
- Proper HTTP status codes

## Security

- JWT token authentication
- CORS protection
- Input validation
- SQL injection prevention through EF Core

## Next Steps

1. ✅ Project setup and database schema
2. ✅ Authentication system (OTP-based)
3. 🔄 Complete JWT token generation
4. 📋 Application management endpoints
5. 📋 File upload system
6. 📋 Payment integration (EaseBuzz)
7. 📋 Email/SMS notification service
8. 📋 Role-based authorization
9. 📋 Workflow management
10. 📋 Certificate generation

## Architecture

- **Repository Pattern**: Data access abstraction
- **Service Layer**: Business logic separation
- **DTOs**: Data transfer objects for API
- **Dependency Injection**: Built-in .NET DI container
- **Entity Framework**: ORM for database operations
