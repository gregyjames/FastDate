use crate::PackedDateTime;
use crate::constants::*;
use std::arch::x86_64::*;

/// Parse the iso datetime into a packed datetime object (SSE3).
/// # Safety
///
/// This method assumes input is in the correct ISO 8601 format.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_sse(input: *const u8) -> PackedDateTime {
    unsafe {
        let src = _mm_loadu_si128(input as *const __m128i);
        let ascii_zero = _mm_set1_epi8(ASCII_ZERO as i8);
        let digits = _mm_sub_epi8(src, ascii_zero);

        let mask = _mm_loadu_si128(SHUFFLE.as_ptr() as *const __m128i);
        let aligned = _mm_shuffle_epi8(digits, mask);

        // vertically mult + add adj pairs
        let mult = _mm_loadu_si128(MULTIPLIERS.as_ptr() as *const __m128i);
        let combined = _mm_maddubs_epi16(aligned, mult);

        let year_hi = _mm_extract_epi16(combined, 0) as u32; // 20
        let year_lo = _mm_extract_epi16(combined, 1) as u32; // 26
        let month = _mm_extract_epi16(combined, 2) as u32; // 04
        let day = _mm_extract_epi16(combined, 3) as u32; // 12
        let hour = _mm_extract_epi16(combined, 4) as u32; // 15
        let minute = _mm_extract_epi16(combined, 5) as u32; // 04

        let year = (year_hi * 100) + year_lo;

        let s1 = (*input.add(17) - b'0') as u32;
        let s2 = (*input.add(18) - b'0') as u32;
        let second = (s1 * 10) + s2;

        PackedDateTime {
            date: (year << 16) | (month << 8) | day,
            time: (hour << 24) | (minute << 16) | (second << 8),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_iso_date_sse() {
        let input = b"2026-04-15T02:27:31";

        unsafe {
            let result = parse_iso_date_sse(input.as_ptr());

            let expected_date = (2026 << 16) | (4 << 8) | 15;
            let expected_time = (2 << 24) | (27 << 16) | (31 << 8);

            assert_eq!(result.date, expected_date, "Date packing mismatch");
            assert_eq!(result.time, expected_time, "Time packing mismatch");
        }
    }

    #[test]
    fn test_parse_iso_date_sse_edge_case_sse() {
        let input = b"1999-12-31T23:59:59";

        unsafe {
            let result = parse_iso_date_sse(input.as_ptr());

            let year = (result.date >> 16) & 0xFFFF;
            let month = (result.date >> 8) & 0xFF;
            let day = result.date & 0xFF;

            assert_eq!(year, 1999);
            assert_eq!(month, 12);
            assert_eq!(day, 31);
        }
    }
}
