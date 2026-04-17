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

/// <summary>
/// Provides SIMD-accelerated parsing of ISO 8601 datetime strings.
/// Dispatches to NEON (AArch64/macOS) or SSE (x86_64/Windows/Linux) native implementations at runtime.
/// </summary>
public static class Parser
{
    private static readonly unsafe delegate*<byte*, PackedDateTime> ParseFn = 
        (AdvSimd.Arm64.IsSupported && OperatingSystem.IsMacOS()) ? &NativeMethods.parse_iso_date_neon_fast :
        (Sse.IsSupported && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())) ? &NativeMethods.parse_iso_date_sse_fast :
        null;
    
    /// <summary>
    /// Parses an ISO 8601 string to a DateTime using SIMD-accelerated native parsing.
    /// Falls back to <see cref="DateTime.ParseExact(string, string, IFormatProvider)"/> on unsupported platforms.
    /// </summary>
    /// <param name="datetime">A 19-character ISO 8601 string in the format <c>YYYY-MM-DDTHH:MM:SS</c>.</param>
    /// <returns>The <see cref="DateTime"/> equivalent of the input string.</returns>
    /// <exception cref="FormatException">Thrown when the input is null, empty, or not exactly 19 characters.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveInlining)]
    public static unsafe DateTime FromIso8601(string datetime)
    {
        if (string.IsNullOrEmpty(datetime) || datetime.Length != 19) ThrowFormat();
        if (ParseFn == null)
        {
            throw new PlatformNotSupportedException();
        }
        
        byte* buffer = stackalloc byte[19];
        
        fixed (char* src = datetime)
        {
            for (var i = 0; i < 19; i++)
            {
                buffer[i] = (byte)src[i];
            }
        }
        
        var packed = ParseFn(buffer);
        
        if (packed.date == 0) ThrowFormat();
        return packed.ToDateTime();
    }

    /// <summary>
    /// Parses an ISO 8601 byte sequence to a DateTime without allocating a string.
    /// </summary>
    /// <param name="data">A read-only span of at least 19 UTF-8 bytes in ISO 8601 format (<c>YYYY-MM-DDTHH:MM:SS</c>).</param>
    /// <returns>The <see cref="DateTime"/> equivalent of the input.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform does not support SSE or NEON SIMD intrinsics.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static unsafe DateTime FromIso8601(ReadOnlySpan<byte> data)
    {
        if (data.Length < 19) ThrowFormat();

        if (ParseFn == null)
        {
            throw new PlatformNotSupportedException();
        }
        
        byte* pBuffer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        var packed = ParseFn(pBuffer);
        if (packed.date == 0) ThrowFormat();
        return packed.ToDateTime();
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowFormat() => throw new FormatException("Invalid ISO8601 format.");
}