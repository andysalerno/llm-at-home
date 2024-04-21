mod scrape;

use axum::{extract::State, Json, Router};
use env_logger::Env;
use log::{debug, info};
use serde::Deserialize;
use std::sync::Arc;

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

pub(crate) struct ServerState {}

impl ServerState {
    pub(crate) fn new() -> Self {
        Self {}
    }
}

impl OpenAIServer {
    pub async fn serve() {
        let state = Arc::new(ServerState::new());

        // build our application with a route
        let app = Router::new()
            .route("/chat/completions", axum::routing::post(Self::root))
            .with_state(state);

        let addr = "0.0.0.0:5555";
        let listener = tokio::net::TcpListener::bind(addr).await.unwrap();

        info!("Starting up server at {addr}...");
        axum::serve(listener, app).await.unwrap();
    }

    async fn root(State(state): State<Arc<ServerState>>, Json(payload): Json<Request>) -> String {
        info!("Got a request: {payload:?}");

        scrape_readably(&payload.uri).await;

        "hello".into()
    }
}

#[allow(unused)]
#[derive(Deserialize, Debug)]
struct Request {
    uri: Vec<String>,
}
