using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveInlining)]
    public static unsafe DateTime FromIso8601(string datetime)
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

        DateTime date;
        
        if ((AdvSimd.Arm64.IsSupported || AdvSimd.IsSupported) && OperatingSystem.IsMacOS())
        {
            var packed = NativeMethods.parse_iso_date_neon_fast(buffer);
            if (packed.date == 0) ThrowFormat();
            date = packed.ToDateTime();
        }
        else
        {
            switch (Sse.IsSupported)
            {
                case true when OperatingSystem.IsWindows():
                case true when OperatingSystem.IsLinux():
                    var packed = NativeMethods.parse_iso_date_sse_fast(buffer);
                    if (packed.date == 0) ThrowFormat();
                    date = packed.ToDateTime();
                    break;
                default:
                    Console.WriteLine("[WARNING] PLATFORM NOT SUPPORTED FALLING BACK ON DATETIME.PARSE");
                    date = DateTime.ParseExact(datetime, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                    break;
            }
        }
        
        return date;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static unsafe DateTime FromIso8601(ReadOnlySpan<byte> data)
    {
        if (data.Length < 19) ThrowFormat();
        
        DateTime date;
        
        byte* pBuffer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        
        if ((AdvSimd.Arm64.IsSupported || AdvSimd.IsSupported) && OperatingSystem.IsMacOS())
        {
            var packed = NativeMethods.parse_iso_date_neon_fast(pBuffer);
            if (packed.date == 0) ThrowFormat();
            date = packed.ToDateTime();
        }
        else
        {
            if ((Sse.IsSupported && OperatingSystem.IsWindows()) || (Sse.IsSupported && OperatingSystem.IsLinux()))
            {
                var packed = NativeMethods.parse_iso_date_sse_fast(pBuffer);
                if (packed.date == 0) ThrowFormat();
                date = packed.ToDateTime();
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "[ERROR] PLATFORM NOT SUPPORTED FALLING BACK ON DATETIME.PARSE");
            }
        }
        
        return date;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowFormat() => throw new FormatException("Invalid ISO8601 format.");
}