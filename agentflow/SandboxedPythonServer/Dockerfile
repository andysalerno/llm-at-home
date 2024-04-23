# Use python:3.10-slim as the base image
FROM python:3.10-slim

# Set the working directory in the container
WORKDIR /app

# Copy the Python script into the container at /app
COPY server.py /app

EXPOSE 8000

# Command to run the server
CMD ["python", "server.py"]
