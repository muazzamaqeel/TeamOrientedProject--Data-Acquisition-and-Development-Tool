import tkinter as tk
from tkinter import messagebox
import subprocess

def start_docker():
    try:
        subprocess.run(["docker-compose", "up", "-d"], check=True)
        messagebox.showinfo("Success", "Docker containers started successfully!")
    except subprocess.CalledProcessError as e:
        messagebox.showerror("Error", f"Failed to start containers: {e}")

def stop_docker():
    try:
        subprocess.run(["docker-compose", "down"], check=True)
        messagebox.showinfo("Success", "Docker containers stopped successfully!")
    except subprocess.CalledProcessError as e:
        messagebox.showerror("Error", f"Failed to stop containers: {e}")

root = tk.Tk()
root.title("Docker Compose GUI")

frame = tk.Frame(root, padx=20, pady=20)
frame.pack(padx=10, pady=10)

start_button = tk.Button(frame, text="Start Docker", command=start_docker, padx=20, pady=10)
start_button.grid(row=0, column=0, padx=10, pady=10)

stop_button = tk.Button(frame, text="Stop Docker", command=stop_docker, padx=20, pady=10)
stop_button.grid(row=0, column=1, padx=10, pady=10)

root.mainloop()
