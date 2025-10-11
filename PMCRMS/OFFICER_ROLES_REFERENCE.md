# Officer Roles - Frontend & Backend Reference

## ✅ Exact Role Values (Case-Sensitive)

Both frontend and backend now use these **exact** role values:

```
JuniorArchitect
AssistantArchitect
JuniorLicenceEngineer
AssistantLicenceEngineer
JuniorStructuralEngineer
AssistantStructuralEngineer
JuniorSupervisor1
AssistantSupervisor1
JuniorSupervisor2
AssistantSupervisor2
ExecutiveEngineer
CityEngineer
Clerk
```

## Frontend Implementation

**File:** `PMCRMS/frontend/src/pages/admin/OfficerManagementPage.tsx`

```typescript
const OFFICER_DESIGNATIONS = [
  "JuniorArchitect",
  "AssistantArchitect",
  "JuniorLicenceEngineer",
  "AssistantLicenceEngineer",
  "JuniorStructuralEngineer",
  "AssistantStructuralEngineer",
  "JuniorSupervisor1",
  "AssistantSupervisor1",
  "JuniorSupervisor2",
  "AssistantSupervisor2",
  "ExecutiveEngineer",
  "CityEngineer",
  "Clerk",
];
```

## Backend Implementation

**File:** `PMCRMS/backend/PMCRMS.API/Models/User.cs`

```csharp
public enum UserRole
{
    Admin = 1,
    User = 2,
    JuniorArchitect = 3,
    AssistantArchitect = 4,
    JuniorLicenceEngineer = 5,
    AssistantLicenceEngineer = 6,
    JuniorStructuralEngineer = 7,
    AssistantStructuralEngineer = 8,
    JuniorSupervisor1 = 9,
    AssistantSupervisor1 = 10,
    JuniorSupervisor2 = 11,
    AssistantSupervisor2 = 12,
    ExecutiveEngineer = 13,
    CityEngineer = 14,
    Clerk = 15
}
```

## API Contract

### Request (Frontend → Backend)

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "role": "JuniorArchitect"
}
```

**Note:** `employeeId` is optional and will be auto-generated if not provided.

### Backend Processing

1. **Receives role as string**: `"JuniorArchitect"`
2. **Validates role exists**: Checks against valid enum values
3. **Parses to enum**: `UserRole.JuniorArchitect`
4. **Auto-generates Employee ID**: `"JA-789234"` (if not provided)
5. **Creates invitation** with parsed enum value
6. **Sends email** with temporary password

### Error Response (Invalid Role)

```json
{
  "success": false,
  "message": "Invalid role: SomeInvalidRole. Valid roles are: JuniorArchitect, AssistantArchitect, JuniorLicenceEngineer, AssistantLicenceEngineer, JuniorStructuralEngineer, AssistantStructuralEngineer, JuniorSupervisor1, AssistantSupervisor1, JuniorSupervisor2, AssistantSupervisor2, ExecutiveEngineer, CityEngineer, Clerk"
}
```

## Key Changes Made

### 1. DTO Updated

- Changed `Role` from `UserRole` enum to `string`
- Backend now accepts role as string and validates/parses it

### 2. Controller Enhanced

- Added role validation with `Enum.TryParse<UserRole>()`
- Provides clear error message with list of valid roles
- Uses parsed `userRole` enum variable throughout

### 3. Employee ID Auto-Generation

- Format: `[RoleInitials]-[6DigitTimestamp]`
- Examples:
  - `JuniorArchitect` → `JA-456789`
  - `ExecutiveEngineer` → `EE-123456`
  - `Clerk` → `C-987654`

## Testing

### Valid Request Examples

```bash
# Junior Architect
{
  "name": "Jane Smith",
  "email": "jane@example.com",
  "role": "JuniorArchitect"
}

# Executive Engineer
{
  "name": "Bob Johnson",
  "email": "bob@example.com",
  "role": "ExecutiveEngineer"
}
```

### Invalid Request Examples

```bash
# Wrong case
{
  "role": "juniorarchitect"  # ❌ Will fail - case matters
}

# Non-existent role
{
  "role": "SeniorArchitect"  # ❌ Will fail - not in enum
}

# Missing required field
{
  "name": "John",
  # ❌ Will fail - email is required
}
```

---

**Last Updated:** October 11, 2025  
**Status:** ✅ Production Ready
