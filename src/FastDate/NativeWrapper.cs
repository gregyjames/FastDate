using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FastDate;

internal static unsafe partial class NativeMethods
{
    [LibraryImport("fastdate", EntryPoint = "parse_iso_date_neon")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition), typeof(CallConvCdecl)])]
    public static partial PackedDateTime parse_iso_date_neon_fast(byte* input);
    
    [LibraryImport("fastdate", EntryPoint = "parse_iso_date_sse")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvSuppressGCTransition), typeof(CallConvCdecl)])]
    public static partial PackedDateTime parse_iso_date_sse_fast(byte* input);
}

/// <summary>
/// Contains the Packed date and times from the parser output.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public record struct PackedDateTime
{
    /// <summary>
    /// Packed Date YYYY-MM-DD
    /// </summary>
    public uint Date;
    
    /// <summary>
    /// Packed Time HH-MM-SS
    /// </summary>
    public uint Time;
}