use std::collections::HashMap;

use log::debug;
use regex::Regex;

/// A chunk of a document. Can be either text, or a Gen section.
#[derive(Debug)]
pub enum DocumentChunk<'a> {
    /// A pure text section.
    Text(&'a str),

    /// A Gen section.
    Gen(Gen),
}

/// A Document is simply a collection of `DocumentChunk`s.
#[derive(Debug)]
pub struct Document<'a> {
    chunks: Vec<DocumentChunk<'a>>,
}

/// A Gen chunk of a document, which has a name and a property bag.
#[derive(Debug)]
pub struct Gen {
    /// The name field from the Gen markup.
    pub name: String,

    /// The properties set in the Gen markup.
    pub properties: HashMap<String, String>,
}

impl Gen {
    /// Create a new Gen.
    #[must_use]
    pub fn new(name: String, properties: HashMap<String, String>) -> Self {
        Self { name, properties }
    }
}

impl<'a> Document<'a> {
    /// Create a new Document.
    #[must_use]
    pub fn new(chunks: Vec<DocumentChunk<'a>>) -> Self {
        Self { chunks }
    }

    /// Parse a document from the given text.
    #[must_use]
    pub fn parse(s: &str) -> Document {
        let re_main = Regex::new(r"\{\{gen\s+'([^']+)'\s*([^}]*)\}\}").unwrap();
        let re_props = Regex::new(r"(\w+)='([^']*)'").unwrap();

        let mut result = Vec::new();
        let mut last = 0;

        for cap_main in re_main.captures_iter(s) {
            let start = cap_main.get(0).unwrap().start();

            // Add the text up to the match
            if last != start {
                let chunk = DocumentChunk::Text(&s[last..start]);
                result.push(chunk);
            }

            let name = &cap_main[1];
            let props_text = &cap_main[2];
            let mut properties = HashMap::new();

            for cap_props in re_props.captures_iter(props_text) {
                let (_, [key, value]) = cap_props.extract();
                debug!("Property: {}, Value: {}", key, value);
                properties.insert(key.to_owned(), value.to_owned());
            }

            let full = &cap_main[0];
            last = start + full.len();

            result.push(DocumentChunk::Gen(Gen::new(name.to_owned(), properties)));
        }

        Document::new(result)
    }

    /// Gets the chunks from this document.
    #[must_use]
    pub fn chunks(&self) -> &[DocumentChunk<'_>] {
        self.chunks.as_ref()
    }
}
