# Junior Engineer Dashboard Filter Fix

## Problem Description

The Junior Engineer dashboard had two filter issues:

### Issue 1: Schedule Appointment Tab

- Applications that already had appointments scheduled were still showing in the "Schedule Appointment" tab
- Applications where payment was completed (and moved to Clerk stage) were still appearing in the scheduling list

User reported: "if scheduled appointment is true then do not show in scheduled appoint list"

### Issue 2: JE Pending Tab (Follow-up Issue)

- The fix for Issue 1 incorrectly removed applications from the "JE Pending" tab
- Applications with scheduled appointments should appear in "JE Pending" tab until JE completes their work (document verification + digital signature)

User reported: "you removed from junior engineer pending tab also. show if scheduled appointment is true and jr engineer approved is false"

## Root Cause

### Schedule Appointment Tab Issue

The filter logic in `OfficerDashboard.tsx` was checking the string-based `currentStage` field instead of the actual boolean `hasAppointment` field that the backend provides via the `JEWorkflowStatusDto`.

### JE Pending Tab Issue

The initial fix added `hasCompletedPayment` checks to both tabs, which incorrectly filtered out applications from the "JE Pending" tab. The JE Pending tab should show ALL applications where:

- Appointment is scheduled (`hasAppointment = true`)
- JE has NOT completed their workflow (document verification + digital signature)

The correct condition is to check if the JE workflow is complete, not if payment is complete.

### Tab Count Badge Issue (Third Issue)

After fixing the filter logic, the tab count badges were showing incorrect numbers. For example, showing "3" when there were actually 0 applications to schedule. This was because:

- The `getFilteredApplications()` function used the NEW workflow-based logic (`app.workflow?.hasAppointment`)
- The tab count calculation still used the OLD string-based logic (`app.currentStage === "Appointment Scheduled"`)

These two must always be in sync, otherwise the counts won't match the actual displayed data.

### Old (Incorrect) Logic

```typescript
const isAppointmentScheduled = app.currentStage === "Appointment Scheduled";
if (activeTab === "tab1") {
  return !isAppointmentScheduled;
}
```

This approach was unreliable because:

1. It depended on string matching which is prone to inconsistencies
2. It didn't check the actual database field `JEAppointmentScheduled`
3. It didn't exclude payment-completed applications

## Solution Implemented

### 1. Schedule Appointment Tab Filter (Lines 306-322)

**Logic**: Show applications that need an appointment to be scheduled.

Exclude applications that:

- Already have appointments scheduled (`hasAppointment = true`), OR
- Have completed payment and moved forward in the workflow

```typescript
if (activeTab === "tab1") {
  // Schedule Appointment tab - only show applications WITHOUT scheduled appointments
  return applications.filter((app) => {
    const hasScheduledAppointment = app.workflow?.hasAppointment === true;

    // Exclude applications that already have appointments or have completed payment/moved forward
    const hasCompletedPayment =
      app.status === "PaymentCompleted" ||
      app.status === "CLERK_PENDING" ||
      app.currentStage === "Payment Completed" ||
      app.currentStage?.includes("Clerk") ||
      app.currentStage?.includes("PAYMENT") ||
      app.currentStage?.includes("EXECUTIVE_ENGINEER_SIGN");

    return !hasScheduledAppointment && !hasCompletedPayment;
  });
}
```

### 2. JE Pending Tab Filter (Lines 323-336)

**Logic**: Show applications where appointment is scheduled but JE work is not yet complete.

Show applications that:

- Have appointments scheduled (`hasAppointment = true`), AND
- JE workflow is NOT yet completed (digital signature not applied OR not forwarded to AE)

```typescript
else {
  // JE Pending tab - appointment scheduled but JE workflow not completed
  // Show if: appointment scheduled AND (JE hasn't approved OR digital signature not applied)
  return applications.filter((app) => {
    const hasScheduledAppointment = app.workflow?.hasAppointment === true;
    const jeWorkflowCompleted =
      app.workflow?.digitalSignatureApplied === true &&
      app.currentStatus === "ASSISTANT_ENGINEER_PENDING";

    // Show applications where appointment is scheduled and JE work is not complete
    return hasScheduledAppointment && !jeWorkflowCompleted;
  });
}
```

### 3. Tab Count Badge Calculation (Lines 677-701)

**Critical**: The count calculation MUST use the exact same logic as `getFilteredApplications()` to ensure the badge numbers match the displayed data.

**Schedule Appointment Tab Count:**

```typescript
if (tab.id === "tab1") {
  // Schedule Appointment tab - same logic as getFilteredApplications()
  tabCount = applications.filter((app) => {
    const hasScheduledAppointment = app.workflow?.hasAppointment === true;
    const hasCompletedPayment =
      app.status === "PaymentCompleted" ||
      app.status === "CLERK_PENDING" ||
      app.currentStage === "Payment Completed" ||
      app.currentStage?.includes("Clerk") ||
      app.currentStage?.includes("PAYMENT") ||
      app.currentStage?.includes("EXECUTIVE_ENGINEER_SIGN");
    return !hasScheduledAppointment && !hasCompletedPayment;
  }).length;
}
```

**JE Pending Tab Count:**

```typescript
else if (tab.id === "tab2") {
  // JE Pending tab - same logic as getFilteredApplications()
  tabCount = applications.filter((app) => {
    const hasScheduledAppointment = app.workflow?.hasAppointment === true;
    const jeWorkflowCompleted =
      app.workflow?.digitalSignatureApplied === true &&
      app.currentStatus === "ASSISTANT_ENGINEER_PENDING";
    return hasScheduledAppointment && !jeWorkflowCompleted;
  }).length;
}
```

### 4. Improved Type Safety (Lines 21-52)

Enhanced the `Application` interface with proper workflow field typing:

```typescript
workflow?: {
  hasAppointment?: boolean;
  appointmentDate?: string;
  isAppointmentCompleted?: boolean;
  allDocumentsVerified?: boolean;
  digitalSignatureApplied?: boolean;
  currentStage?: string;
};
```

## Backend Data Flow

The fix leverages the existing backend infrastructure:

1. **Database Field**: `PositionApplications.JEAppointmentScheduled` (boolean)
2. **Service Layer**: `JEWorkflowService.GetWorkflowStatusAsync()` populates the DTO:
   ```csharp
   HasAppointment = application.JEAppointmentScheduled
   ```
3. **DTO**: `JEWorkflowStatusDto.HasAppointment` (boolean)
4. **Frontend**: `workflow.hasAppointment` is now properly checked

## Files Modified

1. **frontend/src/pages/OfficerDashboard.tsx**
   - Updated `getFilteredApplications()` method (lines 303-336) - Filter logic for both tabs
   - Updated tab count calculation (lines 677-701) - Count badges must match filter logic
   - Enhanced `Application` interface type definition (lines 21-52)

## Testing Checklist

**Schedule Appointment Tab:**

- [ ] Login as Junior Engineer officer
- [ ] Navigate to dashboard
- [ ] Verify "Schedule Appointment" tab shows ONLY applications that need appointments
- [ ] Verify applications with `hasAppointment=true` are NOT shown in "Schedule Appointment" tab
- [ ] Verify applications with status `PaymentCompleted` or `CLERK_PENDING` are NOT shown

**JE Pending Tab:**

- [ ] Verify "JE Pending" tab shows applications with scheduled appointments
- [ ] Verify it shows applications where JE hasn't completed digital signature
- [ ] Verify it hides applications that have been forwarded to AE (after JE signature)
- [ ] Test with various application statuses (appointment scheduled, docs verified, signature pending)

## Impact

- **No backend changes required** - backend was already correctly tracking appointment status
- **No database changes required** - field `JEAppointmentScheduled` already exists
- **Frontend only fix** - corrected filter logic to use the right data field
- **Improved reliability** - using boolean field instead of string comparison

## Additional Notes

- The backend correctly queries and returns the `HasAppointment` field via the API endpoint: `GET /api/JEWorkflow/officer/{officerId}/applications`
- The fix ensures consistency between what the database tracks and what the UI displays
- Payment completion checks cover multiple status variations for robustness

## Date

October 14, 2025

## Revision History

- **Initial Fix (December 2024)**: Fixed Schedule Appointment tab filter to use `hasAppointment` field
- **Follow-up Fix (October 14, 2025)**: Corrected JE Pending tab to show scheduled appointments until JE completes their workflow
- **Count Badge Fix (October 14, 2025)**: Fixed tab count badges to match the filter logic - was showing incorrect counts because it used old string-based logic while filters used new workflow-based logic
