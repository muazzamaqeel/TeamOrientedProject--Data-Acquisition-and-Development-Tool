import socket
import sys
import json

def process_data(data):
    """Generic function to process data."""
    if "CampaignName" in data:
        print(f"Processing database data for campaign: {data['CampaignName']}")
    else:
        print(f"Processing live data for PacifierId: {data.get('PacifierId', 'Unknown')}")

    # Example: Add a processed field to indicate successful processing
    data["processed"] = True
    return data

def process_with_port(port):
    """Process live data using TCP."""
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(("localhost", port))
    server_socket.listen(1)
    print(f"Listening on port {port}")

    while True:
        client_socket, _ = server_socket.accept()
        data = client_socket.recv(1024).decode()
        print(f"Received data: {data}")

        try:
            payload = json.loads(data)
            response = process_data(payload)
            client_socket.sendall(json.dumps(response).encode())
        except json.JSONDecodeError as e:
            print(f"Invalid JSON received: {e}")
            client_socket.sendall(json.dumps({"error": "Invalid JSON"}).encode())
        finally:
            client_socket.close()

def process_without_port():
    """Process database data via standard input."""
    input_data = sys.stdin.read()
    payload = json.loads(input_data)
    response = process_data(payload)
    print(json.dumps(response, indent=4))

if __name__ == "__main__":
    if len(sys.argv) == 2:
        port = int(sys.argv[1])
        process_with_port(port)
    else:
        process_without_port()
