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
        let s1 = (*input.add(17) - b'0') as u32;
        let s2 = (*input.add(18) - b'0') as u32;
        let second = (s1 * 10) + s2;
        
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

        PackedDateTime {
            date: (year << 16) | (month << 8) | day,
            time: (hour << 24) | (minute << 16) | (second << 8),
        }
    }
}

/// Parse an array of iso datetimes into packed datetime objects (SSE3).
/// # Safety
///
/// This method assumes all inputs are in the correct ISO 8601 format and `inputs` and `outputs` are valid for `count` elements.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_sse_bulk(
    inputs: *const *const u8,
    outputs: *mut PackedDateTime,
    count: usize,
) {
    unsafe {
        for i in 0..count {
            *outputs.add(i) = parse_iso_date_sse(*inputs.add(i));
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

    #[test]
    fn test_parse_iso_date_sse_bulk() {
        let input1 = b"2026-04-15T02:27:31";
        let input2 = b"1999-12-31T23:59:59";
        
        let inputs: [*const u8; 2] = [input1.as_ptr(), input2.as_ptr()];
        let mut outputs: [PackedDateTime; 2] = [PackedDateTime { date: 0, time: 0 }; 2];

        unsafe {
            parse_iso_date_sse_bulk(inputs.as_ptr(), outputs.as_mut_ptr(), 2);

            // Verify first
            let expected_date1 = (2026 << 16) | (4 << 8) | 15;
            let expected_time1 = (2 << 24) | (27 << 16) | (31 << 8);
            assert_eq!(outputs[0].date, expected_date1);
            assert_eq!(outputs[0].time, expected_time1);

            // Verify second
            let year2 = (outputs[1].date >> 16) & 0xFFFF;
            let month2 = (outputs[1].date >> 8) & 0xFF;
            let day2 = outputs[1].date & 0xFF;
            assert_eq!(year2, 1999);
            assert_eq!(month2, 12);
            assert_eq!(day2, 31);
        }
    }
}
