using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastDate;

/// <summary>
/// Source-generated native interop methods with <see cref="CallConvSuppressGCTransition"/>
/// for minimal P/Invoke overhead. These bypass the GC cooperative/preemptive transition
/// since the native functions are pure, sub-microsecond SIMD computations.
/// </summary>
internal static unsafe partial class NativeMethods
{
    /// <summary>
    /// Parses an ISO 8601 byte sequence using the NEON (AArch64) SIMD implementation.
    /// </summary>
    /// <param name="input">Pointer to at least 19 UTF-8 bytes in <c>YYYY-MM-DDTHH:MM:SS</c> format.</param>
    /// <returns>A <see cref="PackedDateTime"/> with bit-packed date and time components.</returns>
    [LibraryImport("fastdate", EntryPoint = "parse_iso_date_neon")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition), typeof(CallConvCdecl)])]
    internal static partial PackedDateTime parse_iso_date_neon_fast(byte* input);
    
    /// <summary>
    /// Parses an ISO 8601 byte sequence using the SSE3 (x86_64) SIMD implementation.
    /// </summary>
    /// <param name="input">Pointer to at least 19 UTF-8 bytes in <c>YYYY-MM-DDTHH:MM:SS</c> format.</param>
    /// <returns>A <see cref="PackedDateTime"/> with bit-packed date and time components.</returns>
    [LibraryImport("fastdate", EntryPoint = "parse_iso_date_sse")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition), typeof(CallConvCdecl)])]
    internal static partial PackedDateTime parse_iso_date_sse_fast(byte* input);
}

/// <summary>
/// A bit-packed representation of an ISO 8601 date and time, produced by the native SIMD parser.
/// </summary>
/// <remarks>
/// <para>The date and time components are stored in two 32-bit fields with the following layouts:</para>
/// <list type="bullet">
///   <item><description><see cref="Date"/>: <c>[YYYY (16 bits) | MM (8 bits) | DD (8 bits)]</c></description></item>
///   <item><description><see cref="Time"/>: <c>[HH (8 bits) | MM (8 bits) | SS (8 bits) | unused (8 bits)]</c></description></item>
/// </list>
/// <para>Use <see cref="DateTimeExtensions"/> to extract individual components or convert to <see cref="DateTime"/>.</para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public record struct PackedDateTime
{
    /// <summary>
    /// Bit-packed date: year in bits 16–31, month in bits 8–15, day in bits 0–7.
    /// </summary>
    public uint Date;
    
    /// <summary>
    /// Bit-packed time: hour in bits 24–31, minute in bits 16–23, second in bits 8–15.
    /// </summary>
    public uint Time;
}