FROM rust:1.79.0-slim as builder
WORKDIR /usr/src/myapp

COPY . .
RUN cargo install --path .

CMD ["webprompt"]