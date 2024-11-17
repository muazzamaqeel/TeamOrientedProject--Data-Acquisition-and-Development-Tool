import socket
import sys
import json

def main(port):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(("localhost", port))
    server_socket.listen(1)
    print(f"Listening on port {port}")

    while True:
        client_socket, _ = server_socket.accept()
        data = client_socket.recv(1024).decode()
        print(f"Received data: {data}")

        # Process data (example: echo back with an extra field)
        payload = json.loads(data)
        payload["processed"] = True

        response = json.dumps(payload)
        client_socket.sendall(response.encode())
        client_socket.close()

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python python_processor.py <port>")
        sys.exit(1)

    port = int(sys.argv[1])
    main(port)
