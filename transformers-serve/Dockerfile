FROM nvidia/cuda:12.8.1-devel-ubuntu20.04

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install uv
COPY --from=ghcr.io/astral-sh/uv:latest /uv /usr/local/bin/uv

# Set working directory
WORKDIR /app

# Copy project files
COPY pyproject.toml ./
COPY main.py ./

# Install dependencies using uv
RUN uv venv
RUN uv add setuptools
RUN uv pip install -v gptqmodel --no-build-isolation
RUN uv add gptqmodel
RUN uv sync

# Set the command
CMD ["uv", "run", "transformers", "serve", "--help"]