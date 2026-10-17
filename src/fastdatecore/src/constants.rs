#[cfg(target_arch = "x86_64")]
pub const SHUF_SSE_V0: [u8; 16] = [
    0x80, 0x80, 0x80, 0x80, // pair 0: (0, 0), pair 1: (S0, S1 from v1)
    14, 15, // pair 2: Minute (M0, M1)
    11, 12, // pair 3: Hour (H0, H1)
    8, 9, // pair 4: Day (D0, D1)
    5, 6, // pair 5: Month (M0, M1)
    2, 3, // pair 6: Year low (Y2, Y3)
    0, 1, // pair 7: Year high (Y0, Y1)
];

#[cfg(target_arch = "x86_64")]
pub const SHUF_SSE_V1: [u8; 16] = [
    0x80, 0x80, // pair 0: (0, 0)
    14, 15, // pair 1: Second (S0, S1 from input[17], input[18])
    0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
];

#[cfg(target_arch = "x86_64")]
pub const SHUF_SSE_UTF16_V0: [u8; 16] = [
    0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 10, 12, // Month (M0, M1)
    4, 6, // Year low (Y2, Y3)
    0, 2, // Year high (Y0, Y1)
];

#[cfg(target_arch = "x86_64")]
pub const SHUF_SSE_UTF16_V1: [u8; 16] = [
    0x80, 0x80, 0x80, 0x80, 12, 14, // Minute (M0, M1)
    6, 8, // Hour (H0, H1)
    0, 2, // Day (D0, D1)
    0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
];

#[cfg(target_arch = "x86_64")]
pub const SHUF_SSE_UTF16_V2: [u8; 16] = [
    0x80, 0x80, 12, 14, // Second (S0, S1)
    0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
];

#[cfg(target_arch = "x86_64")]
pub const MULTIPLIERS_SSE: [u8; 16] = [10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1];

#[cfg(target_arch = "x86_64")]
pub const YEAR_FIX_SSE: [u16; 8] = [0, 0, 0, 156, 0, 0, 0, 156];

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
    255, 46, 30, 24, 18, 12, 6, 2, // High 8 bytes: tens digits
    255, 44, 28, 22, 16, 10, 4, 0,
];

#[cfg(target_arch = "aarch64")]
pub const SHUF_NEON_BULK_ONES: [u8; 16] =
    [255, 31, 15, 12, 9, 6, 3, 1, 255, 63, 47, 44, 41, 38, 35, 33];

#[cfg(target_arch = "aarch64")]
pub const SHUF_NEON_BULK_TENS: [u8; 16] =
    [255, 30, 14, 11, 8, 5, 2, 0, 255, 62, 46, 43, 40, 37, 34, 32];

#[cfg(target_arch = "aarch64")]
pub const YEAR_FIX_NEON: [u16; 8] = [0, 0, 0, 156, 0, 0, 0, 156];

#[cfg(target_arch = "aarch64")]
pub const ASCII_OFFSET_NEON: u64 = 0x0650_1010_1010_1000;
