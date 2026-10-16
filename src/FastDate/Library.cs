using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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

    private static readonly unsafe delegate*<ushort*, PackedDateTime> ParseUtf16Fn =
        (AdvSimd.Arm64.IsSupported && OperatingSystem.IsMacOS()) ? &NativeMethods.parse_iso_date_utf16_neon_fast :
        null;
        
    private static readonly unsafe delegate*<byte**, PackedDateTime*, nuint, void> ParseBulkFn = 
        (AdvSimd.Arm64.IsSupported && OperatingSystem.IsMacOS()) ? &NativeMethods.parse_iso_date_neon_bulk_fast :
        (Sse.IsSupported && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())) ? &NativeMethods.parse_iso_date_sse_bulk_fast :
        null;

    private static readonly unsafe delegate*<byte*, PackedDateTime*, nuint, void> ParseBulkContiguousFn =
        (AdvSimd.Arm64.IsSupported && OperatingSystem.IsMacOS()) ? &NativeMethods.parse_iso_date_neon_bulk_contiguous_fast :
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

        if (ParseUtf16Fn != null)
        {
            ushort* pChars = (ushort*)Unsafe.AsPointer(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(datetime.AsSpan())));
            var packedUtf16 = ParseUtf16Fn(pChars);
            if (packedUtf16.Value == 0) ThrowFormat();
            return packedUtf16;
        }

        if (ParseFn == null)
        {
            throw new PlatformNotSupportedException();
        }
        
        byte* buffer = stackalloc byte[20];
        ref ushort c = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(datetime.AsSpan()));

        Vector128.Narrow(Vector128.LoadUnsafe(ref c), Vector128.LoadUnsafe(ref c, 8)).Store(buffer);
        Vector128.Narrow(Vector128.LoadUnsafe(ref c, 3), Vector128.LoadUnsafe(ref c, 11)).Store(buffer + 3);
        
        var packed = ParseFn(buffer);
        
        if (packed.Value == 0) ThrowFormat();
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
        if (packed.Value == 0) ThrowFormat();
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

        ReadOnlySpan<string> span = CollectionsMarshal.AsSpan(datetimes);

        if (ParseBulkContiguousFn != null)
        {
            const int CHUNK_SIZE = 256;
            byte* pChunkBuffer = stackalloc byte[CHUNK_SIZE * 20];

            fixed (PackedDateTime* pOutputs = outputs)
            {
                ref string sRef = ref MemoryMarshal.GetReference(span);
                int offset = 0;

                while (offset < count)
                {
                    int batch = Math.Min(CHUNK_SIZE, count - offset);
                    byte* pCurrent = pChunkBuffer;

                    for (int i = 0; i < batch; i++)
                    {
                        if (sRef.Length < 19) ThrowFormat();

                        ref ushort c = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(sRef.AsSpan()));
                        Vector128.Narrow(Vector128.LoadUnsafe(ref c), Vector128.LoadUnsafe(ref c, 8)).Store(pCurrent);
                        Vector128.Narrow(Vector128.LoadUnsafe(ref c, 3), Vector128.LoadUnsafe(ref c, 11)).Store(pCurrent + 3);

                        pCurrent += 20;
                        sRef = ref Unsafe.Add(ref sRef, 1);
                    }

                    ParseBulkContiguousFn(pChunkBuffer, pOutputs + offset, (nuint)batch);
                    offset += batch;
                }
            }
            return;
        }

        if (ParseBulkFn == null) throw new PlatformNotSupportedException();
        
        byte* pBuffer = (byte*)NativeMemory.Alloc((nuint)(count * 20));
        IntPtr* pPointers = (IntPtr*)NativeMemory.Alloc((nuint)(count * sizeof(IntPtr)));
        
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
                    
                    ref ushort c = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(sRef.AsSpan()));
                    Vector128.Narrow(Vector128.LoadUnsafe(ref c), Vector128.LoadUnsafe(ref c, 8)).Store(pCurrent);
                    Vector128.Narrow(Vector128.LoadUnsafe(ref c, 3), Vector128.LoadUnsafe(ref c, 11)).Store(pCurrent + 3);
                    
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