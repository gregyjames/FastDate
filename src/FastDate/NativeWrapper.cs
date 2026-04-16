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