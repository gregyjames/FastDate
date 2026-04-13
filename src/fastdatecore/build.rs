fn main() {
    csbindgen::Builder::default()
        .input_extern_file("./src/lib.rs")
        .csharp_dll_name("fastdate")
        .csharp_namespace("FastDate")
        .generate_csharp_file("../FastDate/NativeMethods.g.cs")
        .unwrap();
}
