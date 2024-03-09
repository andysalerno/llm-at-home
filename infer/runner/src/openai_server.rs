use crate::{
    api::{ChatChoice, ChatStreamEvent, Message, Request},
    function_selection::{map_to_function, select_function},
};
use axum::{
    extract::State,
    response::{sse::Event, Sse},
    Json, Router,
};
use chat::{history::History, Role};
use chat_formats::detect_chat_template;
use futures_util::{Stream, StreamExt};
use libinfer::{
    chat_client::ChatClient,
    function::Function,
    llm_client::{LLMClient, StreamError, StreamEvent},
    read_prompt,
};
use log::{debug, info};
use std::{convert::Infallible, sync::Arc};

pub(crate) struct OpenAIServer {}

pub(crate) struct ServerState {
    chat_client: ChatClient,
    functions: Vec<Box<dyn Function + Send + Sync + 'static>>,
}

impl ServerState {
    pub(crate) fn new(
        chat_client: ChatClient,
        functions: Vec<Box<dyn Function + Send + Sync + 'static>>,
    ) -> Self {
        Self {
            chat_client,
            functions,
        }
    }
}

impl OpenAIServer {
    pub async fn serve(
        client: impl LLMClient + Send + Sync + 'static,
        functions: Vec<Box<dyn Function + Send + Sync + 'static>>,
    ) {
        let info = client.get_info().await;
        let chat_template = detect_chat_template(info.model_id());
        let chat_client = ChatClient::new(Box::new(client), chat_template);

        let state = Arc::new(ServerState::new(chat_client, functions));

        // build our application with a route
        let app = Router::new()
            .route("/chat/completions", axum::routing::post(Self::root))
            .with_state(state);

        let addr = "0.0.0.0:5555";
        let listener = tokio::net::TcpListener::bind(addr).await.unwrap();

        info!("Starting up server at {addr}...");
        axum::serve(listener, app).await.unwrap();
    }

    async fn root(
        State(state): State<Arc<ServerState>>,
        Json(payload): Json<Request>,
    ) -> Sse<impl Stream<Item = Result<Event, Infallible>>> {
        info!("Got a request: {payload:?}");

        let mut history = {
            let mut history = History::new();

            let prompt = read_prompt("system.txt");
            history.add(chat::Message::new(Role::System, prompt));

            history
        };

        for message in payload
            .messages()
            .iter()
            .filter(|m| m.role() != Role::System.as_str())
        {
            history.add(message.into());
        }

        let function_call =
            select_function(&state.chat_client, state.functions.as_slice(), &history).await;

        info!("Selected function: {function_call:?}");

        let function =
            map_to_function(&function_call, state.functions.as_slice()).unwrap_or_else(|| {
                panic!(
                    "Function invoked, but does not exist: {}",
                    function_call.name()
                )
            });

        let function_output = function
            .get_output(function_call.args(), &state.chat_client)
            .await;

        info!("got function output:\n{function_output}");

        if !function_output.is_empty() {
            history.add(chat::Message::new(Role::Function, function_output));
        }

        let stream = state.chat_client.get_assistant_response_stream(&history);
        let eos_token = state.chat_client.chat_template().eos_token().to_string();

        let model_name = payload.model().to_string();

        let stream = stream.map(move |event| match event {
            Ok(StreamEvent::Open) => {
                info!("openai response stream open");
                let chat_choice = ChatChoice::new(Message::new("assistant", ""), None);
                let stream_event = ChatStreamEvent::new(model_name.clone(), vec![chat_choice]);
                let event = Event::default().json_data(stream_event).unwrap();

                Ok(event)
            }
            Ok(StreamEvent::Message(data)) => {
                let text = data.token().text();

                // This is a leaky hack:
                // We don't want to send the EOS output text, which would otherwise appear at the end
                // of the final message. But we also don't know what text event is the final one.
                // So we remove the EOS text from the end of every streaming event, which is wrong, but works as a bandage.
                let text = text.trim_end_matches(&eos_token);

                let chat_choice = ChatChoice::new(Message::new("", text.to_string()), None);
                let stream_event = ChatStreamEvent::new(model_name.clone(), vec![chat_choice]);
                let event = Event::default().json_data(stream_event).unwrap();

                Ok(event)
            }
            Err(StreamError::StreamClose) => {
                info!("openai response stream closed");
                // Once streaming is done, this is the final event we will send, with finish_reason as 'stop':
                let chat_choice = ChatChoice::new(Message::new("", ""), Some("stop".into()));
                let stream_event = ChatStreamEvent::new(model_name.clone(), vec![chat_choice]);
                let final_event = Event::default().json_data(stream_event).unwrap();

                Ok(final_event)
            }
            _ => panic!("Unknown event: {event:?}"),
        });

        Sse::new(stream)
    }
}

impl From<&Message> for chat::Message {
    fn from(val: &Message) -> Self {
        chat::Message::new(Role::parse(val.role()), val.content())
    }
}
