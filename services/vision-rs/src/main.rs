use std::path::Path;

use tokio::fs;
use tonic::{transport::Server, Request, Response, Status};
use tracing::{info, warn};

pub mod vision {
    tonic::include_proto!("vision");
}

use vision::vision_processor_server::{VisionProcessor, VisionProcessorServer};
use vision::{
    ExtractedBlock, HealthCheckRequest, HealthCheckResponse, ProcessDocumentRequest,
    ProcessDocumentResponse,
};

#[derive(Default)]
struct VisionProcessorService;

#[tonic::async_trait]
impl VisionProcessor for VisionProcessorService {
    async fn process_document(
        &self,
        request: Request<ProcessDocumentRequest>,
    ) -> Result<Response<ProcessDocumentResponse>, Status> {
        let payload = request.into_inner();
        let file_ref = payload.file_ref;
        let mime_type = payload.mime_type;

        let mut blocks = read_sidecar_blocks(&file_ref).await?;
        let used_fallback = blocks.is_empty();

        if used_fallback {
            warn!("No sidecar OCR encontrado para {}. Se enviara fallback.", file_ref);
            blocks = fallback_blocks(&mime_type);
        }

        Ok(Response::new(ProcessDocumentResponse {
            extracted_blocks: blocks,
            engine: "vision-rs-mock-ocr".to_string(),
            used_fallback,
        }))
    }

    async fn health_check(
        &self,
        _request: Request<HealthCheckRequest>,
    ) -> Result<Response<HealthCheckResponse>, Status> {
        Ok(Response::new(HealthCheckResponse {
            status: "ok".to_string(),
            service_name: "vision-rs".to_string(),
        }))
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    tracing_subscriber::fmt()
        .with_env_filter("info")
        .compact()
        .init();

    let address = "0.0.0.0:50051".parse()?;
    let service = VisionProcessorService;

    info!("vision-rs escuchando en {}", address);

    Server::builder()
        .add_service(VisionProcessorServer::new(service))
        .serve(address)
        .await?;

    Ok(())
}

async fn read_sidecar_blocks(file_ref: &str) -> Result<Vec<ExtractedBlock>, Status> {
    let sidecar_path = sidecar_path(file_ref);

    if !sidecar_path.exists() {
        return Ok(Vec::new());
    }

    let content = fs::read_to_string(&sidecar_path)
        .await
        .map_err(|error| Status::internal(format!("No se pudo leer el sidecar: {error}")))?;

    let blocks = content
        .lines()
        .filter(|line| !line.trim().is_empty())
        .enumerate()
        .map(|(index, line)| ExtractedBlock {
            line_number: (index + 1) as i32,
            text: line.trim().to_string(),
            confidence: 0.74,
        })
        .collect();

    Ok(blocks)
}

fn sidecar_path(file_ref: &str) -> std::path::PathBuf {
    let path = Path::new(file_ref);
    let mut result = path.to_path_buf();
    result.set_extension("txt");
    result
}

fn fallback_blocks(mime_type: &str) -> Vec<ExtractedBlock> {
    let sample_lines = if mime_type.eq_ignore_ascii_case("application/pdf") {
        vec![
            "ACEITE VEGETAL 1L S/ 8.90",
            "ARROZ EXTRA 5KG S/ 22.50",
            "AZUCAR RUBIA 1KG S/ 4.90",
        ]
    } else {
        vec![
            "LECHE EVAPORADA 410G S/ 3.40",
            "FIDEOS TALLARIN 500G S/ 2.80",
            "ACEITE VEGETAL 1L S/ 9.10",
        ]
    };

    sample_lines
        .into_iter()
        .enumerate()
        .map(|(index, text)| ExtractedBlock {
            line_number: (index + 1) as i32,
            text: text.to_string(),
            confidence: 0.78,
        })
        .collect()
}
