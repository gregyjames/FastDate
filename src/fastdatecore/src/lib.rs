#[cfg(target_arch = "aarch64")]
use std::arch::aarch64::*;

#[cfg(target_arch = "x86_64")]
use std::arch::x86_64::*;

#[repr(C)]
pub struct PackedDateTime {
    pub date: u32,
    pub time: u32,
}

const SHUFFLE: [u8; 16] = [0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, 255, 255, 255, 255];

const MULTIPLIERS: [u8; 16] = [10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 0, 0, 0, 0];

const ASCII_ZERO: u8 = b'0';

#[cfg(target_arch = "aarch64")]
#[unsafe(no_mangle)]
/// Parse the iso datetime into a packed datetime object.
/// # Safety
///
/// This method assumes input is in the correct ISO 8601 format.
pub unsafe extern "C" fn parse_iso_date_neon(input: *const u8) -> PackedDateTime {
    unsafe {
        let src = vld1q_u8(input);
        let digits = vsubq_u8(src, vdupq_n_u8(ASCII_ZERO));

        let aligned = vqtbl1q_u8(digits, vld1q_u8(SHUFFLE.as_ptr()));
        let mult = vld1q_u8(MULTIPLIERS.as_ptr());

        let prod_lo = vmull_u8(vget_low_u8(aligned), vget_low_u8(mult));
        let prod_hi = vmull_u8(vget_high_u8(aligned), vget_high_u8(mult));

        let date_parts = vpaddlq_u16(prod_lo);
        let time_parts = vpaddlq_u16(prod_hi);

        let year = vgetq_lane_u32(date_parts, 0) * 100 + vgetq_lane_u32(date_parts, 1);
        let month = vgetq_lane_u32(date_parts, 2);
        let day = vgetq_lane_u32(date_parts, 3);

        let hour = vgetq_lane_u32(time_parts, 0);
        let minute = vgetq_lane_u32(time_parts, 1);

        let second =
            ((*input.add(17) - ASCII_ZERO) as u32) * 10 + ((*input.add(18) - ASCII_ZERO) as u32);

        PackedDateTime {
            date: (year << 16) | (month << 8) | day,
            time: (hour << 24) | (minute << 16) | (second << 8),
        }
    }
}

#[cfg(target_arch = "x86_64")]
pub unsafe extern "C" fn parse_iso_date_sse(input: *const u8) -> PackedDateTime {
    let src = _mm_loadu_si128(input as *const __m128i);
    let ascii_zero = _mm_set1_epi8(ASCII_ZERO as i8);
    let digits = _mm_sub_epi8(src, ascii_zero);

    let mask = _mm_loadu_si128(SHUFFLE.as_ptr() as *const __m128i);
    let aligned = _mm_shuffle_epi8(digits, mask);

    PackedDateTime{
        date: 0,
        time: 0,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    #[cfg(target_arch = "aarch64")]
    fn test_parse_iso_date_neon_standard() {
        let input = b"2026-04-12T15:04:05";
        unsafe {
            let result = parse_iso_date_neon(input.as_ptr());

            // Year: 2026, Month: 4, Day: 12
            // Expected Date: (2026 << 16) | (4 << 8) | 12 = 132776972
            assert_eq!(result.date >> 16, 2026);
            assert_eq!((result.date >> 8) & 0xFF, 4);
            assert_eq!(result.date & 0xFF, 12);

            // Hour: 15, Minute: 4, Second: 5
            // Expected Time: (15 << 24) | (4 << 16) | (5 << 8) = 251921664
            assert_eq!(result.time >> 24, 15);
            assert_eq!((result.time >> 16) & 0xFF, 4);
            assert_eq!((result.time >> 8) & 0xFF, 5);
        }
    }

    #[test]
    #[cfg(target_arch = "aarch64")]
    fn test_parse_iso_date_boundaries() {
        // Test Year 0001 and high values like 9999
        let inputs = [
            (b"0001-01-01T00:00:00", 1, 1, 1, 0, 0, 0),
            (b"9999-12-31T23:59:59", 9999, 12, 31, 23, 59, 59),
        ];

        for (str_input, y, m, d, hh, mm, ss) in inputs {
            unsafe {
                let result = parse_iso_date_neon(str_input.as_ptr());
                assert_eq!(result.date >> 16, y);
                assert_eq!((result.date >> 8) & 0xFF, m);
                assert_eq!(result.date & 0xFF, d);
                assert_eq!(result.time >> 24, hh);
                assert_eq!((result.time >> 16) & 0xFF, mm);
                assert_eq!((result.time >> 8) & 0xFF, ss);
            }
        }
    }
}
