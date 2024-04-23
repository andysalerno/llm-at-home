from http.server import BaseHTTPRequestHandler, HTTPServer
import subprocess
import os


class SimpleHTTPRequestHandler(BaseHTTPRequestHandler):
    def do_POST(self):
        # Read the length of the incoming POST data
        content_length = int(self.headers["Content-Length"])
        # Read the POST data
        post_data = self.rfile.read(content_length)

        print("writing to run.py...", flush=True)

        # Write the POST data to run.py
        with open("run.py", "wb") as file:
            file.write(post_data)

        print("Done. Running run.py...", flush=True)

        # Execute run.py and capture stdout and stderr
        result = subprocess.run(["python", "run.py"], capture_output=True, text=True)

        print("Done.", flush=True)

        # Send a 200 OK response
        self.send_response(200)
        self.send_header("Content-type", "text/plain")
        self.end_headers()

        # Send the stdout and stderr from the execution
        self.wfile.write(result.stdout.encode())
        self.wfile.write(result.stderr.encode())


def run(server_class=HTTPServer, handler_class=SimpleHTTPRequestHandler, port=8000):
    server_address = ("0.0.0.0", port)
    httpd = server_class(server_address, handler_class)
    print(f"Server running on port {port}...", flush=True)
    httpd.serve_forever()


if __name__ == "__main__":
    run()
