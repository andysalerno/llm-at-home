# Use a minimal Python image
FROM python:3.13-slim

# Set the working directory
WORKDIR /app

# Copy requirements and install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy the application code
COPY app.py .

# Expose the port (default is 5000, but can be overridden via the PORT env var)
EXPOSE 5000

# Run the application
CMD ["python", "app.py"]
