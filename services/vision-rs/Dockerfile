FROM rust:1-bookworm AS build
WORKDIR /app

COPY contracts ./contracts
COPY services/vision-rs ./services/vision-rs

WORKDIR /app/services/vision-rs
RUN cargo build --release

FROM debian:bookworm-slim
WORKDIR /app
COPY --from=build /app/services/vision-rs/target/release/vision-rs /usr/local/bin/vision-rs

EXPOSE 50051

ENTRYPOINT ["vision-rs"]
