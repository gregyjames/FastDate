fn main(){
    csbindgen::Builder::default()
        .input_extern_file("./src/lib.rs")
        .csharp_dll_name("rust_lib")
        .csharp_namespace("FastDate")
        .generate_csharp_file("../FastDate/NativeMethods.g.cs")
        .unwrap();
}