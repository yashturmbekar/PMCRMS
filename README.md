# PMCRMS - Pune Municipal Corporation Record Management System

## Overview

A comprehensive record management system for Pune Municipal Corporation built with .NET 8 Web API backend and modern frontend technologies.

## Backend Features

- **Authentication & Authorization**: JWT-based authentication with role-based access control
- **User Management**: Multiple user roles (Admin, Junior Engineer, Assistant Engineer, Executive Engineer, City Engineer, Clerk, Applicant)
- **Application Management**: Complete CRUD operations for building permit applications
- **Document Management**: File upload/download with validation and security
- **Status Workflow**: 23-step approval process with role-based transitions
- **Payment Integration**: BillDesk payment gateway integration
- **Email Service**: SendinBlue SMTP integration for notifications
- **Database**: PostgreSQL with Entity Framework Core

## Architecture

- **Backend**: .NET 8 Web API
- **Database**: PostgreSQL
- **Authentication**: JWT Tokens
- **Email**: SendinBlue SMTP
- **Payments**: BillDesk Gateway
- **File Storage**: Local file system (configurable for cloud storage)

## API Endpoints

- **Auth**: `/api/auth` - Authentication and OTP verification
- **Applications**: `/api/applications` - Application CRUD operations
- **Documents**: `/api/documents` - File upload/download management
- **Users**: `/api/users` - User management and profiles
- **Status**: `/api/status` - Workflow status management

## User Roles & Workflow

1. **Applicant**: Submit applications and track status
2. **Junior Engineer**: Initial review and approval
3. **Assistant Engineer**: Secondary review process
4. **Executive Engineer**: Executive approval and digital signatures
5. **City Engineer**: Final approvals and certificate issuance
6. **Clerk**: Administrative processing and payment handling
7. **Admin**: System administration and user management

## Deployment

### GitHub Actions CI/CD

This repository includes GitHub Actions workflow for automated deployment to AWS S3.

#### Required Secrets

To enable automated deployment, add the following secrets to your GitHub repository:

1. Go to your repository on GitHub
2. Navigate to Settings → Secrets and variables → Actions
3. Add the following repository secrets:

```
AWS_ACCESS_KEY_ID          - Your AWS access key ID
AWS_SECRET_ACCESS_KEY      - Your AWS secret access key
AWS_REGION                 - AWS region (e.g., us-east-1)
AWS_S3_BUCKET             - S3 bucket name for deployment
CLOUDFRONT_DISTRIBUTION_ID - (Optional) CloudFront distribution ID for cache invalidation
```

#### Workflow Features

- **Triggers**: Automatically runs on push to `main` or `develop` branches
- **Build Process**: Installs dependencies and builds the project
- **S3 Sync**: Syncs all files except index.html with caching
- **No-Cache HTML**: Uploads index.html with no-cache headers
- **CloudFront**: Optional CloudFront cache invalidation

## Local Development

### Prerequisites

- .NET 8 SDK
- PostgreSQL 12+
- Node.js 18+ (for frontend)

### Backend Setup

1. Clone the repository
2. Navigate to `backend/PMCRMS.API`
3. Update `appsettings.json` with your database connection string
4. Run migrations: `dotnet ef database update`
5. Start the API: `dotnet run`

### Environment Variables

Create a `.env` file in the backend directory with:

```
ConnectionStrings__DefaultConnection=Host=localhost;Database=PMCRMS_DB;Username=postgres;Password=your_password
EmailSettings__SmtpServer=smtp-relay.sendinblue.com
EmailSettings__SmtpPort=587
EmailSettings__FromEmail=contact@invimatic.com
EmailSettings__SmtpUsername=your_sendinblue_username
EmailSettings__SmtpPassword=your_sendinblue_password
PaymentSettings__MerchantId=your_billdesk_merchant_id
PaymentSettings__EncryptionKey=your_billdesk_encryption_key
PaymentSettings__SigningKey=your_billdesk_signing_key
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For support and questions, please contact the development team.
