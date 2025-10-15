# Certificate Images

This directory contains images used in the license certificate generation.

## Required Files

### logo.png

- **Description**: Pune Municipal Corporation (PMC) logo
- **Usage**: Displayed on the top-left corner of the certificate
- **Recommended Size**: 200x200 pixels minimum
- **Format**: PNG with transparent background preferred
- **Source**: The PMC logo with the horseman and "पुणे महानगरपालिका" text

**Note**: Please save the PMC logo image (the one with the horseman on orange background) as `logo.png` in this directory.

### Profile Photos

Profile photos are **automatically fetched** from the `SEDocuments` table where:

- `DocumentType = ProfilePicture (8)`
- `ApplicationId` matches the certificate being generated

The system will:

1. First try to use `FileContent` (if stored in database)
2. Fall back to `FilePath` (if stored as file)
3. Use a placeholder if no photo is found

## Certificate Layout

```
┌─────────────────────────────────────────────────────────┐
│  [Logo]         पुणे महानगरपालिका         [Profile]   │
│                परवाना                                    │
├─────────────────────────────────────────────────────────┤
│  Certificate Number: PMC/ARCH/123/2025-2028            │
│  Name: [Applicant Name]                                │
│  Address: [Full Address]                               │
│  ...certificate content...                             │
└─────────────────────────────────────────────────────────┘
```

## Testing

After placing the logo:

1. Complete a payment for an application
2. Check logs for certificate generation
3. Verify the certificate in `SEDocuments` table
4. Download and view the certificate PDF
