use futures::future;
use log::{debug, info, trace};
use readable_readability::Readability;
use std::{error::Error, time::Duration};

use crate::Chunk;

const MAX_SECTION_LEN: usize = 1000;
const TOP_N_SECTIONS: usize = 3;
const MIN_SECTION_LEN: usize = 50;
const VISIT_LINKS_COUNT: usize = 6;

const USER_AGENT: &str = "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; Googlebot/2.1; +http://www.google.com/bot.html) Chrome/126.0.6437.4 Safari/537.36";

pub(crate) async fn scrape_readably<I, T>(uris: I) -> Vec<Chunk>
where
    I: IntoIterator<Item = T>,
    T: AsRef<str>,
{
    let uris = uris.into_iter().take(VISIT_LINKS_COUNT).collect::<Vec<_>>();

    let scrape_futures = uris.iter().map(scrape);

    future::join_all(scrape_futures)
        .await
        .into_iter()
        .filter_map(Result::ok)
        .zip(uris)
        .filter(|(text, _)| text.len() > MIN_SECTION_LEN)
        .map(|(text, uri)| (split_text_into_sections(text, MAX_SECTION_LEN), uri))
        .flat_map(|(texts, uri)| {
            let uri = uri.as_ref().to_owned();
            let t = texts.into_iter().map(move |text| (text, uri.clone()));
            t
        })
        .map(|(text, uri)| Chunk {
            content: text.to_owned(),
            uri,
        })
        .collect()
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

async fn scrape(url: impl AsRef<str>) -> Result<String, Box<dyn Error + Send + Sync>> {
    let url = url.as_ref();

    debug!("Scraping: {url}...");

    let client = reqwest::ClientBuilder::new()
        .timeout(Duration::from_millis(2000))
        .build()?;

    let response = client
        .get(url)
        .header("User-Agent", USER_AGENT)
        .send()
        .await?
        .error_for_status()?;

    let status = response.status();

    let s = response.text().await?;

    info!(
        "Response: {} Read text from {} length: {}",
        status,
        url,
        s.len()
    );

    let mut readability = Readability::new();
    let (node_ref, _metadata) = readability
        // .strip_unlikelys(true)
        // .clean_attributes(true)
        .parse(&s);

    debug!("Done.");

    let text_content = node_ref.text_contents();

    if !text_content.is_empty() {
        info!("Scraped down to len: {}", text_content.len());

        trace!("Scraped text:\n{text_content}");

        Ok(text_content.trim().into())
    } else {
        // let full_content = node_ref.to_string();
        let full_content = s.to_string();

        info!("Scraped down to len: {}", full_content.len());
        info!("Scraped text:\n{full_content}");

        Ok(full_content.clone())
    }
}
