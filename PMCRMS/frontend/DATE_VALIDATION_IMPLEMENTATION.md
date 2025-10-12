# Date Validation Implementation

## Overview

This document describes the date validation rules implemented in the Position Registration form to ensure data integrity and business logic compliance.

## Validation Rules

### 1. Birth Date Validation

**Rule**: User must be at least 18 years old

**Implementation**:

- Maximum selectable date: Current date minus 18 years
- Example: If today is October 12, 2025, the maximum selectable birth date is October 12, 2007
- Dates after this maximum are disabled in the date picker

**Code Location**: Line ~1878-1890 in `PositionRegistrationPage.tsx`

```tsx
<input
  type="date"
  max={getMaxBirthDate()} // Restricts to 18 years ago
  // ... other props
/>
```

**Helper Function**:

```tsx
const getMaxBirthDate = () => {
  const today = new Date();
  const maxDate = new Date(
    today.getFullYear() - 18,
    today.getMonth(),
    today.getDate()
  );
  return maxDate.toISOString().split("T")[0];
};
```

---

### 2. Qualification Passing Year & Month Validation

**Rules**:

- Passing year cannot be greater than the current year
- If current year is selected, passing month cannot be greater than current month

**Implementation**:

- Year input has `max` attribute set to current year
- Month dropdown dynamically disables future months when current year is selected

**Code Location**: Line ~3147-3175 in `PositionRegistrationPage.tsx`

**Year Input**:

```tsx
<input
  type="number"
  min="1950"
  max={new Date().getFullYear()} // Cannot select future years
  // ... other props
/>
```

**Month Dropdown** (Dynamic Validation):

```tsx
<select>
  {monthOptions.map((month, idx) => {
    const monthValue = idx + 1;
    const yearOfPassing = qual.yearOfPassing.split("-")[0];
    const isCurrentYear =
      yearOfPassing && parseInt(yearOfPassing) === getCurrentYear();
    const isDisabled = isCurrentYear && monthValue > getCurrentMonth();

    return (
      <option
        key={idx}
        value={monthValue}
        disabled={isDisabled ? true : undefined}
      >
        {month}
      </option>
    );
  })}
</select>
```

**Example Scenario**:

- Today: October 12, 2025
- User selects year: 2025
- Available months: January to October
- Disabled months: November, December

**Helper Functions**:

```tsx
const getCurrentYear = () => {
  return new Date().getFullYear();
};

const getCurrentMonth = () => {
  return new Date().getMonth() + 1; // JavaScript months are 0-indexed
};
```

---

### 3. Experience Date Validation

**Rule**: Experience start and end dates cannot be in the future

**Implementation**:

- Both "From Date" and "To Date" have `max` attribute set to current date
- Dates after today are disabled in both date pickers

**Code Location**: Line ~3388-3439 in `PositionRegistrationPage.tsx`

**From Date Input**:

```tsx
<input
  type="date"
  max={getMaxExperienceDate()} // Cannot select future dates
  // ... other props
/>
```

**To Date Input**:

```tsx
<input
  type="date"
  max={getMaxExperienceDate()} // Cannot select future dates
  // ... other props
/>
```

**Helper Function**:

```tsx
const getMaxExperienceDate = () => {
  const today = new Date();
  return today.toISOString().split("T")[0];
};
```

---

## User Experience

### Visual Feedback

1. **Date Pickers**: Future dates are grayed out and cannot be selected
2. **Month Dropdown**: Future months are disabled when current year is selected
3. **Error Messages**: Existing validation error messages still apply for required fields

### Browser Compatibility

- HTML5 date input `max` attribute is supported in all modern browsers
- Option `disabled` attribute works across all browsers
- Graceful fallback: Even if HTML5 validation is bypassed, backend validation should catch invalid dates

---

## Testing Checklist

### Birth Date

- [ ] Cannot select a date less than 18 years ago
- [ ] Can select exactly 18 years ago
- [ ] Can select dates older than 18 years

### Qualification

- [ ] Cannot select year greater than current year
- [ ] Can select current year
- [ ] When current year selected, cannot select future months
- [ ] When current year selected, can select current month
- [ ] When past year selected, all months are available

### Experience

- [ ] Cannot select future dates for "From Date"
- [ ] Cannot select future dates for "To Date"
- [ ] Can select today's date
- [ ] Can select past dates
- [ ] Experience calculation still works correctly

---

## Technical Details

### Date Format

- Input value format: `YYYY-MM-DD` (ISO 8601 date string)
- Internal storage format: `YYYY-MM-DDTHH:MM:SS.000Z` (ISO 8601 UTC timestamp)

### Timezone Handling

- All date calculations use local browser time
- Dates are converted to UTC when submitting to API
- Max date calculations account for current date in user's timezone

### Performance

- Helper functions are defined at component level (recalculated on each render)
- Calculations are lightweight (simple date arithmetic)
- No external dependencies required

---

## Backend Considerations

### Additional Validation Recommended

While frontend validation prevents most invalid entries, backend should also validate:

```csharp
// Birth date validation
if (DateTime.UtcNow.Year - dateOfBirth.Year < 18)
{
    errors.Add("Applicant must be at least 18 years old");
}

// Qualification year validation
if (yearOfPassing > DateTime.UtcNow.Year)
{
    errors.Add("Qualification year cannot be in the future");
}

// Qualification month validation
if (yearOfPassing == DateTime.UtcNow.Year && passingMonth > DateTime.UtcNow.Month)
{
    errors.Add("Qualification month cannot be in the future");
}

// Experience date validation
if (fromDate > DateTime.UtcNow || toDate > DateTime.UtcNow)
{
    errors.Add("Experience dates cannot be in the future");
}
```

---

## Summary

All date validations are now enforced at the UI level using HTML5 date input attributes and dynamic option disabling. This provides immediate feedback to users and prevents invalid data entry before form submission.

**Files Modified**:

- `frontend/src/pages/PositionRegistrationPage.tsx`

**Lines Modified**:

- Birth Date: ~1878-1890
- Qualification Month: ~3147-3175
- Qualification Year: Already had max validation
- Experience Dates: ~3388-3439
- Helper Functions: ~1078-1100
