use std::arch::aarch64::*;

#[repr(C)]
pub struct PackedDateTime {
    pub date: u32,
    pub time: u32,
}

const SHUFFLE: [u8; 16] = [
    0, 1, 2, 3, 5, 6, 8, 9,
    11, 12, 14, 15, 255, 255, 255, 255,
];

const MULTIPLIERS: [u8; 16] = [
    10, 1, 10, 1, 10, 1, 10, 1,
    10, 1, 10, 1, 0, 0, 0, 0,
];

const ASCII_ZERO: u8 = b'0';

#[unsafe(no_mangle)]
pub unsafe extern "C" fn parse_iso_date_neon(input: *const u8) -> PackedDateTime {
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
        ((*input.add(17) - ASCII_ZERO) as u32) * 10 +
        ((*input.add(18) - ASCII_ZERO) as u32);

    PackedDateTime {
        date: (year << 16) | (month << 8) | day,
        time: (hour << 24) | (minute << 16) | (second << 8),
    }
}