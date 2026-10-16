use crate::PackedDateTime;
use crate::constants::*;
use std::arch::aarch64::*;

/// Parse the iso datetime into a packed datetime object (NEON).
/// # Safety
///
/// This method assumes `input` points to at least 19 UTF-8 bytes in ISO 8601 format (`YYYY-MM-DDTHH:MM:SS`).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_neon(input: *const u8) -> PackedDateTime {
    unsafe {
        let v0 = vld1q_u8(input);
        let v1 = vld1q_u8(input.add(3));
        let idx = vld1q_u8(SHUF_NEON_SINGLE.as_ptr());
        let shuf = vqtbl2q_u8(uint8x16x2_t(v0, v1), idx);
        let digits = vandq_u8(shuf, vdupq_n_u8(0x0F));
        let combined = vmla_u8(vget_low_u8(digits), vget_high_u8(digits), vdup_n_u8(10));
        let raw = vget_lane_u64(vreinterpret_u64_u8(combined), 0);
        let value = raw.wrapping_sub((raw >> 56) * (156u64 << 48));
        PackedDateTime { value }
    }
}

/// Parse a UTF-16 iso datetime directly into a packed datetime object (NEON).
/// # Safety
///
/// This method assumes `input` points to at least 19 UTF-16 code units (38 bytes) in ISO 8601 format (`YYYY-MM-DDTHH:MM:SS`).
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_utf16_neon(input: *const u16) -> PackedDateTime {
    unsafe {
        let p = input as *const u8;
        let v0 = vld1q_u8(p);
        let v1 = vld1q_u8(p.add(16));
        let v2 = vld1q_u8(p.add(22));
        let idx = vld1q_u8(SHUF_NEON_UTF16.as_ptr());
        let shuf = vqtbl3q_u8(uint8x16x3_t(v0, v1, v2), idx);
        let digits = vandq_u8(shuf, vdupq_n_u8(0x0F));
        let combined = vmla_u8(vget_low_u8(digits), vget_high_u8(digits), vdup_n_u8(10));
        let raw = vget_lane_u64(vreinterpret_u64_u8(combined), 0);
        let value = raw.wrapping_sub((raw >> 56) * (156u64 << 48));
        PackedDateTime { value }
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
        let idx_ones = vld1q_u8(SHUF_NEON_BULK_ONES.as_ptr());
        let idx_tens = vld1q_u8(SHUF_NEON_BULK_TENS.as_ptr());
        let ten = vdupq_n_u8(10);
        let year_fix = vld1q_u16(YEAR_FIX_NEON.as_ptr());
        let ascii_off = vdupq_n_u64(ASCII_OFFSET_NEON);

        let mut i = 0;
        while i + 4 <= count {
            let p0 = *inputs.add(i);
            let p1 = *inputs.add(i + 1);
            let p2 = *inputs.add(i + 2);
            let p3 = *inputs.add(i + 3);

            let tab01 = uint8x16x4_t(
                vld1q_u8(p0),
                vld1q_u8(p0.add(3)),
                vld1q_u8(p1),
                vld1q_u8(p1.add(3)),
            );
            let ones01 = vqtbl4q_u8(tab01, idx_ones);
            let tens01 = vqtbl4q_u8(tab01, idx_tens);
            let comb01 = vreinterpretq_u16_u8(vmlaq_u8(ones01, tens01, ten));
            let fix01 = vmlsq_u16(comb01, vshrq_n_u16(comb01, 8), year_fix);
            let res01 = vsubq_u64(vreinterpretq_u64_u16(fix01), ascii_off);

            let tab23 = uint8x16x4_t(
                vld1q_u8(p2),
                vld1q_u8(p2.add(3)),
                vld1q_u8(p3),
                vld1q_u8(p3.add(3)),
            );
            let ones23 = vqtbl4q_u8(tab23, idx_ones);
            let tens23 = vqtbl4q_u8(tab23, idx_tens);
            let comb23 = vreinterpretq_u16_u8(vmlaq_u8(ones23, tens23, ten));
            let fix23 = vmlsq_u16(comb23, vshrq_n_u16(comb23, 8), year_fix);
            let res23 = vsubq_u64(vreinterpretq_u64_u16(fix23), ascii_off);

            vst1q_u64(outputs.add(i) as *mut u64, res01);
            vst1q_u64(outputs.add(i + 2) as *mut u64, res23);

            i += 4;
        }

        while i < count {
            *outputs.add(i) = parse_iso_date_neon(*inputs.add(i));
            i += 1;
        }
    }
}

/// Parse a contiguous buffer of 20-byte-strided UTF-8 iso datetimes into packed datetime objects (NEON).
/// # Safety
///
/// This method assumes `inputs` points to at least `count * 20` valid bytes where each 20-byte slot starts with a 19-byte ISO 8601 string, and `outputs` is valid for `count` elements.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_neon_bulk_contiguous(
    inputs: *const u8,
    outputs: *mut PackedDateTime,
    count: usize,
) {
    unsafe {
        let idx_ones = vld1q_u8(SHUF_NEON_BULK_ONES.as_ptr());
        let idx_tens = vld1q_u8(SHUF_NEON_BULK_TENS.as_ptr());
        let ten = vdupq_n_u8(10);
        let year_fix = vld1q_u16(YEAR_FIX_NEON.as_ptr());
        let ascii_off = vdupq_n_u64(ASCII_OFFSET_NEON);

        let mut i = 0;
        let mut p = inputs;
        while i + 4 <= count {
            let tab01 = uint8x16x4_t(
                vld1q_u8(p),
                vld1q_u8(p.add(3)),
                vld1q_u8(p.add(20)),
                vld1q_u8(p.add(23)),
            );
            let ones01 = vqtbl4q_u8(tab01, idx_ones);
            let tens01 = vqtbl4q_u8(tab01, idx_tens);
            let comb01 = vreinterpretq_u16_u8(vmlaq_u8(ones01, tens01, ten));
            let fix01 = vmlsq_u16(comb01, vshrq_n_u16(comb01, 8), year_fix);
            let res01 = vsubq_u64(vreinterpretq_u64_u16(fix01), ascii_off);

            let tab23 = uint8x16x4_t(
                vld1q_u8(p.add(40)),
                vld1q_u8(p.add(43)),
                vld1q_u8(p.add(60)),
                vld1q_u8(p.add(63)),
            );
            let ones23 = vqtbl4q_u8(tab23, idx_ones);
            let tens23 = vqtbl4q_u8(tab23, idx_tens);
            let comb23 = vreinterpretq_u16_u8(vmlaq_u8(ones23, tens23, ten));
            let fix23 = vmlsq_u16(comb23, vshrq_n_u16(comb23, 8), year_fix);
            let res23 = vsubq_u64(vreinterpretq_u64_u16(fix23), ascii_off);

            vst1q_u64(outputs.add(i) as *mut u64, res01);
            vst1q_u64(outputs.add(i + 2) as *mut u64, res23);

            p = p.add(80);
            i += 4;
        }

        while i < count {
            *outputs.add(i) = parse_iso_date_neon(p);
            p = p.add(20);
            i += 1;
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
    fn test_parse_iso_date_utf16_neon() {
        let utf16: Vec<u16> = "2026-04-12T15:04:05".encode_utf16().collect();
        unsafe {
            let expected = parse_iso_date_neon(b"2026-04-12T15:04:05".as_ptr());
            let actual = parse_iso_date_utf16_neon(utf16.as_ptr());
            assert_eq!(actual.value, expected.value);
        }
    }

    #[test]
    fn test_parse_iso_date_boundaries_neon() {
        let inputs = [
            (b"0001-01-01T00:00:00", 1, 1, 1, 0, 0, 0),
            (b"9999-12-31T23:59:59", 9999, 12, 31, 23, 59, 59),
            (b"1999-12-31T23:59:59", 1999, 12, 31, 23, 59, 59),
            (b"2000-02-29T12:34:56", 2000, 2, 29, 12, 34, 56),
        ];

        for (str_input, y, m, d, hh, mm, ss) in inputs {
            unsafe {
                let result = parse_iso_date_neon(str_input.as_ptr());
                let date = (result.value >> 32) as u32;
                let time = result.value as u32;
                assert_eq!(date >> 16, y);
                assert_eq!((date >> 8) & 0xFF, m);
                assert_eq!(date & 0xFF, d);
                assert_eq!(time >> 24, hh);
                assert_eq!((time >> 16) & 0xFF, mm);
                assert_eq!((time >> 8) & 0xFF, ss);
                assert_eq!(time & 0xFF, 0);
            }
        }
    }

    #[test]
    fn test_parse_iso_date_neon_bulk_and_contiguous() {
        let raw_inputs: [&[u8; 19]; 6] = [
            b"2026-04-12T15:04:05",
            b"9999-12-31T23:59:59",
            b"0001-01-01T00:00:00",
            b"1999-12-31T23:59:59",
            b"2024-02-29T08:30:45",
            b"1970-01-01T00:00:01",
        ];

        let ptrs: [*const u8; 6] = [
            raw_inputs[0].as_ptr(),
            raw_inputs[1].as_ptr(),
            raw_inputs[2].as_ptr(),
            raw_inputs[3].as_ptr(),
            raw_inputs[4].as_ptr(),
            raw_inputs[5].as_ptr(),
        ];

        let mut outputs_bulk = [PackedDateTime { value: 0 }; 6];
        let mut outputs_contig = [PackedDateTime { value: 0 }; 6];
        let mut contig_buf = [0u8; 6 * 20];
        for (i, inp) in raw_inputs.iter().enumerate() {
            contig_buf[i * 20..i * 20 + 19].copy_from_slice(*inp);
        }

        unsafe {
            parse_iso_date_neon_bulk(ptrs.as_ptr(), outputs_bulk.as_mut_ptr(), 6);
            parse_iso_date_neon_bulk_contiguous(contig_buf.as_ptr(), outputs_contig.as_mut_ptr(), 6);

            for i in 0..6 {
                let single = parse_iso_date_neon(raw_inputs[i].as_ptr());
                assert_eq!(outputs_bulk[i].value, single.value, "bulk mismatch at {i}");
                assert_eq!(outputs_contig[i].value, single.value, "contig mismatch at {i}");
            }
        }
    }
}

