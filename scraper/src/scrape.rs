use futures::future;
use log::{debug, info, trace};
use readable_readability::Readability;
use serde::Deserialize;
use std::{error::Error, time::Duration};

const MAX_SECTION_LEN: usize = 1000;
const TOP_N_SECTIONS: usize = 3;
const MIN_SECTION_LEN: usize = 50;
const VISIT_LINKS_COUNT: usize = 6;

pub(crate) async fn scrape_readably<I, T>(uris: I) -> Vec<String>
where
    I: IntoIterator<Item = T>,
    T: AsRef<str>,
{
    // Search the web and find relevant text, split into sections:
    let sections: Vec<String> = {
        let scrape_futures = uris.into_iter().take(VISIT_LINKS_COUNT).map(scrape);

        future::join_all(scrape_futures)
            .await
            .into_iter()
            .filter_map(Result::ok)
            .filter(|text| text.len() > MIN_SECTION_LEN)
            .flat_map(|text| split_text_into_sections(text, MAX_SECTION_LEN))
            .collect()
    };

    sections
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

    let response = client.get(url)
        .header("User-Agent", "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.6286.0 Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")
        .send()
        .await?.error_for_status()?;

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
        .strip_unlikelys(true)
        .clean_attributes(true)
        .parse(&s);

    debug!("Done.");

    let text_content = node_ref.text_contents();

    info!("Scraped down to len: {}", text_content.len());

    trace!("Scraped text:\n{text_content}");

    Ok(text_content.trim().into())
}
