#[cfg(target_arch = "x86_64")]
pub const SHUFFLE: [u8; 16] = [0, 1, 2, 3, 5, 6, 8, 9, 11, 12, 14, 15, 255, 255, 255, 255];

#[cfg(target_arch = "x86_64")]
pub const MULTIPLIERS: [u8; 16] = [10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 0, 0, 0, 0];

#[cfg(target_arch = "x86_64")]
pub const ASCII_ZERO: u8 = b'0';

#[cfg(target_arch = "aarch64")]
pub const SHUF_NEON_SINGLE: [u8; 16] = [
    // Low 8 bytes: ones digits [0, s1, m1, h1, D1, M1, Y3, Y1]
    255, 31, 15, 12, 9, 6, 3, 1,
    // High 8 bytes: tens digits [0, s0, m0, h0, D0, M0, Y2, Y0]
    255, 30, 14, 11, 8, 5, 2, 0,
];

#[cfg(target_arch = "aarch64")]
pub const SHUF_NEON_UTF16: [u8; 16] = [
    // Low 8 bytes: ones digits from 3-register UTF-16 table (v0: 0..15, v1: 16..31, v2: 22..37)
    255, 46, 30, 24, 18, 12, 6, 2,
    // High 8 bytes: tens digits
    255, 44, 28, 22, 16, 10, 4, 0,
];

#[cfg(target_arch = "aarch64")]
pub const SHUF_NEON_BULK_ONES: [u8; 16] = [
    255, 31, 15, 12, 9, 6, 3, 1,
    255, 63, 47, 44, 41, 38, 35, 33,
];

#[cfg(target_arch = "aarch64")]
pub const SHUF_NEON_BULK_TENS: [u8; 16] = [
    255, 30, 14, 11, 8, 5, 2, 0,
    255, 62, 46, 43, 40, 37, 34, 32,
];

#[cfg(target_arch = "aarch64")]
pub const YEAR_FIX_NEON: [u16; 8] = [0, 0, 0, 156, 0, 0, 0, 156];

#[cfg(target_arch = "aarch64")]
pub const ASCII_OFFSET_NEON: u64 = 0x0650_1010_1010_1000;

