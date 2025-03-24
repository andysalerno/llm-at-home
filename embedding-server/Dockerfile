FROM pytorch/pytorch:2.6.0-cuda12.6-cudnn9-runtime as base

COPY requirements.txt .
RUN pip install -r requirements.txt

COPY . .

ENV RERANKER_NAME=

ENTRYPOINT ["python", "./embedding_server.py"]