import socket
import json
import threading

def process_data(data):
    if "CampaignName" in data:
        data["processed"] = True
    else:
        data["error"] = "Missing CampaignName"
    return data

def process_with_port(port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(("127.0.0.1", port))  # Explicitly bind to IPv4
    server_socket.listen(1)
    print(f"Python server is listening on port {port}")

    while True:
        client_socket, _ = server_socket.accept()
        data = client_socket.recv(1024).decode()

        try:
            if data == "health_check":
                client_socket.sendall(b"OK")
            else:
                payload = json.loads(data)
                response = process_data(payload)
                client_socket.sendall(json.dumps(response).encode())
        except json.JSONDecodeError:
            client_socket.sendall(json.dumps({"error": "Invalid JSON"}).encode())
        finally:
            client_socket.close()

if __name__ == "__main__":
    import sys
    port = int(sys.argv[1])
    health_check_thread = threading.Thread(target=process_with_port, args=(port,))
    health_check_thread.daemon = True
    health_check_thread.start()
