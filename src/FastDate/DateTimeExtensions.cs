using System.Runtime.CompilerServices;

namespace FastDate;

/// <summary>
/// Provides extension methods for extracting date and time components
/// from a <see cref="PackedDateTime"/> and converting to <see cref="DateTime"/>.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts this <see cref="PackedDateTime"/> to a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="packed">The packed datetime to convert.</param>
    /// <returns>A <see cref="DateTime"/> with the unpacked year, month, day, hour, minute, and second.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static DateTime ToDateTime(this PackedDateTime packed)
    {
        uint d = packed.Date;
        uint t = packed.Time;

        return new DateTime(
            (int)(d >> 16),                 // Year
            (int)((d >> 8) & 0xFF),         // Month
            (int)(d & 0xFF),                // Day
            (int)(t >> 24),                 // Hour
            (int)((t >> 16) & 0xFF),        // Minute
            (int)((t >> 8) & 0xFF)          // Second
        );
    }

    /// <summary>
    /// Converts this <see cref="PackedDateTime"/> to a <see cref="DateOnly"/>, discarding the time component.
    /// </summary>
    /// <param name="packed">The packed datetime to convert.</param>
    /// <returns>A <see cref="DateOnly"/> with the unpacked year, month, and day.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static DateOnly ToDateOnly(this PackedDateTime packed)
    {
        uint d = packed.Date;
        
        return new DateOnly(
            (int)(d >> 16),                 // Year
            (int)((d >> 8) & 0xFF),         // Month
            (int)(d  & 0xFF));              // Day
    }
    
    /// <summary>
    /// Extracts the year component (1–9999) from the packed date.
    /// </summary>
    /// <param name="packed">The packed datetime to extract from.</param>
    /// <returns>The year as a 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Year(this PackedDateTime packed) => (int)(packed.Date >> 16);
    
    /// <summary>
    /// Extracts the month component (1–12) from the packed date.
    /// </summary>
    /// <param name="packed">The packed datetime to extract from.</param>
    /// <returns>The month as a 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Month(this PackedDateTime packed) => (int)((packed.Date >> 8) & 0xFF);
    
    /// <summary>
    /// Extracts the day component (1–31) from the packed date.
    /// </summary>
    /// <param name="packed">The packed datetime to extract from.</param>
    /// <returns>The day as a 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Day(this PackedDateTime packed) => (int)(packed.Date & 0xFF);
    
    /// <summary>
    /// Extracts the hour component (0–23) from the packed time.
    /// </summary>
    /// <param name="packed">The packed datetime to extract from.</param>
    /// <returns>The hour as a 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Hour(this PackedDateTime packed) => (int)(packed.Time >> 24);
    
    /// <summary>
    /// Extracts the minute component (0–59) from the packed time.
    /// </summary>
    /// <param name="packed">The packed datetime to extract from.</param>
    /// <returns>The minute as a 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Minute(this PackedDateTime packed) => (int)((packed.Time >> 16) & 0xFF);
    
    /// <summary>
    /// Extracts the second component (0–59) from the packed time.
    /// </summary>
    /// <param name="packed">The packed datetime to extract from.</param>
    /// <returns>The second as a 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Second(this PackedDateTime packed) => (int)((packed.Time >> 8) & 0xFF);
}