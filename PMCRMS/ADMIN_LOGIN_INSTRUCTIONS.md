# 🔐 Admin Login Instructions

## How to Login as System Administrator

### 📍 **Which Screen to Use?**

**Use the Officer Login Page:** `/officer-login`

The system is smart enough to detect if you're an admin based on your email address and will automatically use the correct login endpoint.

---

## 🎯 **Step-by-Step Login Process**

### 1. Navigate to Officer Login Page

- **URL:** `http://localhost:5173/officer-login` (Development)
- **OR** Click the "Officer Portal" button from the User Login page

### 2. Enter Admin Credentials

**Default System Admin:**

- **Email:** `admin@gmail.com`
- **Password:** `admin@123`

### 3. Login Flow

The system will:

1. Detect that you're using admin@gmail.com
2. Call the `/api/auth/admin-login` endpoint (SystemAdmins table)
3. Verify your credentials in the **SystemAdmins** table
4. Generate a JWT token with admin claims
5. Redirect you to `/admin` dashboard

---

## 🔄 **Login Endpoints Mapping**

| User Type            | Email Pattern     | Frontend Page    | Backend Endpoint                              | Database Table |
| -------------------- | ----------------- | ---------------- | --------------------------------------------- | -------------- |
| **System Admin**     | `admin@gmail.com` | `/officer-login` | `/api/auth/admin-login`                       | `SystemAdmins` |
| **Officers**         | Any other email   | `/officer-login` | `/api/auth/officer-login`                     | `Officers`     |
| **Users/Applicants** | Any email         | `/login`         | `/api/auth/send-otp` + `/api/auth/verify-otp` | `Users`        |

---

## 📋 **What Happens After Login?**

### Admin Login Success:

1. ✅ Token stored in `localStorage` (key: `pmcrms_token`)
2. ✅ User data stored in `localStorage` (key: `pmcrms_user`)
3. ✅ Auto-redirected to `/admin` dashboard
4. ✅ Role detected as `"Admin"`

### Officer Login Success:

1. ✅ Token stored in `localStorage`
2. ✅ User data stored in `localStorage`
3. ✅ Auto-redirected to `/dashboard`
4. ✅ Role detected as officer type (e.g., `"JuniorArchitect"`)

---

## 🚀 **First Time Setup**

The System Admin account is **automatically created** when the application starts for the first time:

**Auto-Created Admin:**

- **Name:** System Administrator
- **Email:** admin@gmail.com
- **Password:** admin@123
- **Employee ID:** ADMIN001
- **IsSuperAdmin:** true
- **Department:** Administration

### Code Location:

```
File: PMCRMS.API/Services/DataSeeder.cs
Method: EnsureSystemAdminExistsAsync()
Called from: Program.cs on startup
```

---

## 🔑 **JWT Token Claims (Admin)**

When you login as admin, your JWT token contains:

```json
{
  "user_id": "1",
  "admin_id": "1",
  "role": "Admin",
  "employee_id": "ADMIN001",
  "is_super_admin": "true",
  "user_type": "SystemAdmin",
  "email": "admin@gmail.com",
  "name": "System Administrator"
}
```

---

## 🛠️ **Admin Capabilities**

After logging in as admin, you can:

✅ **Manage Officers** - Invite, update, deactivate officers
✅ **View All Applications** - See all building permit applications
✅ **Update Form Fees** - Modify application fees
✅ **System Configuration** - Manage system-wide settings
✅ **View Analytics** - Access dashboard and reports

---

## ⚠️ **Important Security Notes**

### 🔒 **Change Default Password**

After first login, you should:

1. Navigate to Settings/Profile
2. Change password from `admin@123` to a strong password
3. The password should be at least 8 characters with:
   - Uppercase letters
   - Lowercase letters
   - Numbers
   - Special characters

### 🚫 **Account Lockout**

- After **5 failed login attempts**, account will be locked for **30 minutes**
- Login attempts are tracked and reset on successful login

### 🔐 **Secure Storage**

- Passwords are hashed using **BCrypt**
- JWT tokens expire after **24 hours**
- No plain-text passwords stored in database

---

## 🧪 **Testing Admin Login**

### Using Browser:

1. Open `http://localhost:5173/officer-login`
2. Enter: `admin@gmail.com` / `admin@123`
3. Click "Access Officer Portal"
4. You should be redirected to `/admin`

### Using API Directly:

```bash
POST http://localhost:5000/api/auth/admin-login
Content-Type: application/json

{
  "email": "admin@gmail.com",
  "password": "admin@123"
}
```

**Expected Response:**

```json
{
  "success": true,
  "message": "Welcome back, System Administrator!",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "guid-here",
    "expiresAt": "2025-10-12T...",
    "user": {
      "id": 1,
      "name": "System Administrator",
      "email": "admin@gmail.com",
      "role": "Admin",
      "employeeId": "ADMIN001",
      "isActive": true
    }
  }
}
```

---

## 📊 **Database Verification**

### Check if Admin Exists:

```sql
SELECT * FROM public."SystemAdmins" WHERE "Email" = 'admin@gmail.com';
```

**Expected Output:**
| Id | Name | Email | EmployeeId | IsSuperAdmin | IsActive |
|----|------|-------|------------|--------------|----------|
| 1 | System Administrator | admin@gmail.com | ADMIN001 | true | true |

---

## ❌ **Troubleshooting**

### Issue: "Invalid email or password"

**Solutions:**

1. Ensure backend is running (`dotnet run`)
2. Check if SystemAdmin was created (query database)
3. Verify you're using correct credentials
4. Check backend logs for password verification details

### Issue: "Password not set for this account"

**Solutions:**

1. Check if `PasswordHash` column has a value in database
2. Restart the application to trigger auto-creation
3. Check application startup logs for "System Admin created successfully"

### Issue: "Account is locked"

**Solution:**

- Wait 30 minutes OR
- Run SQL: `UPDATE public."SystemAdmins" SET "LockedUntil" = NULL, "LoginAttempts" = 0 WHERE "Email" = 'admin@gmail.com';`

### Issue: Redirected to wrong page after login

**Solution:**

1. Clear browser localStorage: `localStorage.clear()`
2. Check if `role` in response is "Admin"
3. Verify frontend routing in `App.tsx`

---

## 📞 **Support**

If you encounter any issues:

1. Check backend logs: `D:\Ezybricks\PMCRMS\backend\PMCRMS.API\logs\`
2. Check browser console for errors
3. Verify database connection is working
4. Ensure all migrations are applied: `dotnet ef database update`

---

**Last Updated:** 2025-10-11  
**Version:** 1.0  
**Status:** ✅ Production Ready
