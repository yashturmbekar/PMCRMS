import React, { useState, useEffect } from "react";
import { Calendar, Clock, ChevronLeft, ChevronRight } from "lucide-react";

interface DateTimePickerProps {
  value: string;
  onChange: (value: string) => void;
  minDate?: Date;
  label?: string;
  required?: boolean;
}

const DateTimePicker: React.FC<DateTimePickerProps> = ({
  value,
  onChange,
  minDate = new Date(),
  label = "Select Date & Time",
  required = false,
}) => {
  const [showPicker, setShowPicker] = useState(false);
  const [selectedDate, setSelectedDate] = useState<Date>(
    value ? new Date(value) : new Date()
  );
  const [currentMonth, setCurrentMonth] = useState(
    value ? new Date(value) : new Date()
  );
  // Convert 24-hour to 12-hour format for display
  const getDisplayHour = (hour24: number) => {
    if (hour24 === 0) return 12; // Midnight
    if (hour24 > 12) return hour24 - 12; // PM hours
    return hour24; // AM hours (1-12)
  };

  const [hours, setHours] = useState(
    value
      ? getDisplayHour(new Date(value).getHours())
      : getDisplayHour(new Date().getHours())
  );
  const [minutes, setMinutes] = useState(
    value ? new Date(value).getMinutes() : new Date().getMinutes()
  );
  const [period, setPeriod] = useState<"AM" | "PM">(
    value
      ? new Date(value).getHours() >= 12
        ? "PM"
        : "AM"
      : new Date().getHours() >= 12
      ? "PM"
      : "AM"
  );

  useEffect(() => {
    if (value) {
      const date = new Date(value);
      setSelectedDate(date);
      setCurrentMonth(date);
      const hour24 = date.getHours();
      setHours(getDisplayHour(hour24));
      setMinutes(date.getMinutes());
      setPeriod(hour24 >= 12 ? "PM" : "AM");
    }
  }, [value]);

  const months = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];

  const daysInMonth = (date: Date) => {
    return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
  };

  const firstDayOfMonth = (date: Date) => {
    return new Date(date.getFullYear(), date.getMonth(), 1).getDay();
  };

  const formatDisplayValue = () => {
    if (!value) return "";
    const date = new Date(value);
    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();

    // Convert to 12-hour format
    const hour24 = date.getHours();
    const displayHour = getDisplayHour(hour24);
    const displayPeriod = hour24 >= 12 ? "PM" : "AM";
    const mins = String(date.getMinutes()).padStart(2, "0");

    return `${day}/${month}/${year} ${String(displayHour).padStart(
      2,
      "0"
    )}:${mins} ${displayPeriod}`;
  };

  const handleDateSelect = (day: number) => {
    const newDate = new Date(
      currentMonth.getFullYear(),
      currentMonth.getMonth(),
      day,
      hours,
      minutes
    );
    setSelectedDate(newDate);
  };

  const handleApply = () => {
    // Convert 12-hour format to 24-hour format
    let hour24 = hours;
    if (period === "AM") {
      hour24 = hours === 12 ? 0 : hours; // 12 AM = 0, 1-11 AM = 1-11
    } else {
      hour24 = hours === 12 ? 12 : hours + 12; // 12 PM = 12, 1-11 PM = 13-23
    }

    const finalDate = new Date(
      selectedDate.getFullYear(),
      selectedDate.getMonth(),
      selectedDate.getDate(),
      hour24,
      minutes
    );

    // Format to datetime-local format (YYYY-MM-DDTHH:mm)
    const year = finalDate.getFullYear();
    const month = String(finalDate.getMonth() + 1).padStart(2, "0");
    const day = String(finalDate.getDate()).padStart(2, "0");
    const hrs = String(finalDate.getHours()).padStart(2, "0");
    const mins = String(finalDate.getMinutes()).padStart(2, "0");

    onChange(`${year}-${month}-${day}T${hrs}:${mins}`);
    setShowPicker(false);
  };

  const renderCalendar = () => {
    const days = [];
    const totalDays = daysInMonth(currentMonth);
    const firstDay = firstDayOfMonth(currentMonth);

    // Empty cells for days before the first day of month
    for (let i = 0; i < firstDay; i++) {
      days.push(
        <div
          key={`empty-${i}`}
          style={{ padding: "8px", textAlign: "center" }}
        ></div>
      );
    }

    // Days of the month
    for (let day = 1; day <= totalDays; day++) {
      const currentDate = new Date(
        currentMonth.getFullYear(),
        currentMonth.getMonth(),
        day
      );
      const isSelected =
        selectedDate.getDate() === day &&
        selectedDate.getMonth() === currentMonth.getMonth() &&
        selectedDate.getFullYear() === currentMonth.getFullYear();
      const isToday =
        new Date().getDate() === day &&
        new Date().getMonth() === currentMonth.getMonth() &&
        new Date().getFullYear() === currentMonth.getFullYear();

      // Use minDate prop for validation
      const minDateMidnight = new Date(minDate);
      minDateMidnight.setHours(0, 0, 0, 0);
      const isPast = currentDate < minDateMidnight;

      days.push(
        <div
          key={day}
          onClick={() => !isPast && handleDateSelect(day)}
          style={{
            padding: "6px",
            textAlign: "center",
            cursor: isPast ? "not-allowed" : "pointer",
            borderRadius: "6px",
            fontSize: "13px",
            fontWeight: isSelected ? "600" : "400",
            color: isPast
              ? "#d1d5db"
              : isSelected
              ? "white"
              : isToday
              ? "#10b981"
              : "#374151",
            backgroundColor: isSelected
              ? "#10b981"
              : isToday
              ? "#d1fae5"
              : "transparent",
            transition: "all 0.2s",
            opacity: isPast ? 0.4 : 1,
          }}
          onMouseEnter={(e) => {
            if (!isPast && !isSelected) {
              e.currentTarget.style.backgroundColor = "#f3f4f6";
            }
          }}
          onMouseLeave={(e) => {
            if (!isPast && !isSelected) {
              e.currentTarget.style.backgroundColor = isToday
                ? "#d1fae5"
                : "transparent";
            }
          }}
        >
          {day}
        </div>
      );
    }

    return days;
  };

  const goToPreviousMonth = () => {
    setCurrentMonth(
      new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1)
    );
  };

  const goToNextMonth = () => {
    setCurrentMonth(
      new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1)
    );
  };

  const handleHourChange = (newHours: number) => {
    setHours(newHours);
  };

  const handleMinuteChange = (newMinutes: number) => {
    setMinutes(newMinutes);
  };

  return (
    <div style={{ position: "relative", width: "100%" }}>
      {label && (
        <label
          style={{
            display: "block",
            marginBottom: "6px",
            fontWeight: 500,
            fontSize: "13px",
            color: "#374151",
          }}
        >
          {label} {required && <span style={{ color: "#dc2626" }}>*</span>}
        </label>
      )}

      <div
        onClick={() => setShowPicker(!showPicker)}
        style={{
          width: "100%",
          padding: "12px 16px",
          border: "1.5px solid #d1d5db",
          borderRadius: "8px",
          fontSize: "14px",
          cursor: "pointer",
          backgroundColor: "white",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          gap: "12px",
          transition: "all 0.2s",
          boxShadow: showPicker ? "0 0 0 3px rgba(16, 185, 129, 0.1)" : "none",
          borderColor: showPicker ? "#10b981" : "#d1d5db",
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "10px",
            flex: 1,
          }}
        >
          <Calendar size={18} style={{ color: "#10b981", flexShrink: 0 }} />
          <span style={{ color: value ? "#374151" : "#9ca3af" }}>
            {value ? formatDisplayValue() : "Select date and time"}
          </span>
        </div>
        <Clock size={16} style={{ color: "#9ca3af", flexShrink: 0 }} />
      </div>

      {showPicker && (
        <>
          {/* Backdrop */}
          <div
            onClick={() => setShowPicker(false)}
            style={{
              position: "fixed",
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              zIndex: 999,
            }}
          />

          {/* Picker Dropdown */}
          <div
            style={{
              position: "absolute",
              top: "calc(100% + 8px)",
              left: "50%",
              transform: "translateX(-50%)",
              zIndex: 1000,
              backgroundColor: "white",
              borderRadius: "10px",
              boxShadow:
                "0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)",
              border: "1px solid #e5e7eb",
              overflow: "visible",
              width: "320px",
              animation: "slideDown 0.2s ease-out",
            }}
          >
            {/* Calendar Header */}
            <div
              style={{
                background: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                padding: "12px 16px",
                color: "white",
              }}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  marginBottom: "12px",
                }}
              >
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    goToPreviousMonth();
                  }}
                  style={{
                    background: "rgba(255, 255, 255, 0.2)",
                    border: "none",
                    borderRadius: "6px",
                    padding: "6px",
                    cursor: "pointer",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    transition: "all 0.2s",
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.background =
                      "rgba(255, 255, 255, 0.3)";
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.background =
                      "rgba(255, 255, 255, 0.2)";
                  }}
                >
                  <ChevronLeft size={20} color="white" />
                </button>

                <div style={{ fontSize: "16px", fontWeight: "600" }}>
                  {months[currentMonth.getMonth()]} {currentMonth.getFullYear()}
                </div>

                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    goToNextMonth();
                  }}
                  style={{
                    background: "rgba(255, 255, 255, 0.2)",
                    border: "none",
                    borderRadius: "6px",
                    padding: "6px",
                    cursor: "pointer",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    transition: "all 0.2s",
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.background =
                      "rgba(255, 255, 255, 0.3)";
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.background =
                      "rgba(255, 255, 255, 0.2)";
                  }}
                >
                  <ChevronRight size={20} color="white" />
                </button>
              </div>
            </div>

            {/* Calendar Body */}
            <div style={{ padding: "12px 16px" }}>
              {/* Day Labels */}
              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "repeat(7, 1fr)",
                  marginBottom: "6px",
                }}
              >
                {["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"].map((day) => (
                  <div
                    key={day}
                    style={{
                      padding: "6px",
                      textAlign: "center",
                      fontSize: "11px",
                      fontWeight: "600",
                      color: "#6b7280",
                    }}
                  >
                    {day}
                  </div>
                ))}
              </div>

              {/* Calendar Grid */}
              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "repeat(7, 1fr)",
                  gap: "2px",
                }}
              >
                {renderCalendar()}
              </div>

              {/* Time Picker */}
              <div
                style={{
                  marginTop: "16px",
                  paddingTop: "16px",
                  borderTop: "1px solid #e5e7eb",
                }}
              >
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                    marginBottom: "10px",
                  }}
                >
                  <Clock size={14} style={{ color: "#10b981" }} />
                  <span
                    style={{
                      fontSize: "12px",
                      fontWeight: "600",
                      color: "#374151",
                    }}
                  >
                    Select Time
                  </span>
                </div>

                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "8px",
                  }}
                >
                  {/* Hours */}
                  <div style={{ flex: 1 }}>
                    <label
                      style={{
                        display: "block",
                        fontSize: "10px",
                        color: "#6b7280",
                        marginBottom: "4px",
                        fontWeight: "500",
                      }}
                    >
                      Hour
                    </label>
                    <select
                      value={hours}
                      onChange={(e) => handleHourChange(Number(e.target.value))}
                      onClick={(e) => e.stopPropagation()}
                      style={{
                        width: "100%",
                        padding: "8px 10px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "6px",
                        fontSize: "13px",
                        cursor: "pointer",
                        outline: "none",
                        backgroundColor: "white",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                      }}
                    >
                      {Array.from({ length: 12 }, (_, i) => {
                        const hour = i + 1; // 1 to 12
                        return (
                          <option key={hour} value={hour}>
                            {String(hour).padStart(2, "0")}
                          </option>
                        );
                      })}
                    </select>
                  </div>

                  <div
                    style={{
                      fontSize: "18px",
                      fontWeight: "600",
                      color: "#6b7280",
                      paddingTop: "14px",
                    }}
                  >
                    :
                  </div>

                  {/* Minutes */}
                  <div style={{ flex: 1 }}>
                    <label
                      style={{
                        display: "block",
                        fontSize: "10px",
                        color: "#6b7280",
                        marginBottom: "4px",
                        fontWeight: "500",
                      }}
                    >
                      Minute
                    </label>
                    <select
                      value={minutes}
                      onChange={(e) =>
                        handleMinuteChange(Number(e.target.value))
                      }
                      onClick={(e) => e.stopPropagation()}
                      style={{
                        width: "100%",
                        padding: "8px 10px",
                        border: "1.5px solid #d1d5db",
                        borderRadius: "6px",
                        fontSize: "13px",
                        cursor: "pointer",
                        outline: "none",
                        backgroundColor: "white",
                      }}
                      onFocus={(e) => {
                        e.target.style.borderColor = "#10b981";
                      }}
                      onBlur={(e) => {
                        e.target.style.borderColor = "#d1d5db";
                      }}
                    >
                      {Array.from({ length: 60 }, (_, i) => (
                        <option key={i} value={i}>
                          {String(i).padStart(2, "0")}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div style={{ flex: 1 }}>
                    <label
                      style={{
                        display: "block",
                        fontSize: "10px",
                        color: "#6b7280",
                        marginBottom: "4px",
                        fontWeight: "500",
                      }}
                    >
                      Period
                    </label>
                    <div
                      style={{
                        display: "flex",
                        gap: "4px",
                      }}
                    >
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setPeriod("AM");
                        }}
                        style={{
                          flex: 1,
                          padding: "8px",
                          border: "1.5px solid #d1d5db",
                          borderRadius: "6px",
                          fontSize: "12px",
                          fontWeight: "600",
                          cursor: "pointer",
                          backgroundColor:
                            period === "AM" ? "#10b981" : "white",
                          color: period === "AM" ? "white" : "#374151",
                          transition: "all 0.2s",
                        }}
                      >
                        AM
                      </button>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setPeriod("PM");
                        }}
                        style={{
                          flex: 1,
                          padding: "8px",
                          border: "1.5px solid #d1d5db",
                          borderRadius: "6px",
                          fontSize: "12px",
                          fontWeight: "600",
                          cursor: "pointer",
                          backgroundColor:
                            period === "PM" ? "#10b981" : "white",
                          color: period === "PM" ? "white" : "#374151",
                          transition: "all 0.2s",
                        }}
                      >
                        PM
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              {/* Action Buttons */}
              <div
                style={{
                  marginTop: "16px",
                  display: "flex",
                  gap: "8px",
                  justifyContent: "flex-end",
                }}
              >
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    setShowPicker(false);
                  }}
                  style={{
                    padding: "8px 16px",
                    border: "1.5px solid #d1d5db",
                    borderRadius: "6px",
                    fontSize: "13px",
                    fontWeight: "600",
                    cursor: "pointer",
                    backgroundColor: "white",
                    color: "#374151",
                    transition: "all 0.2s",
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.backgroundColor = "#f3f4f6";
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.backgroundColor = "white";
                  }}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleApply();
                  }}
                  style={{
                    padding: "8px 20px",
                    border: "none",
                    borderRadius: "6px",
                    fontSize: "13px",
                    fontWeight: "600",
                    cursor: "pointer",
                    background:
                      "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                    color: "white",
                    transition: "all 0.2s",
                    boxShadow: "0 2px 4px rgba(16, 185, 129, 0.2)",
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.transform = "translateY(-1px)";
                    e.currentTarget.style.boxShadow =
                      "0 4px 8px rgba(16, 185, 129, 0.3)";
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.transform = "translateY(0)";
                    e.currentTarget.style.boxShadow =
                      "0 2px 4px rgba(16, 185, 129, 0.2)";
                  }}
                >
                  Apply
                </button>
              </div>
            </div>
          </div>

          <style>
            {`
              @keyframes slideDown {
                from {
                  opacity: 0;
                  transform: translateY(-10px);
                }
                to {
                  opacity: 1;
                  transform: translateY(0);
                }
              }
            `}
          </style>
        </>
      )}
    </div>
  );
};

export default DateTimePicker;
