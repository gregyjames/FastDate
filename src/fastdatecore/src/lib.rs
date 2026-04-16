mod constants;

#[cfg(target_arch = "aarch64")]
mod neon;

#[cfg(target_arch = "x86_64")]
mod sse;

#[cfg(target_arch = "aarch64")]
pub use neon::*;

#[cfg(target_arch = "x86_64")]
pub use sse::*;

#[repr(C)]
pub struct PackedDateTime {
    pub date: u32,
    pub time: u32,
}
