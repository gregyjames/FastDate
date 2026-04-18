use crate::PackedDateTime;
use crate::constants::*;
use std::arch::aarch64::*;

#[unsafe(no_mangle)]
/// Parse the iso datetime into a packed datetime object (NEON).
/// # Safety
///
/// This method assumes input is in the correct ISO 8601 format.
pub unsafe extern "C" fn parse_iso_date_neon(input: *const u8) -> PackedDateTime {
    unsafe {
        let s1 = (*input.add(17) - ASCII_ZERO) as u32;
        let s2 = (*input.add(18) - ASCII_ZERO) as u32;
        let second = (s1 * 10) + s2;

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

        PackedDateTime {
            date: (year << 16) | (month << 8) | day,
            time: (hour << 24) | (minute << 16) | (second << 8),
        }
    }
}

/// Parse an array of iso datetimes into packed datetime objects (NEON).
/// # Safety
///
/// This method assumes all inputs are in the correct ISO 8601 format and `inputs` and `outputs` are valid for `count` elements.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_neon_bulk(
    inputs: *const *const u8,
    outputs: *mut PackedDateTime,
    count: usize,
) {
    unsafe {
        for i in 0..count {
            *outputs.add(i) = parse_iso_date_neon(*inputs.add(i));
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_iso_date_neon_standard() {
        let input = b"2026-04-12T15:04:05";
        unsafe {
            let result = parse_iso_date_neon(input.as_ptr());

            assert_eq!(result.date >> 16, 2026);
            assert_eq!((result.date >> 8) & 0xFF, 4);
            assert_eq!(result.date & 0xFF, 12);

            assert_eq!(result.time >> 24, 15);
            assert_eq!((result.time >> 16) & 0xFF, 4);
            assert_eq!((result.time >> 8) & 0xFF, 5);
        }
    }

    #[test]
    fn test_parse_iso_date_boundaries_neon() {
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

    #[test]
    fn test_parse_iso_date_neon_bulk() {
        let input1 = b"2026-04-12T15:04:05";
        let input2 = b"9999-12-31T23:59:59";

        let inputs: [*const u8; 2] = [input1.as_ptr(), input2.as_ptr()];
        let mut outputs: [PackedDateTime; 2] = [PackedDateTime { date: 0, time: 0 }; 2];

        unsafe {
            parse_iso_date_neon_bulk(inputs.as_ptr(), outputs.as_mut_ptr(), 2);

            // Verify first
            assert_eq!(outputs[0].date >> 16, 2026);
            assert_eq!((outputs[0].date >> 8) & 0xFF, 4);
            assert_eq!(outputs[0].date & 0xFF, 12);
            assert_eq!(outputs[0].time >> 24, 15);
            assert_eq!((outputs[0].time >> 16) & 0xFF, 4);
            assert_eq!((outputs[0].time >> 8) & 0xFF, 5);

            // Verify second
            assert_eq!(outputs[1].date >> 16, 9999);
            assert_eq!((outputs[1].date >> 8) & 0xFF, 12);
            assert_eq!(outputs[1].date & 0xFF, 31);
        }
    }
}
