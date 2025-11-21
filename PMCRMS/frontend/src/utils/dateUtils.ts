/**
 * Parse a date string as local time without timezone conversion
 * This prevents the 5-hour offset issue when dates are converted between UTC and IST
 *
 * @param dateString - ISO format date string (e.g., "2025-10-17T05:22:00")
 * @returns Date object with the exact time specified in the string
 */
export const parseLocalDateTime = (dateString: string): Date => {
  if (!dateString) return new Date();

  if (dateString.includes("T")) {
    // Parse as local datetime without timezone conversion
    const parts = dateString.split("T");
    const dateParts = parts[0].split("-");
    const timeParts = parts[1].split(":");

    return new Date(
      parseInt(dateParts[0]), // year
      parseInt(dateParts[1]) - 1, // month (0-indexed)
      parseInt(dateParts[2]), // day
      parseInt(timeParts[0]), // hours
      parseInt(timeParts[1]), // minutes
      parseInt(timeParts[2] || "0") // seconds
    );
  }

  // Fallback to standard parsing
  return new Date(dateString);
};

/**
 * Format a date object to ISO string without timezone conversion
 *
 * @param date - Date object to format
 * @param includeSeconds - Whether to include seconds in the output (default: true)
 * @returns ISO format string (e.g., "2025-10-17T05:22:00")
 */
export const formatLocalDateTime = (
  date: Date,
  includeSeconds: boolean = true
): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  const hours = String(date.getHours()).padStart(2, "0");
  const minutes = String(date.getMinutes()).padStart(2, "0");

  if (includeSeconds) {
    const seconds = String(date.getSeconds()).padStart(2, "0");
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
  }

  return `${year}-${month}-${day}T${hours}:${minutes}`;
};

/**
 * Format a date string for display in a human-readable format
 *
 * @param dateString - ISO format date string (e.g., "2025-10-17T05:22:00")
 * @returns Formatted display string (e.g., "Oct 17, 2025 05:22 AM")
 */
export const formatDisplayDate = (dateString: string): string => {
  if (!dateString) return "-";

  const date = parseLocalDateTime(dateString);

  if (isNaN(date.getTime())) return "-";

  const months = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];

  const month = months[date.getMonth()];
  const day = date.getDate();
  const year = date.getFullYear();
  const hours = date.getHours();
  const minutes = String(date.getMinutes()).padStart(2, "0");
  const ampm = hours >= 12 ? "PM" : "AM";
  const displayHours = hours % 12 || 12;

  return `${month} ${day}, ${year} ${String(displayHours).padStart(
    2,
    "0"
  )}:${minutes} ${ampm}`;
};
