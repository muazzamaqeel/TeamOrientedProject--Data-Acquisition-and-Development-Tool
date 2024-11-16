import socket
import sys
import json

def main(port):
    host = '127.0.0.1'  # Localhost
    buffer_size = 4096  # Buffer size for receiving data

    # Connect to the TCP server
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect((host, port))

        # Read data sent from the server
        data = s.recv(buffer_size).decode()
        campaign_data = json.loads(data)
        
        # Process the data (customize this logic as needed)
        response_data = json.dumps({"status": "processed", "data": campaign_data})

        # Send response back to the server
        s.sendall(response_data.encode())

if __name__ == "__main__":
    port = int(sys.argv[1])  # Receive port as an argument
    main(port)
