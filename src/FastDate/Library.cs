using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace FastDate;

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
        
    private static readonly unsafe delegate*<byte**, PackedDateTime*, nuint, void> ParseBulkFn = 
        (AdvSimd.Arm64.IsSupported && OperatingSystem.IsMacOS()) ? &NativeMethods.parse_iso_date_neon_bulk_fast :
        (Sse.IsSupported && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())) ? &NativeMethods.parse_iso_date_sse_bulk_fast :
        null;
    
    /// <summary>
    /// Parses an ISO 8601 string to a DateTime using SIMD-accelerated native parsing.
    /// Falls back to <see cref="DateTime.ParseExact(string, string, IFormatProvider)"/> on unsupported platforms.
    /// </summary>
    /// <param name="datetime">A 19-character ISO 8601 string in the format <c>YYYY-MM-DDTHH:MM:SS</c>.</param>
    /// <returns>The <see cref="DateTime"/> equivalent of the input string.</returns>
    /// <exception cref="FormatException">Thrown when the input is null, empty, or not exactly 19 characters.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static unsafe PackedDateTime FromIso8601(string datetime)
    {
        if (string.IsNullOrEmpty(datetime) || datetime.Length < 19) ThrowFormat();
        if (ParseFn == null)
        {
            throw new PlatformNotSupportedException();
        }
        
        byte* buffer = stackalloc byte[20];
        ref char c = ref MemoryMarshal.GetReference(datetime.AsSpan());

        buffer[0] = (byte)c;
        buffer[1] = (byte)Unsafe.Add(ref c, 1);
        buffer[2] = (byte)Unsafe.Add(ref c, 2);
        buffer[3] = (byte)Unsafe.Add(ref c, 3);
        buffer[4] = (byte)Unsafe.Add(ref c, 4);
        buffer[5] = (byte)Unsafe.Add(ref c, 5);
        buffer[6] = (byte)Unsafe.Add(ref c, 6);
        buffer[7] = (byte)Unsafe.Add(ref c, 7);
        buffer[8] = (byte)Unsafe.Add(ref c, 8);
        buffer[9] = (byte)Unsafe.Add(ref c, 9);
        buffer[10] = (byte)Unsafe.Add(ref c, 10);
        buffer[11] = (byte)Unsafe.Add(ref c, 11);
        buffer[12] = (byte)Unsafe.Add(ref c, 12);
        buffer[13] = (byte)Unsafe.Add(ref c, 13);
        buffer[14] = (byte)Unsafe.Add(ref c, 14);
        buffer[15] = (byte)Unsafe.Add(ref c, 15);
        buffer[16] = (byte)Unsafe.Add(ref c, 16);
        buffer[17] = (byte)Unsafe.Add(ref c, 17);
        buffer[18] = (byte)Unsafe.Add(ref c, 18);
        
        
        var packed = ParseFn(buffer);
        
        if (packed.Date == 0) ThrowFormat();
        return packed;
    }

    /// <summary>
    /// Parses an ISO 8601 byte sequence to a DateTime without allocating a string.
    /// </summary>
    /// <param name="data">A read-only span of at least 19 UTF-8 bytes in ISO 8601 format (<c>YYYY-MM-DDTHH:MM:SS</c>).</param>
    /// <returns>The <see cref="DateTime"/> equivalent of the input.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform does not support SSE or NEON SIMD intrinsics.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static unsafe PackedDateTime FromIso8601(ReadOnlySpan<byte> data)
    {
        if (data.Length < 19) ThrowFormat();
        if (ParseFn == null)
        {
            throw new PlatformNotSupportedException();
        }
        
        byte* pBuffer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        var packed = ParseFn(pBuffer);
        if (packed.Date == 0) ThrowFormat();
        return packed;
    }

    /// <summary>
    /// Parses a list of ISO 8601 strings in bulk into a pre-allocated output span,
    /// providing zero-allocation batch processing for string collections.
    /// </summary>
    /// <param name="datetimes">A list of 19-character ISO 8601 strings in the format <c>YYYY-MM-DDTHH:MM:SS</c>.</param>
    /// <param name="outputs">A span of PackedDateTime structs to receive the output. Must be at least as large as datetimes.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe void FromIso8601Bulk(List<string> datetimes, Span<PackedDateTime> outputs)
    {
        ArgumentNullException.ThrowIfNull(datetimes);
        int count = datetimes.Count;
        if (count == 0) return;
        if (outputs.Length < count) throw new ArgumentException("Output span is too small.");
        if (ParseBulkFn == null) throw new PlatformNotSupportedException();
        
        byte* pBuffer = (byte*)NativeMemory.Alloc((nuint)(count * 20));
        IntPtr* pPointers = (IntPtr*)NativeMemory.Alloc((nuint)(count * sizeof(IntPtr)));
        ReadOnlySpan<string> span = CollectionsMarshal.AsSpan(datetimes);
        
        try
        {
            fixed (PackedDateTime* pOutputs = outputs)
            {
                byte* pCurrent = pBuffer;
                IntPtr* pPtr = pPointers;
                ref string sRef = ref MemoryMarshal.GetReference(span);
                
                for (int i = 0; i < count; i++)
                {
                    if (sRef.Length < 19) ThrowFormat();
                    
                    *pPtr = (IntPtr)pCurrent;
                    
                    ref char c = ref MemoryMarshal.GetReference(sRef.AsSpan());
                    
                    pCurrent[0] = (byte)c;
                    pCurrent[1] = (byte)Unsafe.Add(ref c, 1);
                    pCurrent[2] = (byte)Unsafe.Add(ref c, 2);
                    pCurrent[3] = (byte)Unsafe.Add(ref c, 3);
                    pCurrent[4] = (byte)Unsafe.Add(ref c, 4);
                    pCurrent[5] = (byte)Unsafe.Add(ref c, 5);
                    pCurrent[6] = (byte)Unsafe.Add(ref c, 6);
                    pCurrent[7] = (byte)Unsafe.Add(ref c, 7);
                    pCurrent[8] = (byte)Unsafe.Add(ref c, 8);
                    pCurrent[9] = (byte)Unsafe.Add(ref c, 9);
                    pCurrent[10] = (byte)Unsafe.Add(ref c, 10);
                    pCurrent[11] = (byte)Unsafe.Add(ref c, 11);
                    pCurrent[12] = (byte)Unsafe.Add(ref c, 12);
                    pCurrent[13] = (byte)Unsafe.Add(ref c, 13);
                    pCurrent[14] = (byte)Unsafe.Add(ref c, 14);
                    pCurrent[15] = (byte)Unsafe.Add(ref c, 15);
                    pCurrent[16] = (byte)Unsafe.Add(ref c, 16);
                    pCurrent[17] = (byte)Unsafe.Add(ref c, 17);
                    pCurrent[18] = (byte)Unsafe.Add(ref c, 18);
                    
                    pCurrent += 20;
                    pPtr++;
                    sRef = ref Unsafe.Add(ref sRef, 1);
                }
                
                ParseBulkFn((byte**)pPointers, pOutputs, (nuint)count);
            }
        }
        finally
        {
            NativeMemory.Free(pBuffer);
            NativeMemory.Free(pPointers);
        }
    }

    /// <summary>
    /// Parses a list of ISO 8601 strings in bulk, minimizing FFI overhead.
    /// This method automatically converts the UTF-16 strings to UTF-8 without pinning strings in memory, but allocates a new return array.
    /// </summary>
    /// <param name="datetimes">A list of 19-character ISO 8601 strings in the format <c>YYYY-MM-DDTHH:MM:SS</c>.</param>
    /// <returns>An array of PackedDateTime representing the parsed dates.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static PackedDateTime[] FromIso8601Bulk(List<string> datetimes)
    {
        ArgumentNullException.ThrowIfNull(datetimes);
        int count = datetimes.Count;
        if (count == 0) return [];
        
        PackedDateTime[] outputs = new PackedDateTime[count];
        FromIso8601Bulk(datetimes, outputs);
        return outputs;
    }
    
    
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowFormat() => throw new FormatException("Invalid ISO8601 format.");
}