FROM rust:1.77.2-slim as builder
WORKDIR /usr/src/myapp

RUN apt-get update && apt-get install -y libssl-dev pkg-config && rm -rf /var/lib/apt/lists/*

COPY . .
RUN cargo install --path .

CMD ["scraper"]