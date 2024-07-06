use axum::{
    body::Body,
    http::{header, StatusCode, Uri},
    response::{IntoResponse, Response},
    routing::get,
    Router,
};
use env_logger::Env;
use log::info;
use std::path::{Path, PathBuf};
use tokio::fs;

#[tokio::main]
async fn main() {
    init_logging();

    let app = Router::new()
        .route("/", get(serve_file))
        .route("/*file", get(serve_file));

    let addr = "0.0.0.0:3000";
    let listener = tokio::net::TcpListener::bind(addr).await.unwrap();
    info!("Listening on {}", addr);
    axum::serve(listener, app.into_make_service())
        .await
        .unwrap();
}

fn init_logging() {
    let env = Env::default().filter_or("RUST_LOG", "info");
    env_logger::init_from_env(env);
    info!("Starting up.");
}

async fn serve_file(uri: Uri) -> impl IntoResponse {
    let mut path = PathBuf::from("public");

    if uri.path() == "/" {
        path.push("index.html");
    } else {
        path.push(uri.path().trim_start_matches('/'));
    }

    info!("Trying to serve file: {:?}", path);

    match fs::read(&path).await {
        Ok(contents) => {
            let mime_type = mime_type_for_path(&path);
            Response::builder()
                .status(StatusCode::OK)
                .header(header::CONTENT_TYPE, mime_type)
                .body(Body::from(contents))
                .unwrap()
        }
        Err(_) => Response::builder()
            .status(StatusCode::NOT_FOUND)
            .body(Body::from("file not found"))
            .unwrap(),
    }
}

fn mime_type_for_path(path: &Path) -> &'static str {
    let extension = path.extension().and_then(|e| e.to_str()).unwrap_or("");

    match extension {
        "html" => "text/html",
        "css" => "text/css",
        "js" => "application/javascript",
        "png" => "image/png",
        "jpg" | "jpeg" => "image/jpeg",
        "gif" => "image/gif",
        _ => "application/octet-stream",
    }
}
