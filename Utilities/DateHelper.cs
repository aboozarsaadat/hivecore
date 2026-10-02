using System.Globalization;
using Humanizer;

namespace HiveCore.Utilities;

/// <summary>
/// Helpers for formatting and comparing dates/times consistently across the app.
/// All persistence is done in UTC.
/// </summary>
public static class DateHelper
{
    /// <summary>Returns current UTC time.</summary>
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Returns current time in the given timezone.</summary>
    public static DateTime NowIn(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return DateTime.UtcNow;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }

    /// <summary>Converts a UTC DateTime to a target timezone.</summary>
    public static DateTime ToTimeZone(DateTime utc, string? timeZoneId)
    {
        if (utc.Kind != DateTimeKind.Utc)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return utc;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }
        catch
        {
            return utc;
        }
    }

    /// <summary>Human readable relative time (e.g., "3 hours ago").</summary>
    public static string ToRelative(DateTime utc, DateTime? now = null)
    {
        var reference = now ?? DateTime.UtcNow;
        return utc.Humanize(false, reference);
    }

    /// <summary>Full ISO 8601 string (UTC).</summary>
    public static string ToIso(DateTime utc)
        => utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    /// <summary>Short date (e.g., "Sep 25, 2025").</summary>
    public static string ToShortDate(DateTime utc)
        => utc.ToUniversalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture);

    /// <summary>Join date string (e.g., "Joined September 2025").</summary>
    public static string ToJoinedLabel(DateTime utc)
        => $"Joined {utc.ToUniversalTime():MMMM yyyy}";

    // ============================================================
    // Boundaries
    // ============================================================
    public static DateTime StartOfDay(DateTime value)
        => value.Date;

    public static DateTime EndOfDay(DateTime value)
        => value.Date.AddDays(1).AddTicks(-1);

    public static DateTime StartOfWeek(DateTime value, DayOfWeek start = DayOfWeek.Monday)
    {
        int diff = (7 + (value.DayOfWeek - start)) % 7;
        return value.Date.AddDays(-diff);
    }

    public static DateTime StartOfMonth(DateTime value)
        => new(value.Year, value.Month, 1, 0, 0, 0, value.Kind);

    public static DateTime EndOfMonth(DateTime value)
        => StartOfMonth(value).AddMonths(1).AddTicks(-1);

    // ============================================================
    // Comparisons
    // ============================================================
    public static bool IsToday(DateTime utc)
        => utc.Date == DateTime.UtcNow.Date;

    public static bool IsYesterday(DateTime utc)
        => utc.Date == DateTime.UtcNow.Date.AddDays(-1);

    public static bool IsWithinLastHours(DateTime utc, int hours)
        => utc >= DateTime.UtcNow.AddHours(-Math.Abs(hours));

    public static bool IsWithinLastDays(DateTime utc, int days)
        => utc >= DateTime.UtcNow.AddDays(-Math.Abs(days));

    /// <summary>Calculates age in whole years from a birthdate.</summary>
    public static int? CalculateAge(DateTime? dateOfBirth)
    {
        if (dateOfBirth is null) return null;

        var today = DateTime.UtcNow.Date;
        var dob = dateOfBirth.Value.Date;

        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;

        return age < 0 ? null : age;
    }

    // ============================================================
    // UNIX timestamps
    // ============================================================
    public static long ToUnixSeconds(DateTime utc)
        => new DateTimeOffset(utc.ToUniversalTime()).ToUnixTimeSeconds();

    public static DateTime FromUnixSeconds(long seconds)
        => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
}