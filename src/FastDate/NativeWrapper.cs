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
    /// Parses an array of ISO 8601 byte sequences using the NEON (AArch64) SIMD implementation.
    /// </summary>
    [LibraryImport("fastdate", EntryPoint = "parse_iso_date_neon_bulk")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition), typeof(CallConvCdecl)])]
    internal static partial void parse_iso_date_neon_bulk_fast(byte** inputs, PackedDateTime* outputs, nuint count);
    
    /// <summary>
    /// Parses an ISO 8601 byte sequence using the SSE3 (x86_64) SIMD implementation.
    /// </summary>
    /// <param name="input">Pointer to at least 19 UTF-8 bytes in <c>YYYY-MM-DDTHH:MM:SS</c> format.</param>
    /// <returns>A <see cref="PackedDateTime"/> with bit-packed date and time components.</returns>
    [LibraryImport("fastdate", EntryPoint = "parse_iso_date_sse")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition), typeof(CallConvCdecl)])]
    internal static partial PackedDateTime parse_iso_date_sse_fast(byte* input);

    /// <summary>
    /// Parses an array of ISO 8601 byte sequences using the SSE3 (x86_64) SIMD implementation.
    /// </summary>
    [LibraryImport("fastdate", EntryPoint = "parse_iso_date_sse_bulk")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition), typeof(CallConvCdecl)])]
    internal static partial void parse_iso_date_sse_bulk_fast(byte** inputs, PackedDateTime* outputs, nuint count);
}

/// <summary>
/// A bit-packed representation of an ISO 8601 date and time, produced by the native SIMD parser.
/// </summary>
/// <remarks>
/// <para>The date and time components are stored in a single 64-bit field with the following layout:</para>
/// <list type="bullet">
///   <item><description>High 32 bits (Date): <c>[YYYY (16 bits) | MM (8 bits) | DD (8 bits)]</c></description></item>
///   <item><description>Low 32 bits (Time): <c>[HH (8 bits) | MM (8 bits) | SS (8 bits) | unused (8 bits)]</c></description></item>
/// </list>
/// <para>Use <see cref="DateTimeExtensions"/> to extract individual components or convert to <see cref="DateTime"/>.</para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public record struct PackedDateTime
{
    /// <summary>
    /// Bit-packed value containing both date and time.
    /// </summary>
    public ulong Value;
}