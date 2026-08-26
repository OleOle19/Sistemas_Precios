fn main() -> Result<(), Box<dyn std::error::Error>> {
    let protoc = protoc_bin_vendored::protoc_bin_path()?;
    unsafe {
        std::env::set_var("PROTOC", protoc);
    }

    tonic_build::configure()
        .build_server(true)
        .compile_protos(&["../../contracts/vision.proto"], &["../../contracts"])?;

    println!("cargo:rerun-if-changed=../../contracts/vision.proto");
    Ok(())
}
