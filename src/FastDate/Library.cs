using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace FastDate;

internal static class DateTimeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ToDateTime(this PackedDateTime packed)
    {
        uint d = packed.date;
        uint t = packed.time;

        return new DateTime(
            (int)(d >> 16),          // Year
            (int)((d >> 8) & 0xFF),  // Month
            (int)(d & 0xFF),         // Day
            (int)(t >> 24),          // Hour
            (int)((t >> 16) & 0xFF), // Minute
            (int)((t >> 8) & 0xFF)   // Second
        );
    }
}

public static class FastDate
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe DateTime FromISO8601(string datetime)
    {
        if (datetime.Length != 19) ThrowFormat();
        
        byte* buffer = stackalloc byte[19];
        
        fixed (char* src = datetime)
        {
            for (var i = 0; i < 19; i++)
            {
                buffer[i] = (byte)src[i];
            }
        }

        DateTime date = DateTime.MinValue;
        
        if (AdvSimd.Arm64.IsSupported || AdvSimd.IsSupported)
        {
            var packed = NativeMethods.parse_iso_date_neon(buffer);
            if (packed.date == 0) ThrowFormat();
            date = packed.ToDateTime();
        }
        
        return date;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe DateTime FromISO8601(ReadOnlySpan<byte> data)
    {
        if (data.Length < 19) ThrowFormat();
        
        byte* pBuffer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        
        PackedDateTime packed = NativeMethods.parse_iso_date_neon(pBuffer);

        if (packed.date == 0) ThrowFormat();

        return packed.ToDateTime();
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowFormat() => throw new FormatException("Invalid ISO8601 format.");
}