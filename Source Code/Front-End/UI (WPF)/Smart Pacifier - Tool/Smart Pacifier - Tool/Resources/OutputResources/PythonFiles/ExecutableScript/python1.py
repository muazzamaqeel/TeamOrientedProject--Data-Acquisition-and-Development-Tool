import sys
import socket

def main():
    if len(sys.argv) < 2:
        print("Error: Port argument is missing.")
        sys.exit(1)

    port = int(sys.argv[1])

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind(("127.0.0.1", port))
        server_socket.listen(1)
        print(f"Listening on port {port}...")

        conn, addr = server_socket.accept()
        with conn:
            print(f"Connected by {addr}")
            data = conn.recv(1024)
            while data:
                print(f"Received: {data.decode('utf-8')}")
                conn.sendall(b"Batch processed successfully.")
                data = conn.recv(1024)

if __name__ == "__main__":
    main()
