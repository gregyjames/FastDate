using System.Runtime.CompilerServices;

namespace FastDate;

internal static class DateTimeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTime(this PackedDateTime packed)
    {
        uint d = packed.Date;
        uint t = packed.Time;

        return new DateTime(
            (int)(d >> 16),             // Year
            (int)((d >> 8) & 0xFF),    // Month
            (int)(d & 0xFF),             // Day
            (int)(t >> 24),             // Hour
            (int)((t >> 16) & 0xFF),   // Minute
            (int)((t >> 8) & 0xFF)    // Second
        );
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Year(this PackedDateTime packed) => (int)(packed.Date >> 16);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Month(this PackedDateTime packed) => (int)((packed.Date >> 8) & 0xFF);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Day(this PackedDateTime packed) => (int)(packed.Date & 0xFF);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Hour(this PackedDateTime packed) => (int)(packed.Time >> 24);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Minute(this PackedDateTime packed) => (int)((packed.Time >> 16) & 0xFF);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Second(this PackedDateTime packed) => (int)((packed.Time >> 8) & 0xFF);
}