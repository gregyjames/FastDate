use crate::PackedDateTime;
use crate::constants::*;
use std::arch::x86_64::*;

/// Parse the iso datetime into a packed datetime object (SSE3).
/// # Safety
///
/// This method assumes `input` points to at least 19 UTF-8 bytes in ISO 8601 format (`YYYY-MM-DDTHH:MM:SS`).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_sse(input: *const u8) -> PackedDateTime {
    unsafe {
        let v0 = _mm_loadu_si128(input as *const __m128i);
        let v1 = _mm_loadu_si128(input.add(3) as *const __m128i);
        let mask0 = _mm_loadu_si128(SHUF_SSE_V0.as_ptr() as *const __m128i);
        let mask1 = _mm_loadu_si128(SHUF_SSE_V1.as_ptr() as *const __m128i);
        let s0 = _mm_shuffle_epi8(v0, mask0);
        let s1 = _mm_shuffle_epi8(v1, mask1);
        let aligned = _mm_or_si128(s0, s1);
        let digits = _mm_and_si128(aligned, _mm_set1_epi8(0x0F));

        let mult = _mm_loadu_si128(MULTIPLIERS_SSE.as_ptr() as *const __m128i);
        let combined = _mm_maddubs_epi16(digits, mult);

        let packed = _mm_packus_epi16(combined, combined);
        let raw = _mm_cvtsi128_si64(packed) as u64;
        let value = raw.wrapping_sub((raw >> 56) * (156u64 << 48));

        PackedDateTime { value }
    }
}

/// Parse a UTF-16 iso datetime directly into a packed datetime object (SSE3).
/// # Safety
///
/// This method assumes `input` points to at least 19 UTF-16 code units (38 bytes) in ISO 8601 format (`YYYY-MM-DDTHH:MM:SS`).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_utf16_sse(input: *const u16) -> PackedDateTime {
    unsafe {
        let p = input as *const u8;
        let v0 = _mm_loadu_si128(p as *const __m128i);
        let v1 = _mm_loadu_si128(p.add(16) as *const __m128i);
        let v2 = _mm_loadu_si128(p.add(22) as *const __m128i);

        let mask0 = _mm_loadu_si128(SHUF_SSE_UTF16_V0.as_ptr() as *const __m128i);
        let mask1 = _mm_loadu_si128(SHUF_SSE_UTF16_V1.as_ptr() as *const __m128i);
        let mask2 = _mm_loadu_si128(SHUF_SSE_UTF16_V2.as_ptr() as *const __m128i);

        let s0 = _mm_shuffle_epi8(v0, mask0);
        let s1 = _mm_shuffle_epi8(v1, mask1);
        let s2 = _mm_shuffle_epi8(v2, mask2);
        let aligned = _mm_or_si128(_mm_or_si128(s0, s1), s2);
        let digits = _mm_and_si128(aligned, _mm_set1_epi8(0x0F));

        let mult = _mm_loadu_si128(MULTIPLIERS_SSE.as_ptr() as *const __m128i);
        let combined = _mm_maddubs_epi16(digits, mult);

        let packed = _mm_packus_epi16(combined, combined);
        let raw = _mm_cvtsi128_si64(packed) as u64;
        let value = raw.wrapping_sub((raw >> 56) * (156u64 << 48));

        PackedDateTime { value }
    }
}

/// Parse a contiguous buffer of 20-byte-strided UTF-8 iso datetimes into packed datetime objects (SSE3).
/// # Safety
///
/// This method assumes `inputs` points to at least `count * 20` valid bytes where each 20-byte slot starts with a 19-byte ISO 8601 string, and `outputs` is valid for `count` elements.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_sse_bulk(
    inputs: *const u8,
    outputs: *mut PackedDateTime,
    count: usize,
) {
    unsafe {
        let mask0 = _mm_loadu_si128(SHUF_SSE_V0.as_ptr() as *const __m128i);
        let mask1 = _mm_loadu_si128(SHUF_SSE_V1.as_ptr() as *const __m128i);
        let ascii_mask = _mm_set1_epi8(0x0F);
        let mult = _mm_loadu_si128(MULTIPLIERS_SSE.as_ptr() as *const __m128i);
        let year_mult = _mm_loadu_si128(YEAR_FIX_SSE.as_ptr() as *const __m128i);

        let mut i = 0;
        let mut p = inputs;

        // Process 4 dates per iteration using 128-bit SIMD registers
        while i + 4 <= count {
            // Date 0
            let v0_0 = _mm_loadu_si128(p as *const __m128i);
            let v0_1 = _mm_loadu_si128(p.add(3) as *const __m128i);
            let s0_0 = _mm_shuffle_epi8(v0_0, mask0);
            let s0_1 = _mm_shuffle_epi8(v0_1, mask1);
            let bytes0 = _mm_or_si128(s0_0, s0_1);
            let d0 = _mm_and_si128(bytes0, ascii_mask);
            let comb0 = _mm_maddubs_epi16(d0, mult);

            // Date 1
            let v1_0 = _mm_loadu_si128(p.add(20) as *const __m128i);
            let v1_1 = _mm_loadu_si128(p.add(23) as *const __m128i);
            let s1_0 = _mm_shuffle_epi8(v1_0, mask0);
            let s1_1 = _mm_shuffle_epi8(v1_1, mask1);
            let bytes1 = _mm_or_si128(s1_0, s1_1);
            let d1 = _mm_and_si128(bytes1, ascii_mask);
            let comb1 = _mm_maddubs_epi16(d1, mult);

            // Combine Date 0 (low 64 bits) and Date 1 (high 64 bits)
            let raw01 = _mm_packus_epi16(comb0, comb1);
            let hi01 = _mm_srli_epi16(raw01, 8);
            let fix01 = _mm_mullo_epi16(hi01, year_mult);
            let res01 = _mm_sub_epi16(raw01, fix01);

            // Date 2
            let v2_0 = _mm_loadu_si128(p.add(40) as *const __m128i);
            let v2_1 = _mm_loadu_si128(p.add(43) as *const __m128i);
            let s2_0 = _mm_shuffle_epi8(v2_0, mask0);
            let s2_1 = _mm_shuffle_epi8(v2_1, mask1);
            let bytes2 = _mm_or_si128(s2_0, s2_1);
            let d2 = _mm_and_si128(bytes2, ascii_mask);
            let comb2 = _mm_maddubs_epi16(d2, mult);

            // Date 3
            let v3_0 = _mm_loadu_si128(p.add(60) as *const __m128i);
            let v3_1 = _mm_loadu_si128(p.add(63) as *const __m128i);
            let s3_0 = _mm_shuffle_epi8(v3_0, mask0);
            let s3_1 = _mm_shuffle_epi8(v3_1, mask1);
            let bytes3 = _mm_or_si128(s3_0, s3_1);
            let d3 = _mm_and_si128(bytes3, ascii_mask);
            let comb3 = _mm_maddubs_epi16(d3, mult);

            // Combine Date 2 (low 64 bits) and Date 3 (high 64 bits)
            let raw23 = _mm_packus_epi16(comb2, comb3);
            let hi23 = _mm_srli_epi16(raw23, 8);
            let fix23 = _mm_mullo_epi16(hi23, year_mult);
            let res23 = _mm_sub_epi16(raw23, fix23);

            _mm_storeu_si128(outputs.add(i) as *mut __m128i, res01);
            _mm_storeu_si128(outputs.add(i + 2) as *mut __m128i, res23);

            p = p.add(80);
            i += 4;
        }

        // Handle remaining pair if available
        if i + 2 <= count {
            let v0_0 = _mm_loadu_si128(p as *const __m128i);
            let v0_1 = _mm_loadu_si128(p.add(3) as *const __m128i);
            let s0_0 = _mm_shuffle_epi8(v0_0, mask0);
            let s0_1 = _mm_shuffle_epi8(v0_1, mask1);
            let bytes0 = _mm_or_si128(s0_0, s0_1);
            let d0 = _mm_and_si128(bytes0, ascii_mask);
            let comb0 = _mm_maddubs_epi16(d0, mult);

            let v1_0 = _mm_loadu_si128(p.add(20) as *const __m128i);
            let v1_1 = _mm_loadu_si128(p.add(23) as *const __m128i);
            let s1_0 = _mm_shuffle_epi8(v1_0, mask0);
            let s1_1 = _mm_shuffle_epi8(v1_1, mask1);
            let bytes1 = _mm_or_si128(s1_0, s1_1);
            let d1 = _mm_and_si128(bytes1, ascii_mask);
            let comb1 = _mm_maddubs_epi16(d1, mult);

            let raw01 = _mm_packus_epi16(comb0, comb1);
            let hi01 = _mm_srli_epi16(raw01, 8);
            let fix01 = _mm_mullo_epi16(hi01, year_mult);
            let res01 = _mm_sub_epi16(raw01, fix01);

            _mm_storeu_si128(outputs.add(i) as *mut __m128i, res01);

            p = p.add(40);
            i += 2;
        }

        // Handle trailing single item
        while i < count {
            *outputs.add(i) = parse_iso_date_sse(p);
            p = p.add(20);
            i += 1;
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_iso_date_sse_standard() {
        let input = b"2026-04-12T15:04:05";
        unsafe {
            let result = parse_iso_date_sse(input.as_ptr());
            let date = (result.value >> 32) as u32;
            let time = result.value as u32;

            assert_eq!(date >> 16, 2026);
            assert_eq!((date >> 8) & 0xFF, 4);
            assert_eq!(date & 0xFF, 12);

            assert_eq!(time >> 24, 15);
            assert_eq!((time >> 16) & 0xFF, 4);
            assert_eq!((time >> 8) & 0xFF, 5);
            assert_eq!(time & 0xFF, 0);
        }
    }

    #[test]
    fn test_parse_iso_date_utf16_sse() {
        let utf16: Vec<u16> = "2026-04-12T15:04:05".encode_utf16().collect();
        unsafe {
            let expected = parse_iso_date_sse(b"2026-04-12T15:04:05".as_ptr());
            let actual = parse_iso_date_utf16_sse(utf16.as_ptr());
            assert_eq!(actual.value, expected.value);
        }
    }

    #[test]
    fn test_parse_iso_date_boundaries_sse() {
        let inputs: [(&[u8], u32, u32, u32, u32, u32, u32); 7] = [
            (b"0001-01-01T00:00:00", 1, 1, 1, 0, 0, 0),
            (b"9999-12-31T23:59:59", 9999, 12, 31, 23, 59, 59),
            (b"1999-12-31T23:59:59", 1999, 12, 31, 23, 59, 59),
            (b"2000-02-29T12:34:56", 2000, 2, 29, 12, 34, 56),
            (b"2024-02-29T08:30:45", 2024, 2, 29, 8, 30, 45),
            (b"1970-01-01T00:00:01", 1970, 1, 1, 0, 0, 1),
            (b"2026-10-15T23:10:44.1234567Z", 2026, 10, 15, 23, 10, 44),
        ];

        for (str_input, y, m, d, hh, mm, ss) in inputs {
            unsafe {
                let result = parse_iso_date_sse(str_input.as_ptr());
                let date = (result.value >> 32) as u32;
                let time = result.value as u32;
                assert_eq!(date >> 16, y, "year mismatch for {:?}", std::str::from_utf8(str_input));
                assert_eq!((date >> 8) & 0xFF, m, "month mismatch");
                assert_eq!(date & 0xFF, d, "day mismatch");
                assert_eq!(time >> 24, hh, "hour mismatch");
                assert_eq!((time >> 16) & 0xFF, mm, "minute mismatch");
                assert_eq!((time >> 8) & 0xFF, ss, "second mismatch");
                assert_eq!(time & 0xFF, 0, "unused byte mismatch");

                // Also verify UTF-16 on all boundary dates
                let utf16_input: Vec<u16> = std::str::from_utf8(str_input).unwrap().encode_utf16().collect();
                let utf16_res = parse_iso_date_utf16_sse(utf16_input.as_ptr());
                assert_eq!(utf16_res.value, result.value, "UTF-16 mismatch for {:?}", std::str::from_utf8(str_input));
            }
        }
    }

    #[test]
    fn test_parse_iso_date_sse_bulk_batch_sizes() {
        let sample_dates = [
            b"2026-04-12T15:04:05",
            b"9999-12-31T23:59:59",
            b"0001-01-01T00:00:00",
            b"1999-12-31T23:59:59",
            b"2024-02-29T08:30:45",
            b"1970-01-01T00:00:01",
            b"2000-02-29T12:34:56",
        ];

        let counts_to_test = [0, 1, 2, 3, 4, 5, 6, 7, 8, 15, 16, 255, 256, 257, 512, 1000];

        for &count in &counts_to_test {
            let mut contig_buf = vec![0u8; count * 20];
            let mut expected = Vec::with_capacity(count);

            for i in 0..count {
                let sample = sample_dates[i % sample_dates.len()];
                contig_buf[i * 20..i * 20 + 19].copy_from_slice(sample);
                unsafe {
                    expected.push(parse_iso_date_sse(sample.as_ptr()).value);
                }
            }

            let mut outputs = vec![PackedDateTime { value: 0 }; count];
            unsafe {
                parse_iso_date_sse_bulk(contig_buf.as_ptr(), outputs.as_mut_ptr(), count);
            }

            for i in 0..count {
                assert_eq!(
                    outputs[i].value, expected[i],
                    "Bulk mismatch at index {i} for total count {count}"
                );
            }
        }
    }

    #[test]
    fn test_parse_iso_date_sse_all_years() {
        for y in 1..=9999 {
            let s = format!("{:04}-07-14T11:22:33", y);
            let bytes = s.as_bytes();
            let utf16: Vec<u16> = s.encode_utf16().collect();
            unsafe {
                let res_utf8 = parse_iso_date_sse(bytes.as_ptr());
                let res_utf16 = parse_iso_date_utf16_sse(utf16.as_ptr());
                assert_eq!(res_utf8.value, res_utf16.value, "UTF-8 vs UTF-16 mismatch for year {y}");

                let date = (res_utf8.value >> 32) as u32;
                let time = res_utf8.value as u32;
                assert_eq!(date >> 16, y, "year mismatch for {y}");
                assert_eq!((date >> 8) & 0xFF, 7, "month mismatch");
                assert_eq!(date & 0xFF, 14, "day mismatch");
                assert_eq!(time >> 24, 11, "hour mismatch");
                assert_eq!((time >> 16) & 0xFF, 22, "minute mismatch");
                assert_eq!((time >> 8) & 0xFF, 33, "second mismatch");
                assert_eq!(time & 0xFF, 0, "unused byte mismatch");
            }
        }
    }

    #[test]
    fn test_parse_iso_date_sse_bulk() {
        let input1 = b"2026-04-15T02:27:31";
        let input2 = b"1999-12-31T23:59:59";

        let mut contig_buf = [0u8; 2 * 20];
        contig_buf[0..19].copy_from_slice(input1);
        contig_buf[20..39].copy_from_slice(input2);
        let mut outputs: [PackedDateTime; 2] = [PackedDateTime { value: 0 }; 2];

        unsafe {
            parse_iso_date_sse_bulk(contig_buf.as_ptr(), outputs.as_mut_ptr(), 2);

            // Verify first
            let expected_date1 = (2026 << 16) | (4 << 8) | 15;
            let expected_time1 = (2 << 24) | (27 << 16) | (31 << 8);
            let expected_value1 = ((expected_date1 as u64) << 32) | (expected_time1 as u64);
            assert_eq!(outputs[0].value, expected_value1);

            // Verify second
            let date2 = (outputs[1].value >> 32) as u32;
            let year2 = (date2 >> 16) & 0xFFFF;
            let month2 = (date2 >> 8) & 0xFF;
            let day2 = date2 & 0xFF;
            assert_eq!(year2, 1999);
            assert_eq!(month2, 12);
            assert_eq!(day2, 31);
        }
    }

    #[test]
    fn test_parse_iso_date_sse_components_range() {
        for m in 1..=12 {
            for d in 1..=28 {
                let s = format!("2025-{:02}-{:02}T12:34:56", m, d);
                let bytes = s.as_bytes();
                let utf16: Vec<u16> = s.encode_utf16().collect();
                unsafe {
                    let res = parse_iso_date_sse(bytes.as_ptr());
                    let res_u16 = parse_iso_date_utf16_sse(utf16.as_ptr());
                    assert_eq!(res.value, res_u16.value, "UTF-16 mismatch for m={m}, d={d}");
                    let date = (res.value >> 32) as u32;
                    assert_eq!((date >> 8) & 0xFF, m);
                    assert_eq!(date & 0xFF, d);
                }
            }
        }

        // Specifically test days 29..=31 for 31-day months
        for m in [1, 3, 5, 7, 8, 10, 12] {
            for d in 29..=31 {
                let s = format!("2025-{:02}-{:02}T12:34:56", m, d);
                let bytes = s.as_bytes();
                let utf16: Vec<u16> = s.encode_utf16().collect();
                unsafe {
                    let res = parse_iso_date_sse(bytes.as_ptr());
                    let res_u16 = parse_iso_date_utf16_sse(utf16.as_ptr());
                    assert_eq!(res.value, res_u16.value, "UTF-16 mismatch for m={m}, d={d}");
                    let date = (res.value >> 32) as u32;
                    assert_eq!((date >> 8) & 0xFF, m);
                    assert_eq!(date & 0xFF, d);
                }
            }
        }

        for h in 0..=23 {
            for min in 0..=59 {
                let s = format!("2025-06-15T{:02}:{:02}:42", h, min);
                let bytes = s.as_bytes();
                let utf16: Vec<u16> = s.encode_utf16().collect();
                unsafe {
                    let res = parse_iso_date_sse(bytes.as_ptr());
                    let res_u16 = parse_iso_date_utf16_sse(utf16.as_ptr());
                    assert_eq!(res.value, res_u16.value, "UTF-16 mismatch for h={h}, min={min}");
                    let time = res.value as u32;
                    assert_eq!(time >> 24, h);
                    assert_eq!((time >> 16) & 0xFF, min);
                    assert_eq!((time >> 8) & 0xFF, 42);
                }
            }
        }

        for sec in 0..=59 {
            let s = format!("2025-06-15T08:09:{:02}", sec);
            let bytes = s.as_bytes();
            let utf16: Vec<u16> = s.encode_utf16().collect();
            unsafe {
                let res = parse_iso_date_sse(bytes.as_ptr());
                let res_u16 = parse_iso_date_utf16_sse(utf16.as_ptr());
                assert_eq!(res.value, res_u16.value, "UTF-16 mismatch for sec={sec}");
                let time = res.value as u32;
                assert_eq!((time >> 8) & 0xFF, sec);
            }
        }
    }

    #[test]
    fn test_parse_iso_date_sse_bulk_dense_batch_sizes() {
        let sample_dates = [
            b"2026-04-12T15:04:05",
            b"9999-12-31T23:59:59",
            b"0001-01-01T00:00:00",
            b"1999-12-31T23:59:59",
            b"2024-02-29T08:30:45",
            b"1970-01-01T00:00:01",
            b"2000-02-29T12:34:56",
        ];

        for count in 0..=40 {
            let mut contig_buf = vec![0u8; count * 20];
            let mut expected = Vec::with_capacity(count);

            for i in 0..count {
                let sample = sample_dates[i % sample_dates.len()];
                contig_buf[i * 20..i * 20 + 19].copy_from_slice(sample);
                unsafe {
                    expected.push(parse_iso_date_sse(sample.as_ptr()).value);
                }
            }

            let mut outputs = vec![PackedDateTime { value: 0 }; count];
            unsafe {
                parse_iso_date_sse_bulk(contig_buf.as_ptr(), outputs.as_mut_ptr(), count);
            }

            for i in 0..count {
                assert_eq!(
                    outputs[i].value, expected[i],
                    "Dense bulk mismatch at index {i} for total count {count}"
                );
            }
        }
    }

    #[test]
    fn test_parse_iso_date_sse_subsecond_suffixes() {
        let suffixes = [
            "2026-04-12T15:04:05Z",
            "2026-04-12T15:04:05.0Z",
            "2026-04-12T15:04:05.1234567Z",
            "2026-04-12T15:04:05.9999999+00:00",
            "2026-04-12T15:04:05-07:00",
        ];

        for s in suffixes {
            let bytes = s.as_bytes();
            let utf16: Vec<u16> = s.encode_utf16().collect();
            unsafe {
                let res_utf8 = parse_iso_date_sse(bytes.as_ptr());
                let res_utf16 = parse_iso_date_utf16_sse(utf16.as_ptr());
                assert_eq!(res_utf8.value, res_utf16.value, "Mismatch on suffix for {s}");

                let date = (res_utf8.value >> 32) as u32;
                let time = res_utf8.value as u32;
                assert_eq!(date >> 16, 2026);
                assert_eq!((date >> 8) & 0xFF, 4);
                assert_eq!(date & 0xFF, 12);
                assert_eq!(time >> 24, 15);
                assert_eq!((time >> 16) & 0xFF, 4);
                assert_eq!((time >> 8) & 0xFF, 5);
                assert_eq!(time & 0xFF, 0);
            }
        }
    }
}
