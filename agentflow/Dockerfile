FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

ARG EMBEDDING_URI=http://embedding-server:8000/embeddings
ARG SCRAPER_URI=http://scraper:8002/scrape
ARG OPENAI_URI=http://openaiapi:8003
ARG MODEL_NAME=http://openaiapi:8003

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Examples.dll", "$OPENAI_URI", "$EMBEDDING_URI", "$SCRAPER_URI", "$MODEL_NAME", "-v", "--prompt-dir=Prompts"]
