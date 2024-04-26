mod scrape;

use axum::{Json, Router};
use env_logger::Env;
use log::{debug, info};
use serde::{Deserialize, Serialize};

use crate::scrape::scrape_readably;

#[tokio::main]
async fn main() {
    {
        let log_level = "info";
        let env = Env::default().filter_or(
            "RUST_LOG",
            // format!("runner={log_level},model_client={log_level},chat={log_level},functions={log_level},libinfer={log_level},axum={log_level}"),
            log_level,
        );
        env_logger::init_from_env(env);
        debug!("Starting up.");
    }

    OpenAIServer::serve().await;
}

pub(crate) struct OpenAIServer {}

impl OpenAIServer {
    pub async fn serve() {
        let port = std::env::args()
            .filter(|a| a.starts_with("-p="))
            .map(|a| a.trim_start_matches("-p=").to_owned())
            .map(|a| a.parse::<usize>().unwrap())
            .next()
            .unwrap_or(8002);

        // build our application with a route
        let app = Router::new().route("/scrape", axum::routing::post(Self::root));

        let addr = format!("0.0.0.0:{}", port);
        info!("Starting up server at {addr}. Tip: use -p=<port> to specify a port");
        let listener = tokio::net::TcpListener::bind(addr).await.unwrap();

        axum::serve(listener, app).await.unwrap();
    }

    async fn root(Json(payload): Json<Request>) -> Json<Response> {
        info!("Got a request: {payload:?}");

        let chunks = scrape_readably(&payload.uris).await;

        let response = Response { chunks };

        info!("Done.");

        Json(response)
    }
}

#[allow(unused)]
#[derive(Deserialize, Debug)]
struct Request {
    uris: Vec<String>,
}

#[allow(unused)]
#[derive(Serialize, Deserialize, Debug)]
struct Response {
    chunks: Vec<Chunk>,
}

#[allow(unused)]
#[derive(Serialize, Deserialize, Debug)]
pub(crate) struct Chunk {
    content: String,
    uri: String,
}
