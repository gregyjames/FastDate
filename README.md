# C# + Rust Library Template

A production-ready template for building cross-platform C# libraries with high-performance Rust components. This template demonstrates how to create NuGet packages that include native Rust libraries for Windows, Linux, and macOS.

## Features

- 🦀 **Rust FFI Integration** - Call high-performance Rust code from C#
- 🔨 **NUKE Build System** - Modern, strongly-typed build automation
- 🚀 **CI/CD Ready** - Automated builds with GitHub Actions
- 📦 **Multi-platform Support** - Windows, Linux, and macOS native libraries
- 🏷️ **Semantic Versioning** - Version from git tags (e.g., `v1.0.0`)
- 📚 **GitHub Packages** - Automatic publishing on release

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- [Rust](https://rustup.rs/) (stable toolchain)
- [NUKE Global Tool](https://nuke.build/) (optional, for local builds)

## Quick Start

### 1. Use This Template

Click "Use this template" on GitHub to create your own repository.

### 2. Customize Your Project

Update the following files with your project details:

**`rust-lib/Cargo.toml`:**
```toml
[package]
name = "your_lib_name"  # Change this
version = "0.1.0"
edition = "2024"

[lib]
crate-type = ["cdylib"]
```

**Your `.csproj` file:**
```xml
<PropertyGroup>
  <PackageId>YourPackageName</PackageId>
  <Authors>Your Name</Authors>
  <Description>Your package description</Description>
</PropertyGroup>
```

## Project Structure

```
.
├── .github/
│   └── workflows/
│       └── build.yml          # CI/CD pipeline
├── build/
│   └── Build.cs               # NUKE build script
├── rust-lib/
│   ├── Cargo.toml             # Rust project configuration
│   └── src/
│       └── lib.rs             # Rust FFI functions
├── src/
│   └── YourProject/
│       ├── YourProject.csproj # C# project with native lib config
│       └── RustInterop.cs     # P/Invoke wrapper
├── tests/
│   └── YourProject.Tests/     # Unit tests
├── .nuke                      # NUKE configuration
└── README.md
```

## Building Native Libraries

The Rust library is automatically built for all platforms in CI/CD. For local development:

```bash
cd rust-lib

# Build for your current platform
cargo build --release

# The output will be in:
# - Windows: target/release/your_lib.dll
# - Linux:   target/release/libyour_lib.so
# - macOS:   target/release/libyour_lib.dylib
```

## Calling Rust from C#

**Rust side (lib.rs):**
```rust
#[no_mangle]
pub extern "C" fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

**C# side:**
```csharp
[DllImport("your_lib", CallingConvention = CallingConvention.Cdecl)]
public static extern int add(int a, int b);

// Usage
int result = add(5, 3); // Returns 8
```

## CI/CD Pipeline

The GitHub Actions workflow automatically:

1. **Builds native libraries** on Windows, Linux, and macOS in parallel
2. **Runs tests** to ensure everything works
3. **Creates NuGet package** with all native libraries included
4. **Publishes to GitHub Packages** when you push a version tag

### Creating a Release

```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0

# The CI/CD pipeline will:
# - Build for all platforms
# - Create NuGet package version 1.0.0
# - Publish to GitHub Packages
# - Create a GitHub Release
```

### Pre-release Versions

```bash
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```

## Configuration

### Custom Build Steps

Edit `build/Build.cs` to add custom build logic. NUKE provides a fluent API for build automation.

## License

This template is available under the MIT License. See LICENSE file for details.

## Acknowledgments

- Built with [NUKE](https://nuke.build/)
- Powered by [Rust](https://www.rust-lang.org/)
- Automated with [GitHub Actions](https://github.com/features/actions)
