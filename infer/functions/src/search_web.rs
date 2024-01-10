use async_trait::async_trait;
use chat::{history::History, Message, Role};
use futures::future;
use libinfer::{
    chat_client::ChatClient,
    embeddings::{Embedding, EmbeddingsRequest},
    function::Function,
};
use log::{debug, info, trace};
use ordered_float::OrderedFloat;
use readable_readability::Readability;
use serde::Deserialize;
use std::{error::Error, time::Duration, vec};

const MAX_SECTION_LEN: usize = 1000;
const TOP_N_SECTIONS: usize = 3;
const MIN_SECTION_LEN: usize = 50;
const VISIT_LINKS_COUNT: usize = 6;

/// A function that performs a web search.
pub struct WebSearch;

#[async_trait]
impl Function for WebSearch {
    fn name(&self) -> &str {
        "search_web"
    }

    fn description_for_model(&self) -> &str {
        r#"def search_web(query: str) -> WebResults:
    """
    Searches online for a query.

    Examples:
        search_web('movies showing this weekend')
        search_web('best pizza in Seattle')
        search_web('news about large language models')
    """
    # implementation omitted
    pass"#
    }

    async fn get_output(&self, input: &str, model_client: &ChatClient) -> String {
        // Search the web and find relevant text, split into sections:
        let sections: Vec<String> = {
            let top_links = search(input).await;

            let scrape_futures = top_links.into_iter().take(VISIT_LINKS_COUNT).map(scrape);

            future::join_all(scrape_futures)
                .await
                .into_iter()
                .filter_map(Result::ok)
                .filter(|text| text.len() > MIN_SECTION_LEN)
                .flat_map(|text| split_text_into_sections(text, MAX_SECTION_LEN))
                .collect()
        };

        // Get embeddings for the sections:
        let corpus_embeddings = {
            debug!("Getting embeddings for {} text extracts...", sections.len());

            // The embedding model we use expects a certain formatting:
            let sections_as_query = sections
                .iter()
                .map(|text| format!("passage: {text}"))
                .collect();

            let embeddings_result = model_client
                .get_embeddings(&EmbeddingsRequest::new(sections_as_query))
                .await;

            let mut corpus_embeddings = embeddings_result.take_embeddings();
            corpus_embeddings.sort_unstable_by_key(Embedding::index);

            {
                let len = corpus_embeddings.len();
                debug!("Got {len} embeddings.");
            }

            corpus_embeddings
        };

        // Transform user input into a question:
        let user_embed_str: String = {
            debug!("Turning input '{input}' into a question");
            let history = build_question_generation_history(input);

            let response = model_client.get_assistant_response(&history).await;

            let response_str = response.content();

            info!("Converted input '{}' to question '{}'", input, response_str);

            // hack for now: remove the hardcoded 'question: '
            let response_str = response_str.trim_start_matches("question: ");

            // response_str.into()
            format!("query: {response_str}")
        };

        // Get embedding for user query:
        let user_input_embedding = {
            let response = model_client
                .get_embeddings(&EmbeddingsRequest::new(vec![user_embed_str.clone()]))
                .await;
            let embeddings = response.take_embeddings();
            embeddings.into_iter().next().expect("Expected embeddings")
        };

        // Get scores for each embedding, and sort from best to worst:
        let with_scores = {
            debug!("Finding closest matches for: {user_embed_str}");
            let mut with_scores: Vec<_> = corpus_embeddings
                .into_iter()
                .map(|e| {
                    let similarity =
                        cosine_similarity(user_input_embedding.embedding(), e.embedding());
                    (e, OrderedFloat(similarity))
                })
                .collect();

            with_scores.sort_unstable_by_key(|(_, score)| -*score);

            with_scores
        };

        // Build final result from top-scoring embeddings:
        {
            let mut result = String::new();
            for (n, (embedding, score)) in
                with_scores.into_iter().take(TOP_N_SECTIONS + 3).enumerate()
            {
                let index = embedding.index();
                let original_text = &sections[index];
                debug!("Score {score}: {original_text}");

                if n < TOP_N_SECTIONS {
                    result.push_str(&format!("    [WEB_RESULT {n}]: {original_text}\n"));
                }
            }

            // Trailing newline
            result.pop();

            result
        }
    }
}

fn split_text_into_sections(input: impl Into<String>, max_section_len: usize) -> Vec<String> {
    let mut result = Vec::<String>::new();

    let input: String = input.into();

    for sentence in input
        .split_terminator(&['.', '\n'])
        .filter(|s| !s.is_empty())
    {
        let sentence = sentence.trim().to_owned();
        if let Some(last) = result.last_mut() {
            if last.len() + sentence.len() > max_section_len {
                result.push(sentence);
            } else {
                last.push_str(". ");
                last.push_str(&sentence);
            }
        } else {
            result.push(sentence);
        }
    }

    result
}

fn cosine_similarity(vec1: &[f32], vec2: &[f32]) -> f32 {
    let dot_product: f32 = vec1.iter().zip(vec2.iter()).map(|(a, b)| a * b).sum();
    let magnitude_vec1: f32 = vec1.iter().map(|&n| n.powi(2)).sum::<f32>().sqrt();
    let magnitude_vec2: f32 = vec2.iter().map(|&n| n.powi(2)).sum::<f32>().sqrt();

    dot_product / (magnitude_vec1 * magnitude_vec2)
}

async fn search(query: &str) -> Vec<String> {
    let query = query.replace('"', "");

    debug!("Searching Google for '{query}'");

    let (api_key, cx) = get_api_key_cx();
    let client = reqwest::Client::new();

    let response = client
        .get("https://www.googleapis.com/customsearch/v1")
        .query(&[
            ("key", api_key.as_str()),
            ("cx", cx.as_str()),
            ("q", &query),
        ])
        .timeout(Duration::from_millis(5000))
        .send()
        .await
        .unwrap()
        .json::<Response>()
        .await
        .unwrap();

    let len = response.items.len();
    debug!("Got {len} results");

    response.items.into_iter().map(|i| i.link).collect()
}

async fn scrape(url: impl AsRef<str>) -> Result<String, Box<dyn Error + Send + Sync>> {
    let url = url.as_ref();

    debug!("Scraping: {url}...");

    let client = reqwest::ClientBuilder::new()
        .timeout(Duration::from_millis(2000))
        .build()?;

    let response = client.get(url)
        .header("User-Agent", "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.6226.2 Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)").send().await?;
    let s = response.text().await?;

    info!("Read text from {} length: {}", url, s.len());

    let mut readability = Readability::new();
    let (node_ref, _metadata) = readability
        .strip_unlikelys(true)
        .clean_attributes(true)
        .parse(&s);

    debug!("Done.");

    let text_content = node_ref.text_contents();

    info!("Scraped down to len: {}", text_content.len());

    trace!("Scraped text:\n{text_content}");

    Ok(text_content.trim().into())
}

fn get_api_key_cx() -> (String, String) {
    let api_key =
        std::fs::read_to_string(".googlekey.txt").expect("Expected to find google key file.");
    let cx =
        std::fs::read_to_string(".googlecx.txt").expect("Expected to find google context file.");

    (api_key, cx)
}

fn build_question_generation_history(text: &str) -> History {
    let mut history = History::new();

    history.add(Message::new(
        Role::User,
        "Hello! I want you to take some text, and transform it into a question.\nIf the text is already a question, just return it as-is.\nMake sense?",
    ));
    history.add(Message::new(Role::Assistant, "Makes sense! I'm ready."));
    history.add(Message::new(
        Role::User,
        "Transform this into a question: 'protests in france'",
    ));
    history.add(Message::new(
        Role::Assistant,
        "question: what are the protests in France?",
    ));
    history.add(Message::new(
        Role::User,
        "Transform this into a question: 'weather in Seattle this weekend'",
    ));
    history.add(Message::new(
        Role::Assistant,
        "question: what is the weather in Seattle this weekend?",
    ));
    history.add(Message::new(
        Role::User,
        "Transform this into a question: 'best anime 2023'",
    ));
    history.add(Message::new(
        Role::Assistant,
        "question: what's the best anime in 2023?",
    ));
    history.add(Message::new(
        Role::User,
        format!("Transform this into a question: '{text}'"),
    ));

    history
}

#[derive(Deserialize)]
struct Response {
    items: Vec<Item>,
}

#[allow(dead_code)]
#[derive(Deserialize)]
struct Item {
    title: String,
    link: String,
    snippet: String,
}
